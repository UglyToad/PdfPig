namespace UglyToad.PdfPig.Graphics
{
    using Colors;
    using Content;
    using Filters;
    using Geometry;
    using Logging;
    using Operations;
    using Parser;
    using PdfFonts;
    using PdfPig.Core;
    using System;
    using System.Collections.Generic;
    using Tokenization.Scanner;
    using Tokens;
    using Util;
    using static PdfPig.Core.PdfSubpath;

    internal class ContentStreamProcessor : BaseStreamProcessor<PageContent>
    {
        /// <summary>
        /// Stores each letter as it is encountered in the content stream.
        /// </summary>
        private readonly List<Letter> letters = new List<Letter>();

        /// <summary>
        /// Stores each path as it is encountered in the content stream.
        /// </summary>
        private readonly List<PdfPath> paths = new List<PdfPath>();

        /// <summary>
        /// Stores a link to each image (either inline or XObject) as it is encountered in the content stream.
        /// </summary>
        private readonly List<Union<XObjectContentRecord, InlineImage>> images = new List<Union<XObjectContentRecord, InlineImage>>();

        /// <summary>
        /// Stores each marked content as it is encountered in the content stream.
        /// </summary>
        private readonly List<MarkedContentElement> markedContents = new List<MarkedContentElement>();

        private readonly MarkedContentStack markedContentStack = new MarkedContentStack();

        public PdfSubpath CurrentSubpath { get; private set; }

        public PdfPath CurrentPath { get; private set; }

        internal ContentStreamProcessor(PdfRectangle cropBox, IResourceStore resourceStore, UserSpaceUnit userSpaceUnit, PageRotationDegrees rotation,
            IPdfTokenScanner pdfScanner,
            IPageContentParser pageContentParser,
            ILookupFilterProvider filterProvider,
            InternalParsingOptions parsingOptions)
            : base(cropBox, resourceStore, userSpaceUnit, rotation, pdfScanner, pageContentParser, filterProvider, pageSize, parsingOptions)
        { }

        public override PageContent Process(int pageNumberCurrent, IReadOnlyList<IGraphicsStateOperation> operations)
        {
            PageNumber = pageNumberCurrent;
            CloneAllStates();

            ProcessOperations(operations);

            return new PageContent(operations, letters, paths, images, markedContents, pdfScanner, filterProvider, resourceStore);
        }

        public override void ShowGlyph(IFont font, TextRenderingMode textRenderingMode, IColor strokingColor, IColor nonStrokingColor,
            double fontSize, double pointSize, int code, string unicode, long currentOffset, TransformationMatrix renderingMatrix,
            TransformationMatrix textMatrix, TransformationMatrix transformationMatrix, CharacterBoundingBox characterBoundingBox)
        {
            var transformedGlyphBounds = PerformantRectangleTransformer
                .Transform(renderingMatrix, textMatrix, transformationMatrix, characterBoundingBox.GlyphBounds);

            var transformedPdfBounds = PerformantRectangleTransformer
                .Transform(renderingMatrix, textMatrix, transformationMatrix, new PdfRectangle(0, 0, characterBoundingBox.Width, 0));

            Letter letter = null;
            if (Diacritics.IsInCombiningDiacriticRange(unicode) && currentOffset > 0 && letters.Count > 0)
            {
                var attachTo = letters[letters.Count - 1];

                if (attachTo.TextSequence == TextSequence
                    && Diacritics.TryCombineDiacriticWithPreviousLetter(unicode, attachTo.Value, out var newLetter))
                {
                    // TODO: union of bounding boxes.
                    letters.Remove(attachTo);

                    letter = new Letter(
                        newLetter,
                        attachTo.GlyphRectangle,
                        attachTo.StartBaseLine,
                        attachTo.EndBaseLine,
                        attachTo.Width,
                        attachTo.FontSize,
                        attachTo.Font,
                        attachTo.RenderingMode,
                        attachTo.StrokeColor,
                        attachTo.FillColor,
                        attachTo.PointSize,
                        attachTo.TextSequence);
                }
            }

            // If we did not create a letter for a combined diacritic, create one here.
            if (letter == null)
            {
                letter = new Letter(
                    unicode,
                    transformedGlyphBounds,
                    transformedPdfBounds.BottomLeft,
                    transformedPdfBounds.BottomRight,
                    transformedPdfBounds.Width,
                    fontSize,
                    font.Details,
                    textRenderingMode,
                    strokingColor,
                    nonStrokingColor,
                    pointSize,
                    TextSequence);
            }

            letters.Add(letter);
            markedContentStack.AddLetter(letter);
        }

        public override void ShowXObjectImage(XObjectContentRecord xObjectContentRecord)
        {
            images.Add(Union<XObjectContentRecord, InlineImage>.One(xObjectContentRecord));
            markedContentStack.AddXObject(xObjectContentRecord, pdfScanner, filterProvider, resourceStore);
        }

        public override void BeginSubpath()
        {
            if (CurrentPath == null)
            {
                CurrentPath = new PdfPath();
            }

            AddCurrentSubpath();
            CurrentSubpath = new PdfSubpath();
        }

        public override PdfPoint? CloseSubpath()
        {
            if (CurrentSubpath == null)
            {
                return null;
            }

            PdfPoint point;
            if (CurrentSubpath.Commands[0] is Move move)
            {
                point = move.Location;
            }
            else
            {
                throw new ArgumentException("CloseSubpath(): first command not Move.");
            }

            CurrentSubpath.CloseSubpath();
            AddCurrentSubpath();
            return point;
        }

        public void AddCurrentSubpath()
        {
            if (CurrentSubpath == null)
            {
                return;
            }

            CurrentPath.Add(CurrentSubpath);
            CurrentSubpath = null;
        }

        public override void StrokePath(bool close)
        {
            if (CurrentPath == null)
            {
                return;
            }

            CurrentPath.SetStroked();

            if (close)
            {
                CurrentSubpath?.CloseSubpath();
            }

            ClosePath();
        }

        public override void FillPath(FillingRule fillingRule, bool close)
        {
            if (CurrentPath == null)
            {
                return;
            }

            CurrentPath.SetFilled(fillingRule);

            if (close)
            {
                CurrentSubpath?.CloseSubpath();
            }

            ClosePath();
        }

        public override void FillStrokePath(FillingRule fillingRule, bool close)
        {
            if (CurrentPath == null)
            {
                return;
            }

            CurrentPath.SetFilled(fillingRule);
            CurrentPath.SetStroked();

            if (close)
            {
                CurrentSubpath?.CloseSubpath();
            }

            ClosePath();
        }

        public override void MoveTo(double x, double y)
        {
            BeginSubpath();
            var point = CurrentTransformationMatrix.Transform(new PdfPoint(x, y));
            CurrentPosition = point;
            CurrentSubpath.MoveTo(point.X, point.Y);
        }

        public override void BezierCurveTo(double x2, double y2, double x3, double y3)
        {
            if (CurrentSubpath == null)
            {
                return;
            }

            var controlPoint2 = CurrentTransformationMatrix.Transform(new PdfPoint(x2, y2));
            var end = CurrentTransformationMatrix.Transform(new PdfPoint(x3, y3));

            CurrentSubpath.BezierCurveTo(CurrentPosition.X, CurrentPosition.Y, controlPoint2.X, controlPoint2.Y, end.X, end.Y);
            CurrentPosition = end;
        }

        public override void BezierCurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            if (CurrentSubpath == null)
            {
                return;
            }

            var controlPoint1 = CurrentTransformationMatrix.Transform(new PdfPoint(x1, y1));
            var controlPoint2 = CurrentTransformationMatrix.Transform(new PdfPoint(x2, y2));
            var end = CurrentTransformationMatrix.Transform(new PdfPoint(x3, y3));

            CurrentSubpath.BezierCurveTo(controlPoint1.X, controlPoint1.Y, controlPoint2.X, controlPoint2.Y, end.X, end.Y);
            CurrentPosition = end;
        }

        public override void LineTo(double x, double y)
        {
            if (CurrentSubpath == null)
            {
                return;
            }

            var endPoint = CurrentTransformationMatrix.Transform(new PdfPoint(x, y));

            CurrentSubpath.LineTo(endPoint.X, endPoint.Y);
            CurrentPosition = endPoint;
        }

        public override void Rectangle(double x, double y, double width, double height)
        {
            BeginSubpath();
            var lowerLeft = CurrentTransformationMatrix.Transform(new PdfPoint(x, y));
            var upperRight = CurrentTransformationMatrix.Transform(new PdfPoint(x + width, y + height));

            CurrentSubpath.Rectangle(lowerLeft.X, lowerLeft.Y, upperRight.X - lowerLeft.X, upperRight.Y - lowerLeft.Y);
            AddCurrentSubpath();
        }

        public override void EndPath()
        {
            if (CurrentPath == null)
            {
                return;
            }

            AddCurrentSubpath();

            if (CurrentPath.IsClipping)
            {
                if (!parsingOptions.ClipPaths)
                {
                    // if we don't clip paths, add clipping path to paths
                    paths.Add(CurrentPath);
                    markedContentStack.AddPath(CurrentPath);
                }
                CurrentPath = null;
                return;
            }

            paths.Add(CurrentPath);
            markedContentStack.AddPath(CurrentPath);
            CurrentPath = null;
        }

        public override void ClosePath()
        {
            AddCurrentSubpath();

            if (CurrentPath.IsClipping)
            {
                EndPath();
                return;
            }

            var currentState = GetCurrentState();
            if (CurrentPath.IsStroked)
            {
                CurrentPath.LineDashPattern = currentState.LineDashPattern;
                CurrentPath.StrokeColor = currentState.CurrentStrokingColor;
                CurrentPath.LineWidth = currentState.LineWidth;
                CurrentPath.LineCapStyle = currentState.CapStyle;
                CurrentPath.LineJoinStyle = currentState.JoinStyle;
            }

            if (CurrentPath.IsFilled)
            {
                CurrentPath.FillColor = currentState.CurrentNonStrokingColor;
            }

            if (parsingOptions.ClipPaths)
            {
                var clippedPath = currentState.CurrentClippingPath.Clip(CurrentPath, parsingOptions.Logger);
                if (clippedPath != null)
                {
                    paths.Add(clippedPath);
                    markedContentStack.AddPath(clippedPath);
                }
            }
            else
            {
                paths.Add(CurrentPath);
                markedContentStack.AddPath(CurrentPath);
            }

            CurrentPath = null;
        }

        public override void ModifyClippingIntersect(FillingRule clippingRule)
        {
            if (CurrentPath == null)
            {
                return;
            }

            AddCurrentSubpath();
            CurrentPath.SetClipping(clippingRule);

            if (parsingOptions.ClipPaths)
            {
                var currentClipping = GetCurrentState().CurrentClippingPath;
                currentClipping.SetClipping(clippingRule);

                var newClippings = CurrentPath.Clip(currentClipping, parsingOptions.Logger);
                if (newClippings == null)
                {
                    parsingOptions.Logger.Warn("Empty clipping path found. Clipping path not updated.");
                }
                else
                {
                    GetCurrentState().CurrentClippingPath = newClippings;
                }
            }
        }

        public override void ShowInlineImage(InlineImage inlineImage)
        {
            images.Add(Union<XObjectContentRecord, InlineImage>.Two(inlineImage));
            markedContentStack.AddImage(inlineImage);
        }

        public override void BeginMarkedContent(NameToken name, NameToken propertyDictionaryName, DictionaryToken properties)
        {
            if (propertyDictionaryName != null)
            {
                var actual = resourceStore.GetMarkedContentPropertiesDictionary(propertyDictionaryName);

                properties = actual ?? properties;
            }

            markedContentStack.Push(name, properties);
        }

        public override void EndMarkedContent()
        {
            if (markedContentStack.CanPop)
            {
                var mc = markedContentStack.Pop(pdfScanner);
                if (mc != null)
                {
                    markedContents.Add(mc);
                }
            }
        }
    }
}
