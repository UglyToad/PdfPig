#nullable disable

namespace UglyToad.PdfPig.Graphics
{
    using System;
    using System.Collections.Generic;
    using Content;
    using Filters;
    using Geometry;
    using Operations;
    using Parser;
    using PdfFonts;
    using PdfPig.Core;
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
        private readonly List<Union<XObjectContentRecord, InlineImage>> images = new();

        /// <summary>
        /// Stores each marked content as it is encountered in the content stream.
        /// </summary>
        private readonly List<MarkedContentElement> markedContents = new List<MarkedContentElement>();

        private readonly MarkedContentStack markedContentStack = new MarkedContentStack();

        public PdfSubpath CurrentSubpath { get; private set; }

        public PdfPath CurrentPath { get; private set; }

        public ContentStreamProcessor(
            int pageNumber,
            IResourceStore resourceStore,
            IPdfTokenScanner pdfScanner,
            IPageContentParser pageContentParser,
            ILookupFilterProvider filterProvider,
            CropBox cropBox,
            UserSpaceUnit userSpaceUnit,
            PageRotationDegrees rotation,
            TransformationMatrix initialMatrix,
            ParsingOptions parsingOptions)
            : base(pageNumber,
                resourceStore,
                pdfScanner,
                pageContentParser,
                filterProvider,
                cropBox,
                userSpaceUnit,
                rotation,
                initialMatrix,
                parsingOptions)
        {
        }

        public override PageContent Process(int pageNumberCurrent, IReadOnlyList<IGraphicsStateOperation> operations)
        {
            PageNumber = pageNumberCurrent;
            CloneAllStates();

            ProcessOperations(operations);

            return new PageContent(operations,
                letters,
                paths,
                images,
                markedContents,
                PdfScanner,
                FilterProvider,
                ResourceStore);
        }

        public override void RenderGlyph(IFont font,
            CurrentGraphicsState currentState,
            double fontSize,
            double pointSize,
            int code,
            string unicode,
            long currentOffset,
            in TransformationMatrix renderingMatrix,
            in TransformationMatrix textMatrix,
            in TransformationMatrix transformationMatrix,
            CharacterBoundingBox characterBoundingBox)
        {
            var transformedGlyphBounds = PerformantRectangleTransformer
                .Transform(renderingMatrix, textMatrix, transformationMatrix, characterBoundingBox.GlyphBounds);
            
            var transformedPdfBounds = PerformantRectangleTransformer
                .Transform(renderingMatrix,
                    textMatrix,
                    transformationMatrix,
                    new PdfRectangle(0, 0, characterBoundingBox.Width, UserSpaceUnit.PointMultiples));

            if (ParsingOptions.ClipPaths)
            {
                var currentClipping = currentState.CurrentClippingPath;
                if (currentClipping?.IntersectsWith(transformedGlyphBounds) == false)
                {
                    return;
                }
            }

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

            // The bbox is assumed to be valid if the width or the height is greater than 0.
            // The whitespace letter can have a 0 height and still be a valid bbox.
            // This could change in the future (i.e. AND instead of OR).
            bool isBboxValid = transformedGlyphBounds.Width > double.Epsilon ||
                               transformedGlyphBounds.Height > double.Epsilon;

            // If we did not create a letter for a combined diacritic, create one here.
            if (letter is null)
            {
                letter = new Letter(
                    unicode,
                    isBboxValid ? transformedGlyphBounds : transformedPdfBounds,
                    transformedPdfBounds.BottomLeft,
                    transformedPdfBounds.BottomRight,
                    transformedPdfBounds.Width,
                    fontSize,
                    font.Details,
                    currentState.FontState.TextRenderingMode,
                    currentState.CurrentStrokingColor!,
                    currentState.CurrentNonStrokingColor!,
                    pointSize,
                    TextSequence);
            }

            letters.Add(letter);

            markedContentStack.AddLetter(letter);
        }

        protected override void RenderXObjectImage(XObjectContentRecord xObjectContentRecord)
        {
            images.Add(Union<XObjectContentRecord, InlineImage>.One(xObjectContentRecord));

            markedContentStack.AddXObject(xObjectContentRecord, PdfScanner, FilterProvider, ResourceStore);
        }

        public override void BeginSubpath()
        {
            if (CurrentPath is null)
            {
                CurrentPath = new PdfPath();
            }

            AddCurrentSubpath();
            CurrentSubpath = new PdfSubpath();
        }

        public override PdfPoint? CloseSubpath()
        {
            if (CurrentSubpath is null)
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

        private void AddCurrentSubpath()
        {
            if (CurrentSubpath is null)
            {
                return;
            }

            CurrentPath.Add(CurrentSubpath);
            CurrentSubpath = null;
        }

        public override void StrokePath(bool close)
        {
            if (CurrentPath is null)
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
            if (CurrentPath is null)
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
            if (CurrentPath is null)
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
            if (CurrentSubpath is null)
            {
                return;
            }

            var controlPoint2 = CurrentTransformationMatrix.Transform(new PdfPoint(x2, y2));
            var end = CurrentTransformationMatrix.Transform(new PdfPoint(x3, y3));

            CurrentSubpath.BezierCurveTo(CurrentPosition.X,
                CurrentPosition.Y,
                controlPoint2.X,
                controlPoint2.Y,
                end.X,
                end.Y);
            CurrentPosition = end;
        }

        public override void BezierCurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            if (CurrentSubpath is null)
            {
                return;
            }

            var controlPoint1 = CurrentTransformationMatrix.Transform(new PdfPoint(x1, y1));
            var controlPoint2 = CurrentTransformationMatrix.Transform(new PdfPoint(x2, y2));
            var end = CurrentTransformationMatrix.Transform(new PdfPoint(x3, y3));

            CurrentSubpath.BezierCurveTo(controlPoint1.X,
                controlPoint1.Y,
                controlPoint2.X,
                controlPoint2.Y,
                end.X,
                end.Y);
            CurrentPosition = end;
        }

        public override void LineTo(double x, double y)
        {
            if (CurrentSubpath is null)
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
            if (CurrentPath is null)
            {
                return;
            }

            AddCurrentSubpath();

            if (CurrentPath.IsClipping)
            {
                if (!ParsingOptions.ClipPaths)
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

            if (CurrentPath!.IsClipping)
            {
                EndPath();
                return;
            }

            var currentState = GetCurrentState();
            if (CurrentPath.IsStroked)
            {
                CurrentPath.SetStrokeDetails(currentState);
            }

            if (CurrentPath.IsFilled)
            {
                CurrentPath.SetFillDetails(currentState);
            }

            if (ParsingOptions.ClipPaths)
            {
                var clippedPath = currentState.CurrentClippingPath.Clip(CurrentPath, ParsingOptions.Logger);
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
            if (CurrentPath is null)
            {
                return;
            }

            AddCurrentSubpath();
            CurrentPath.SetClipping(clippingRule);

            if (ParsingOptions.ClipPaths)
            {
                var currentClipping = GetCurrentState().CurrentClippingPath!;
                currentClipping.SetClipping(clippingRule);

                var newClippings = CurrentPath.Clip(currentClipping, ParsingOptions.Logger);
                if (newClippings is null)
                {
                    ParsingOptions.Logger.Warn("Empty clipping path found. Clipping path not updated.");
                }
                else
                {
                    GetCurrentState().CurrentClippingPath = newClippings;
                }
            }
        }

        protected override void RenderInlineImage(InlineImage inlineImage)
        {
            images.Add(Union<XObjectContentRecord, InlineImage>.Two(inlineImage));

            markedContentStack.AddImage(inlineImage);
        }

        public override void BeginMarkedContent(NameToken name,
            NameToken propertyDictionaryName,
            DictionaryToken properties)
        {
            if (propertyDictionaryName != null)
            {
                var actual = ResourceStore.GetMarkedContentPropertiesDictionary(propertyDictionaryName);

                properties = actual ?? properties;
            }

            markedContentStack.Push(name, properties);
        }

        public override void EndMarkedContent()
        {
            if (markedContentStack.CanPop)
            {
                var mc = markedContentStack.Pop(PdfScanner);
                if (mc != null)
                {
                    markedContents.Add(mc);
                }
            }
        }

        public override void PaintShading(NameToken shadingName)
        {
            // We do nothing for the moment
            // Do the following if you need to access the shading:
            // var shading = ResourceStore.GetShading(shadingName);
        }
    }
}