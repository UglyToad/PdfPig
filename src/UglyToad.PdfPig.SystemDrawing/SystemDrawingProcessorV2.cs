namespace UglyToad.PdfPig.SystemDrawing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Drawing.Text;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Graphics;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Graphics.Core;
    using UglyToad.PdfPig.Graphics.Shading;
    using UglyToad.PdfPig.PdfFonts;
    using UglyToad.PdfPig.Tokens;
    using UglyToad.PdfPig.XObjects;

    public class SystemDrawingProcessorV2 : IOperationContext, IDrawingProcessor
    {
        private Graphics currentGraphics;

        private readonly Stack<CurrentSystemGraphicsState> graphicsStack = new Stack<CurrentSystemGraphicsState>();
        private IFont activeExtendedGraphicsStateFont;

        public GraphicsPath CurrentPath { get; private set; }

        #region IDrawingProcessor
        float pageScale;
        float pageHeight;
        Page currentPage;

        public MemoryStream DrawPage(Page page, double scale)
        {
            if (Math.Abs(scale) < double.Epsilon)
            {
                throw new ArgumentException("The scaling factor must be different from 0.", nameof(scale));
            }

            fontFamilies = new Dictionary<string, (PrivateFontCollection, FontFamily)>();
            pageScale = (float)scale;
            currentPage = page;
            pageHeight = (float)page.Height;

            var ms = new MemoryStream();
            using (var bitmap = new Bitmap((int)Math.Ceiling(page.Width * pageScale), (int)Math.Ceiling(page.Height * pageScale)))
            using (currentGraphics = Graphics.FromImage(bitmap))
            {
                currentGraphics.SmoothingMode = SmoothingMode.HighQuality;
                currentGraphics.CompositingQuality = CompositingQuality.HighQuality;
                currentGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                currentGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                currentGraphics.Clear(Color.White);
                Init();

                foreach (var stateOperation in page.Operations)
                {
                    stateOperation.Run(this);
                }

                bitmap.Save(ms, ImageFormat.Png);

                foreach (var font in fontFamilies)
                {
                    font.Value.family.Dispose();
                    font.Value.collection?.Dispose();
                }
            }

            return ms;
        }

        public void Init()
        {
            // flip transform
            currentGraphics.Transform = GetInitialMatrix(currentPage.Rotation.Value, currentPage.MediaBox);
            currentGraphics.TranslateTransform(0, -pageHeight, MatrixOrder.Append);
            currentGraphics.ScaleTransform(pageScale, -pageScale, MatrixOrder.Append);

            //graphics.PageUnit = GraphicsUnit.Point;
            //currentGraphics.RenderingOrigin = new Point(0, (int)currentPage.Height);

            // initiate CurrentClippingPath to cropBox
            GraphicsPath clip = new GraphicsPath(FillMode.Alternate);
            clip.StartFigure();
            clip.AddLine((float)currentPage.CropBox.Bounds.BottomLeft.X, (float)currentPage.CropBox.Bounds.BottomLeft.Y, (float)currentPage.CropBox.Bounds.TopRight.X, (float)currentPage.CropBox.Bounds.BottomLeft.Y);
            clip.AddLine((float)currentPage.CropBox.Bounds.TopRight.X, (float)currentPage.CropBox.Bounds.BottomLeft.Y, (float)currentPage.CropBox.Bounds.TopRight.X, (float)currentPage.CropBox.Bounds.TopRight.Y);
            clip.AddLine((float)currentPage.CropBox.Bounds.TopRight.X, (float)currentPage.CropBox.Bounds.TopRight.Y, (float)currentPage.CropBox.Bounds.BottomLeft.X, (float)currentPage.CropBox.Bounds.TopRight.Y);
            clip.CloseFigure();
            currentGraphics.SetClip(clip, CombineMode.Replace);

            graphicsStack.Push(new CurrentSystemGraphicsState()
            {
                GraphicsState = currentGraphics.Save()
            });

            ColorSpaceContext = new SystemDrawingColorSpaceContext(GetCurrentState, currentPage.ExperimentalAccess.ResourceStore);
        }


        public void DrawImage(IPdfImage image)
        {
            if (image.TryGetPng(out var png))
            {
                using (var img = Image.FromStream(new MemoryStream(png)))
                {
                    img.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    currentGraphics.DrawImage(img, new RectangleF(0, 0, 1, 1));
                }
            }
            else
            {
                if (image.TryGetBytes(out var bytes))
                {
                    try
                    {
                        using (var img = Image.FromStream(new MemoryStream(bytes.ToArray())))
                        {
                            img.RotateFlip(RotateFlipType.RotateNoneFlipY);
                            currentGraphics.DrawImage(img, new RectangleF(0, 0, 1, 1));
                        }
                        return;
                    }
                    catch (Exception)
                    {

                    }
                }

                try
                {
                    using (var img = Image.FromStream(new MemoryStream(image.RawBytes.ToArray())))
                    {
                        img.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        currentGraphics.DrawImage(img, new RectangleF(0, 0, 1, 1));
                    }
                }
                catch (Exception)
                {
                    currentGraphics.FillRectangle(Brushes.HotPink, new RectangleF(0, 0, 1, 1));
                }
            }
        }
        #endregion

        #region State
        [DebuggerStepThrough]
        public CurrentSystemGraphicsState GetCurrentState()
        {
            return graphicsStack.Peek();
        }

        public void PopState()
        {
            graphicsStack.Pop();
            currentGraphics.Restore(GetCurrentState().GraphicsState);
            activeExtendedGraphicsStateFont = null;
        }

        public void PushState()
        {
            graphicsStack.Peek().GraphicsState = currentGraphics.Save();
            graphicsStack.Push(graphicsStack.Peek().DeepClone());
        }
        #endregion

        #region IOperationContext
        public IColorSpaceContext ColorSpaceContext { get; set; }

        public PdfPoint CurrentPosition { get; set; }

        public Matrix TextMatrix { get; set; }

        public Matrix TextLineMatrix { get; set; }

        public int StackSize => graphicsStack.Count;

        public void ApplyXObject(NameToken xObjectName)
        {
            var xObjectStream = currentPage.ExperimentalAccess.ResourceStore.GetXObject(xObjectName);

            // For now we will determine the type and store the object with the graphics state information preceding it.
            // Then consumers of the page can request the object(s) to be retrieved by type.
            var subType = (NameToken)xObjectStream.StreamDictionary.Data[NameToken.Subtype.Data];

            var state = GetCurrentState();

            var matrix = TransformationMatrix.Identity;

            if (subType.Equals(NameToken.Ps))
            {
                var contentRecord = new XObjectContentRecord(XObjectType.PostScript, xObjectStream, matrix, state.RenderingIntent,
                    state.CurrentStrokingColorSpace ?? ColorSpace.DeviceRGB);

                //xObjects[XObjectType.PostScript].Add(contentRecord);
                // Draw
            }
            else if (subType.Equals(NameToken.Image))
            {
                var contentRecord = new XObjectContentRecord(XObjectType.Image, xObjectStream, matrix, state.RenderingIntent,
                    state.CurrentStrokingColorSpace ?? ColorSpace.DeviceRGB);

                DrawImage(XObjectFactory.ReadImage(contentRecord,
                                                   currentPage.ExperimentalAccess.PdfTokenScanner,
                                                   currentPage.ExperimentalAccess.FilterProvider,
                                                   currentPage.ExperimentalAccess.ResourceStore));
            }
            else if (subType.Equals(NameToken.Form))
            {
                ProcessFormXObject(xObjectStream);
            }
            else
            {
                throw new InvalidOperationException($"XObject encountered with unexpected SubType {subType}. {xObjectStream.StreamDictionary}.");
            }
        }

        private void ProcessFormXObject(StreamToken formStream)
        {
            /*
             * When a form XObject is invoked the following should happen:
             *
             * 1. Save the current graphics state, as if by invoking the q operator.
             * 2. Concatenate the matrix from the form dictionary's Matrix entry with the current transformation matrix.
             * 3. Clip according to the form dictionary's BBox entry.
             * 4. Paint the graphics objects specified in the form's content stream.
             * 5. Restore the saved graphics state, as if by invoking the Q operator.
             */

            var hasResources = formStream.StreamDictionary.TryGet<DictionaryToken>(NameToken.Resources, currentPage.ExperimentalAccess.PdfTokenScanner, out var formResources);
            if (hasResources)
            {
                currentPage.ExperimentalAccess.ResourceStore.LoadResourceDictionary(formResources);
            }

            // 1. Save current state.
            PushState();

            var startState = GetCurrentState();

            // 2. Update current transformation matrix.
            if (formStream.StreamDictionary.TryGet<ArrayToken>(NameToken.Matrix, currentPage.ExperimentalAccess.PdfTokenScanner, out var formMatrixToken))
            {
                ModifyCurrentTransformationMatrix(formMatrixToken.Data.OfType<NumericToken>().Select(x => x.Double).ToArray());
            }

            var contentStream = formStream.Decode(currentPage.ExperimentalAccess.FilterProvider);
            var operations = currentPage.ExperimentalAccess.PageContentParser.Parse(currentPage.Number, new ByteArrayInputBytes(contentStream), null);

            // 3. Clip according to the form dictionary's BBox entry.
            if (formStream.StreamDictionary.TryGet<ArrayToken>(NameToken.Bbox, currentPage.ExperimentalAccess.PdfTokenScanner, out var formBboxToken))
            {
                NumericToken left = null;
                NumericToken bottom = null;
                NumericToken right = null;
                NumericToken top = null;

                if (formBboxToken.Length == 4)
                {
                    left = formBboxToken[0] as NumericToken;
                    bottom = formBboxToken[1] as NumericToken;
                    right = formBboxToken[2] as NumericToken;
                    top = formBboxToken[3] as NumericToken;
                }
                else if (formBboxToken.Length == 6)
                {
                    left = formBboxToken[2] as NumericToken;
                    bottom = formBboxToken[3] as NumericToken;
                    right = formBboxToken[4] as NumericToken;
                    top = formBboxToken[5] as NumericToken;
                }

                if (left != null && bottom != null && right != null && top != null)
                {
                    GraphicsPath clip = new GraphicsPath(FillMode.Alternate); // alternate???
                    clip.StartFigure();
                    clip.AddLine((float)left.Double, (float)bottom.Double, (float)right.Double, (float)bottom.Double);
                    clip.AddLine((float)right.Double, (float)bottom.Double, (float)right.Double, (float)top.Double);
                    clip.AddLine((float)right.Double, (float)top.Double, (float)left.Double, (float)top.Double);
                    clip.CloseFigure();
                    currentGraphics.SetClip(clip, CombineMode.Intersect);
                }
            }

            // 4. Paint the objects.
            foreach (var stateOperation in operations)
            {
                stateOperation.Run(this);
            }

            // 5. Restore saved state.
            PopState();

            if (hasResources)
            {
                currentPage.ExperimentalAccess.ResourceStore.UnloadResourceDictionary();
            }
        }

        #region InlineImage
        private InlineImageBuilder inlineImageBuilder;
        public void BeginInlineImage()
        {
            if (inlineImageBuilder != null)
            {
                //log?.Error("Begin inline image (BI) command encountered while another inline image was active.");
            }

            inlineImageBuilder = new InlineImageBuilder();
        }

        public void EndInlineImage(IReadOnlyList<byte> bytes)
        {
            if (inlineImageBuilder == null)
            {
                //log?.Error("End inline image (EI) command encountered without a corresponding begin inline image (BI) command.");
                return;
            }

            inlineImageBuilder.Bytes = bytes;

            var image = inlineImageBuilder.CreateInlineImage(TransformationMatrix.Identity,
                currentPage.ExperimentalAccess.FilterProvider,
                currentPage.ExperimentalAccess.PdfTokenScanner,
                GetCurrentState().RenderingIntent,
                currentPage.ExperimentalAccess.ResourceStore);

            DrawImage(image);

            inlineImageBuilder = null;
        }

        public void SetInlineImageProperties(IReadOnlyDictionary<NameToken, IToken> properties)
        {
            if (inlineImageBuilder == null)
            {
                //log?.Error("Begin inline image data (ID) command encountered without a corresponding begin inline image (BI) command.");
                return;
            }

            inlineImageBuilder.Properties = properties;
        }
        #endregion

        #region MarkedContent
        public void BeginMarkedContent(NameToken name, NameToken propertyDictionaryName, DictionaryToken properties)
        {
            // do nothing
        }

        public void EndMarkedContent()
        {
            // do nothing
        }
        #endregion

        #region Paths
        /*
         * PdfPath is GraphicsPath
         * PdfSubpath is Figure
         */
        public void AddCurrentSubpath()
        {
            // do nothing
        }

        public void BeginSubpath()
        {
            if (CurrentPath == null)
            {
                CurrentPath = new GraphicsPath();
            }

            CurrentPath.StartFigure();
        }

        public void MoveTo(double x, double y)
        {
            BeginSubpath();
            CurrentPosition = new PdfPoint(x, y);
        }

        public void LineTo(double x, double y)
        {
            if (CurrentPath == null)
            {
                return;
            }

            CurrentPath.AddLine((float)CurrentPosition.X, (float)CurrentPosition.Y,
                                (float)x, (float)y);
            CurrentPosition = new PdfPoint(x, y);
        }

        public void BezierCurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            if (CurrentPath == null)
            {
                return;
            }

            CurrentPath.AddBezier((float)CurrentPosition.X, (float)CurrentPosition.Y,
                                  (float)x1, (float)y1,
                                  (float)x2, (float)y2,
                                  (float)x3, (float)y3);
            CurrentPosition = new PdfPoint(x3, y3);
        }

        public void BezierCurveTo(double x2, double y2, double x3, double y3)
        {
            if (CurrentPath == null)
            {
                return;
            }

            CurrentPath.AddBezier((float)CurrentPosition.X, (float)CurrentPosition.Y,
                                  (float)CurrentPosition.X, (float)CurrentPosition.Y,
                                  (float)x2, (float)y2,
                                  (float)x3, (float)y3);
            CurrentPosition = new PdfPoint(x3, y3);
        }

        public void Rectangle(double x, double y, double width, double height)
        {
            MoveTo(x, y);                       // x y m
            LineTo(x + width, y);               // (x + width) y l
            LineTo(x + width, y + height);      // (x + width) (y + height) l
            LineTo(x, y + height);              // x (y + height) l
            CloseSubpath();                     // h
        }

        /// <summary>
        /// Do not draw the path
        /// </summary>
        public void EndPath()
        {
            CurrentPath = null;
        }

        /// <summary>
        /// Draw the path
        /// </summary>
        public void ClosePath()
        {
            CurrentPath = null;
        }

        public void FillPath(FillingRule fillingRule, bool close)
        {
            if (CurrentPath == null)
            {
                return;
            }

            if (close)
            {
                CurrentPath.CloseFigure();
            }

            CurrentPath.FillMode = fillingRule.ToSystemFillMode();
            using (var brush = new SolidBrush(GetCurrentState().CurrentNonStrokingColor))
            {
                currentGraphics.FillPath(brush, CurrentPath);
            }

            ClosePath();
        }

        public void StrokePath(bool close)
        {
            if (CurrentPath == null)
            {
                return;
            }

            if (close)
            {
                CurrentPath.CloseFigure();
            }

            var currentState = GetCurrentState();
            try
            {
                using (var pen = currentState.CurrentStrokingPen)
                {
                    currentGraphics.DrawPath(pen, CurrentPath);
                }
            }
            catch (OutOfMemoryException)
            {
                //you will get an OutOfMemoryException if you try to use a LinearGradientBrush to fill a rectangle whose width or height is zero
                var bounds = CurrentPath.GetBounds();
                if (bounds.Width >= 1 && bounds.Height >= 1)
                {
                    throw;
                }
            }


            ClosePath();
        }

        public void FillStrokePath(FillingRule fillingRule, bool close)
        {
            if (CurrentPath == null)
            {
                return;
            }

            if (close)
            {
                CurrentPath.CloseFigure();
            }

            var currentState = GetCurrentState();

            // Fill
            CurrentPath.FillMode = fillingRule.ToSystemFillMode();
            using (var brush = new SolidBrush(currentState.CurrentNonStrokingColor))
            {
                currentGraphics.FillPath(brush, CurrentPath);
            }

            // Stroke
            try
            {
                using (var pen = currentState.CurrentStrokingPen)
                {
                    currentGraphics.DrawPath(pen, CurrentPath);
                }
            }
            catch (OutOfMemoryException)
            {
                //you will get an OutOfMemoryException if you try to use a LinearGradientBrush to fill a rectangle whose width or height is zero
                var bounds = CurrentPath.GetBounds();
                if (bounds.Width >= 1 && bounds.Height >= 1)
                {
                    throw;
                }
            }

            ClosePath();
        }

        public void PaintShading(NameToken Name)
        {
            if (currentPage.Dictionary.TryGet<DictionaryToken>(NameToken.Resources, out var resources) && 
                resources.TryGet<DictionaryToken>(NameToken.Shading, currentPage.ExperimentalAccess.PdfTokenScanner, out var shadingResources))
            {
                // page 183
                if (shadingResources.TryGet<DictionaryToken>(Name, currentPage.ExperimentalAccess.PdfTokenScanner, out var shadingDictionary))
                {
                    var shading = PdfShading.Parse(shadingDictionary, currentPage.ExperimentalAccess.PdfTokenScanner);

                    if (shading.Background != null)
                    {
                        // paint background
                    }

                    // to implement, using a placeholder for the moment
                    using (var brush = shading.ToSystemGradientBrush())
                    using (var region = currentGraphics.Clip.Clone())
                    {
                        currentGraphics.FillRegion(brush, region);
                    }
                }
                else
                {
                    // is it possible??
                }
            }
        }

        public PdfPoint? CloseSubpath()
        {
            if (CurrentPath == null)
            {
                return null;
            }

            CurrentPath.CloseFigure();

            if (CurrentPath.PointCount > 0)
            {
                var firstPoint = CurrentPath.PathPoints[0];
                return new PdfPoint(firstPoint.X, firstPoint.Y);
            }
            return null;
        }

        public void ModifyClippingIntersect(FillingRule clippingRule)
        {
            if (CurrentPath == null)
            {
                return;
            }

            CurrentPath.FillMode = clippingRule.ToSystemFillMode();
            currentGraphics.SetClip(CurrentPath, CombineMode.Intersect);
        }
        #endregion

        #region Lines
        public void SetLineCap(LineCapStyle cap)
        {
            GetCurrentState().LineCap = cap.ToSystemLineCap();
            GetCurrentState().DashCap = cap.ToSystemDashCap();
        }

        private float ConvertDashPatterValue(decimal value)
        {
            if (value == 0)
            {
                return 1f / 72f;
            }
            return (float)((double)value / 72.0);
        }

        public void SetLineDashPattern(LineDashPattern lineDashPattern)
        {
            // update DashPatternArray
            /*
             * https://docs.microsoft.com/en-us/dotnet/api/system.drawing.pen.dashpattern?view=dotnet-plat-ext-3.1
             * The elements in the dashArray array set the length of each dash and space in the dash pattern. 
             * The first element sets the length of a dash, the second element sets the length of a space, the
             * third element sets the length of a dash, and so on. Consequently, each element should be a 
             * non-zero positive number.
             */
            if (lineDashPattern.Array.Count == 1)
            {
                var v = ConvertDashPatterValue(lineDashPattern.Array[0]);
                GetCurrentState().DashPatternArray = new float[] { v, v };
            }
            else if (lineDashPattern.Array.Count > 0)
            {
                List<float> pattern = new List<float>();
                for (int i = 0; i < lineDashPattern.Array.Count; i++)
                {
                    var v = ConvertDashPatterValue(lineDashPattern.Array[i]);
                    pattern.Add(v);
                }
                GetCurrentState().DashPatternArray = pattern.ToArray();
            }
            else
            {
                GetCurrentState().DashPatternArray = null;
            }

            // update DashPatternPhase
            GetCurrentState().DashPatternPhase = lineDashPattern.Phase; // divide by 72??
        }

        public void SetLineJoin(LineJoinStyle join)
        {
            GetCurrentState().JoinStyle = join.ToSystemLineJoin();
        }

        public void SetLineWidth(decimal width)
        {
            GetCurrentState().LineWidth = width;
        }

        public void SetMiterLimit(decimal limit)
        {
            GetCurrentState().MiterLimit = limit;
        }

        public void SetFlatnessTolerance(decimal tolerance)
        {
            GetCurrentState().Flatness = tolerance;
        }
        #endregion

        #region Text
        #region TextMatrix
        public void BeginText()
        {
            TextMatrix = MatrixExtensions.Identity;
            TextLineMatrix = MatrixExtensions.Identity;
        }

        public void EndText()
        {
            TextMatrix = MatrixExtensions.Identity;
            TextLineMatrix = MatrixExtensions.Identity;
        }

        public void SetTextMatrix(double[] value)
        {
            using (var newMatrix = MatrixExtensions.FromArray(value))
            {
                TextMatrix = newMatrix.Clone();
                TextLineMatrix = newMatrix.Clone();
            }
        }

        private void AdjustTextMatrix(double tx, double ty)
        {
            TextMatrix.Translate((float)tx, (float)ty, MatrixOrder.Prepend);
        }

        public void MoveToNextLineWithOffset(double tx, double ty)
        {
            using (var currentTextLineMatrix = TextLineMatrix.Clone())
            {
                currentTextLineMatrix.Translate((float)tx, (float)ty, MatrixOrder.Prepend);
                TextLineMatrix = currentTextLineMatrix.Clone();
                TextMatrix = currentTextLineMatrix.Clone();
            }
        }

        public void MoveToNextLineWithOffset()
        {
            MoveToNextLineWithOffset(0, -1 * GetCurrentState().FontState.Leading);
        }
        #endregion

        public void SetTextLeading(double leading)
        {
            GetCurrentState().FontState.Leading = leading;
        }

        public void SetTextRenderingMode(TextRenderingMode mode)
        {
            GetCurrentState().FontState.TextRenderingMode = mode;
        }

        public void SetTextRise(double rise)
        {
            GetCurrentState().FontState.Rise = rise;
        }

        public void SetWordSpacing(double spacing)
        {
            GetCurrentState().FontState.WordSpacing = spacing;
        }

        public void ShowPositionedText(IReadOnlyList<IToken> tokens)
        {
            var currentState = GetCurrentState();

            var textState = currentState.FontState;

            var fontSize = textState.FontSize;
            var horizontalScaling = textState.HorizontalScaling / 100.0;
            var font = currentPage.ExperimentalAccess.ResourceStore.GetFont(textState.FontName);

            var isVertical = font.IsVertical;

            foreach (var token in tokens)
            {
                if (token is NumericToken number)
                {
                    var positionAdjustment = (double)number.Data;

                    double tx, ty;
                    if (isVertical)
                    {
                        tx = 0;
                        ty = -positionAdjustment / 1000 * fontSize;
                    }
                    else
                    {
                        tx = -positionAdjustment / 1000 * fontSize * horizontalScaling;
                        ty = 0;
                    }

                    AdjustTextMatrix(tx, ty);
                }
                else
                {
                    IReadOnlyList<byte> bytes;
                    if (token is HexToken hex)
                    {
                        bytes = hex.Bytes;
                    }
                    else
                    {
                        bytes = OtherEncodings.StringAsLatin1Bytes(((StringToken)token).Data);
                    }

                    ShowText(new ByteArrayInputBytes(bytes));
                }
            }
        }

        private Dictionary<string, (PrivateFontCollection collection, FontFamily family)> fontFamilies;

        public void ShowText(IInputBytes bytes)
        {
            var currentState = GetCurrentState();

            IFont font = currentState.FontState.FromExtendedGraphicsState ? activeExtendedGraphicsStateFont : currentPage.ExperimentalAccess.ResourceStore.GetFont(currentState.FontState.FontName);

            if (font == null)
            {
                throw new InvalidOperationException($"Could not find the font with name {currentState.FontState.FontName} in the resource store. It has not been loaded yet.");
            }

            var fontSize = currentState.FontState.FontSize;
            var horizontalScaling = currentState.FontState.HorizontalScaling / 100.0;
            var characterSpacing = currentState.FontState.CharacterSpacing;
            var rise = currentState.FontState.Rise;

            using (Matrix renderingMatrix = MatrixExtensions.FromValues(fontSize * horizontalScaling, 0, 0, fontSize, 0, rise))
            {
                /*
                // TODO: this does not seem correct, produces the correct result for now but we need to revisit.
                // see: https://stackoverflow.com/questions/48010235/pdf-specification-get-font-size-in-points
                double pointSize = double.NaN;
                using (var fontSizeMatrix = currentGraphics.Transform.Clone())
                {
                    //ReverseFlipYAxisTransform(fontSizeMatrix);
                    //fontSizeMatrix.Multiply(TextMatrix, MatrixOrder.Append);
                    //fontSizeMatrix = fontSizeMatrix.Multiply(fontSize);

                    pointSize = Math.Round(TextMatrix.Elements[0] * fontSize, 2); // A

                    // Assume a rotated letter
                    if (pointSize == 0)
                    {
                        pointSize = Math.Round(TextMatrix.Elements[1] * fontSize, 2); // B
                    }
                }

                if (pointSize < 0)
                {
                    pointSize *= -1;
                }
                */

                while (bytes.MoveNext())
                {
                    var code = font.ReadCharacterCode(bytes, out int codeLength);

                    var foundUnicode = font.TryGetUnicode(code, out var unicode);

                    if (!foundUnicode || unicode == null)
                    {
                        //log?.Warn($"We could not find the corresponding character with code {code} in font {font.Name}.");
                        // Try casting directly to string as in PDFBox 1.8.
                        unicode = new string((char)code, 1);
                    }

                    var wordSpacing = 0.0;
                    if (code == ' ' && codeLength == 1)
                    {
                        wordSpacing += GetCurrentState().FontState.WordSpacing;
                    }

                    var boundingBox = font.GetBoundingBox(code);

                    using (var textMatrix = TextMatrix.Clone())
                    {
                        if (font.IsVertical)
                        {
                            if (!(font is IVerticalWritingSupported verticalFont))
                            {
                                throw new InvalidOperationException($"Font {font.Name} was in vertical writing mode but did not implement {nameof(IVerticalWritingSupported)}.");
                            }

                            var positionVector = verticalFont.GetPositionVector(code);

                            textMatrix.Translate((float)positionVector.X, (float)positionVector.Y);
                        }

                        // If the text rendering mode calls for filling, the current nonstroking color in the graphics state is used; 
                        // if it calls for stroking, the current stroking color is used.
                        // In modes that perform both filling and stroking, the effect is as if each glyph outline were filled and then stroked in separate operations.
                        // TODO: expose color as something more advanced
                        Color color = currentState.FontState.TextRenderingMode != TextRenderingMode.Stroke
                            ? currentState.CurrentNonStrokingColor
                            : currentState.CurrentStrokingColor;

                        using (var renderingMatrixCopy = renderingMatrix.Clone())
                        {
                            renderingMatrixCopy.Multiply(textMatrix, MatrixOrder.Append);

                            // print fonts that have paths
                            if (font.TryGetPath(code, out var pdfSubpaths))
                            {
                                if (pdfSubpaths == null || pdfSubpaths.Count == 0)
                                {
                                    throw new ArgumentException("DrawLetter(): empty path");
                                }

                                var bbox = boundingBox.GlyphBounds;

                                // ************** for debugging purpose - to remove
                                //GraphicsPath gpTest = new GraphicsPath(FillMode.Alternate); // Alternate?
                                //gpTest.StartFigure();
                                //gpTest.AddLine((float)bbox.BottomLeft.X, (float)bbox.BottomLeft.Y, (float)bbox.TopRight.X, (float)bbox.BottomLeft.Y);
                                //gpTest.AddLine((float)bbox.TopRight.X, (float)bbox.BottomLeft.Y, (float)bbox.TopRight.X, (float)bbox.TopRight.Y);
                                //gpTest.AddLine((float)bbox.TopRight.X, (float)bbox.TopRight.Y, (float)bbox.BottomLeft.X, (float)bbox.TopRight.Y);
                                //gpTest.CloseFigure();
                                //gpTest.Transform(renderingMatrixCopy);
                                //currentGraphics.DrawPath(Pens.Red, gpTest);

                                //GraphicsPath gpTest2 = new GraphicsPath(FillMode.Alternate); // Alternate?
                                //gpTest2.AddRectangle(new RectangleF(0, (float)0, (float)bbox.Width, (float)1));
                                //using (var inverseYAxis = MatrixExtensions.GetScaleMatrix(1, -1))
                                //{
                                //    inverseYAxis.Translate(0, (float)1, MatrixOrder.Append);
                                //    gpTest2.Transform(inverseYAxis);
                                //}
                                //gpTest2.Transform(renderingMatrixCopy);
                                //currentGraphics.DrawPath(Pens.GreenYellow, gpTest2);
                                // **************** end to remove

                                GraphicsPath gp = new GraphicsPath(FillMode.Alternate); // Alternate?
                                foreach (var subpath in pdfSubpaths)
                                {
                                    foreach (var c in subpath.Commands)
                                    {
                                        if (c is PdfSubpath.Move move)
                                        {
                                            gp.StartFigure();
                                        }
                                        else if (c is PdfSubpath.Line line)
                                        {
                                            gp.AddLine((float)line.From.X, (float)line.From.Y,
                                                       (float)line.To.X, (float)line.To.Y);
                                        }
                                        else if (c is PdfSubpath.BezierCurve curve)
                                        {
                                            gp.AddBezier((float)curve.StartPoint.X, (float)curve.StartPoint.Y,
                                                         (float)curve.FirstControlPoint.X, (float)curve.FirstControlPoint.Y,
                                                         (float)curve.SecondControlPoint.X, (float)curve.SecondControlPoint.Y,
                                                         (float)curve.EndPoint.X, (float)curve.EndPoint.Y);
                                        }
                                        else if (c is PdfSubpath.Close)
                                        {
                                            gp.CloseFigure();
                                        }
                                    }
                                }

                                gp.Transform(renderingMatrixCopy);

                                using (var fillBrush = new SolidBrush(color))
                                {
                                    currentGraphics.FillPath(fillBrush, gp);
                                }
                            }
                            else
                            {
                                if (unicode != "" && unicode != " ")
                                {
                                    // unstable here: are the PrivateFontCollection dispose by the GC?? we need to avoid that

                                    // https://web.archive.org/web/20170313145219/https://blog.andreloker.de/post/2008/07/03/Load-a-font-from-disk-stream-or-byte-array.aspx
                                    // It seems that you must not dipose the PrivateFontCollection before you're done with 
                                    // the fonts within it; otherwise your app my crash. I updated the methods above to return 
                                    // the PrivateFontCollection instance. The caller has to dispose the collection after /
                                    // he/she is done using the fonts.
                                    FontFamily fontFamily;
                         
                                    if (fontFamilies.ContainsKey(font.Name))
                                    {
                                        fontFamily = fontFamilies[font.Name].family;
                                    }
                                    else if (fontFamilies.ContainsKey(font.Details.FontFamily))
                                    {
                                        fontFamily = fontFamilies[font.Details.FontFamily].family;
                                    }
                                    else
                                    {
                                        if (font.TryGetDecodedFontBytes(currentPage.ExperimentalAccess.PdfTokenScanner, currentPage.ExperimentalAccess.FilterProvider, out var fontBytes) &&
                                            TryLoadFontCollection(fontBytes.ToArray(), out PrivateFontCollection fontCollection))
                                        {
                                            fontFamily = fontCollection.Families[0];
                                            fontFamilies[font.Name] = (fontCollection, fontFamily);
               
                                            if (font.Details.IsBold && fontFamily.IsStyleAvailable(FontStyle.Bold))
                                            {

                                            }

                                            if (font.Details.IsItalic && fontFamily.IsStyleAvailable(FontStyle.Italic))
                                            {

                                            }
                                        }
                                        else
                                        {
                                            try
                                            {
                                                fontFamily = new FontFamily(font.Details.FontFamily);
                                                fontFamilies[font.Details.FontFamily] = (null, fontFamily);
                                            }
                                            catch
                                            {
                                                fontFamily = new FontFamily("Arial");
                                                fontFamilies[font.Name] = (null, fontFamily);
                                            }
                                        }
                                    }

                                    var style = font.Details.IsBold ? FontStyle.Bold : (font.Details.IsItalic ? FontStyle.Italic : FontStyle.Regular);

                                    var bbox = boundingBox.GlyphBounds;

                                    // ************** for debugging purpose - to remove
                                    //GraphicsPath gpTest = new GraphicsPath(FillMode.Alternate); // Alternate?
                                    //gpTest.StartFigure();
                                    //gpTest.AddLine((float)bbox.BottomLeft.X, (float)bbox.BottomLeft.Y, (float)bbox.TopRight.X, (float)bbox.BottomLeft.Y);
                                    //gpTest.AddLine((float)bbox.TopRight.X, (float)bbox.BottomLeft.Y, (float)bbox.TopRight.X, (float)bbox.TopRight.Y);
                                    //gpTest.AddLine((float)bbox.TopRight.X, (float)bbox.TopRight.Y, (float)bbox.BottomLeft.X, (float)bbox.TopRight.Y);
                                    //gpTest.CloseFigure();
                                    //gpTest.Transform(renderingMatrixCopy);
                                    //currentGraphics.DrawPath(Pens.Red, gpTest);

                                    //GraphicsPath gpTest2 = new GraphicsPath(FillMode.Alternate); // Alternate?
                                    //gpTest2.AddRectangle(new RectangleF(0, (float)0, (float)bbox.Width, (float)1));
                                    //using (var inverseYAxis = MatrixExtensions.GetScaleMatrix(1, -1))
                                    //{
                                    //    inverseYAxis.Translate(0, (float)1, MatrixOrder.Append);
                                    //    gpTest2.Transform(inverseYAxis);
                                    //}
                                    //gpTest2.Transform(renderingMatrixCopy);
                                    //currentGraphics.DrawPath(Pens.GreenYellow, gpTest2);
                                    // **************** end to remove

                                    GraphicsPath gp = new GraphicsPath();
                                    try
                                    {
                                        gp.AddString(unicode,
                                            fontFamily,
                                            (int)style,
                                            1,
                                            new RectangleF(0, 0, (float)bbox.Width, 1),
                                            StringFormat.GenericDefault);

                                        // flip letter's y-axis
                                        using (var inverseYAxis = MatrixExtensions.GetScaleMatrix(1, -1))
                                        {
                                            inverseYAxis.Translate(0, 1, MatrixOrder.Append);
                                            gp.Transform(inverseYAxis);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.Print(ex.Message);

                                        gp.StartFigure();
                                        gp.AddLine((float)bbox.BottomLeft.X, (float)bbox.BottomLeft.Y, (float)bbox.TopRight.X, (float)bbox.BottomLeft.Y);
                                        gp.AddLine((float)bbox.TopRight.X, (float)bbox.BottomLeft.Y, (float)bbox.TopRight.X, (float)bbox.TopRight.Y);
                                        gp.AddLine((float)bbox.TopRight.X, (float)bbox.TopRight.Y, (float)bbox.BottomLeft.X, (float)bbox.TopRight.Y);
                                        gp.CloseFigure();
                                    }

                                    gp.Transform(renderingMatrixCopy);

                                    using (var fillBrush = new SolidBrush(color))
                                    {
                                        currentGraphics.FillPath(fillBrush, gp);
                                    }
                                }
                            }
                        }
                    }

                    double tx, ty;
                    if (font.IsVertical)
                    {
                        var verticalFont = (IVerticalWritingSupported)font;
                        var displacement = verticalFont.GetDisplacementVector(code);
                        tx = 0;
                        ty = (displacement.Y * fontSize) + characterSpacing + wordSpacing;
                    }
                    else
                    {
                        tx = (boundingBox.Width * fontSize + characterSpacing + wordSpacing) * horizontalScaling;
                        ty = 0;
                    }

                    TextMatrix.Translate((float)tx, (float)ty);
                }
            }
        }

        private static bool TryLoadFontCollection(byte[] buffer, out PrivateFontCollection fontCollection)
        {
            //https://web.archive.org/web/20170313145219/https://blog.andreloker.de/post/2008/07/03/Load-a-font-from-disk-stream-or-byte-array.aspx
            fontCollection = null;
            // pin array so we can get its address
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            
            try
            {
                var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);
                fontCollection = new PrivateFontCollection();
                fontCollection.AddMemoryFont(ptr, buffer.Length);
                if (fontCollection.Families.Length == 0)
                {
                    fontCollection.Dispose();
                    return false;
                }
                return true;
            }
            catch
            {
                fontCollection?.Dispose();
                return false;
            }
            finally
            {
                // don't forget to unpin the array!
                handle.Free();

                // might need to do it on close...
                // https://stackoverflow.com/a/16375217/8621903
            }
        }

        private static string CleanFontName(string font)
        {
            if (font.Length > 7 && font[6].Equals('+'))
            {
                string subset = font.Substring(0, 6);
                if (subset.Equals(subset.ToUpper()))
                {
                    return font.Split('+')[1];
                }
            }

            return font;
        }

        public void SetCharacterSpacing(double spacing)
        {
            GetCurrentState().FontState.CharacterSpacing = spacing;
        }

        public void SetFontAndSize(NameToken font, double size)
        {
            var currentState = GetCurrentState();
            currentState.FontState.FontSize = size;
            currentState.FontState.FontName = font;
        }

        public void SetHorizontalScaling(double scale)
        {
            GetCurrentState().FontState.HorizontalScaling = scale;
        }
        #endregion

        #region TransformationMatrix
        /// <summary>
        /// Flip the matrix to be in system.drawing coordinates system
        /// </summary>
        /// <param name="matrix"></param>
        public void FlipYAxisTransform(Matrix matrix)
        {
            // flip transform to system.drawing

            matrix.Translate(0, -pageHeight, MatrixOrder.Append);
            matrix.Scale(pageScale, -pageScale, MatrixOrder.Append);
        }

        /// <summary>
        /// Flip the matrix to be in Pdf coordinates system
        /// </summary>
        /// <param name="matrix"></param>
        public void ReverseFlipYAxisTransform(Matrix matrix)
        {
            // flip back to pdf coordinate system
            matrix.Scale(1f / pageScale, -1f / pageScale, MatrixOrder.Append);
            matrix.Translate(0, pageHeight, MatrixOrder.Append);
        }

        [System.Diagnostics.Contracts.Pure]
        private static Matrix GetInitialMatrix(int rotation, MediaBox mediaBox)
        {
            // TODO: this is a bit of a hack because I don't understand matrices
            // TODO: use MediaBox (i.e. pageSize) or CropBox?

            /* 
             * There should be a single Affine Transform we can apply to any point resulting
             * from a content stream operation which will rotate the point and translate it back to
             * a point where the origin is in the page's lower left corner.
             *
             * For example this matrix represents a (clockwise) rotation and translation:
             * [  cos  sin  tx ]
             * [ -sin  cos  ty ]
             * [    0    0   1 ]
             * Warning: rotation is counter-clockwise here
             * 
             * The values of tx and ty are those required to move the origin back to the expected origin (lower-left).
             * The corresponding values should be:
             * Rotation:  0   90  180  270
             *       tx:  0    0    w    w
             *       ty:  0    h    h    0
             *
             * Where w and h are the page width and height after rotation.
            */

            float cos, sin;
            float dx = 0, dy = 0;
            switch (rotation)
            {
                case 0:
                    cos = 1;
                    sin = 0;
                    break;
                case 90:
                    cos = 0;
                    sin = 1;
                    dy = (float)mediaBox.Bounds.Height;
                    break;
                case 180:
                    cos = -1;
                    sin = 0;
                    dx = (float)mediaBox.Bounds.Width;
                    dy = (float)mediaBox.Bounds.Height;
                    break;
                case 270:
                    cos = 0;
                    sin = -1;
                    dx = (float)mediaBox.Bounds.Width;
                    break;
                default:
                    throw new InvalidOperationException($"Invalid value for page rotation: {rotation}.");
            }

            return new Matrix(cos, -sin,
                              sin, cos,
                              dx, dy);
        }

        public void ModifyCurrentTransformationMatrix(double[] value)
        {
            using (Matrix ctm = currentGraphics.Transform.Clone())
            {
                // flip back to pdf coordinate system
                ReverseFlipYAxisTransform(ctm);

                // update pdf matrix
                ctm.Multiply(MatrixExtensions.FromArray(value), MatrixOrder.Prepend);

                // flip transform to system.drawing
                FlipYAxisTransform(ctm);

                // update transformation matrix
                currentGraphics.Transform = ctm.Clone();
            }
        }
        #endregion

        public void SetNamedGraphicsState(NameToken stateName)
        {
            var currentGraphicsState = GetCurrentState();

            var state = currentPage.ExperimentalAccess.ResourceStore.GetExtendedGraphicsStateDictionary(stateName);

            if (state.TryGet(NameToken.Lw, currentPage.ExperimentalAccess.PdfTokenScanner, out NumericToken lwToken))
            {
                currentGraphicsState.LineWidth = lwToken.Data;
            }

            if (state.TryGet(NameToken.Lc, currentPage.ExperimentalAccess.PdfTokenScanner, out NumericToken lcToken))
            {
                currentGraphicsState.LineCap = ((LineCapStyle)lcToken.Int).ToSystemLineCap();
                currentGraphicsState.DashCap = ((LineCapStyle)lcToken.Int).ToSystemDashCap();
            }

            if (state.TryGet(NameToken.Lj, currentPage.ExperimentalAccess.PdfTokenScanner, out NumericToken ljToken))
            {
                currentGraphicsState.JoinStyle = ((LineJoinStyle)ljToken.Int).ToSystemLineJoin();
            }

            if (state.TryGet(NameToken.Font, currentPage.ExperimentalAccess.PdfTokenScanner, out ArrayToken fontArray) && fontArray.Length == 2
                && fontArray.Data[0] is IndirectReferenceToken fontReference && fontArray.Data[1] is NumericToken sizeToken)
            {
                currentGraphicsState.FontState.FromExtendedGraphicsState = true;
                currentGraphicsState.FontState.FontSize = (double)sizeToken.Data;
                activeExtendedGraphicsStateFont = currentPage.ExperimentalAccess.ResourceStore.GetFontDirectly(fontReference);
            }
        }
        #endregion
    }
}
