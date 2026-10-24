#nullable disable

namespace UglyToad.PdfPig.Graphics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Colors;
    using Colors.Icc;
    using Content;
    using Core;
    using Filters;
    using Geometry;
    using Geometry.ClipperLibrary;
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

        private readonly HashSet<Letter> occludedLetters = new HashSet<Letter>();

        /// <summary>
        /// The opaque paint reaching each letter so far, for letters not yet fully covered.
        /// </summary>
        private readonly Dictionary<Letter, LetterCoverage> letterCoverage = new Dictionary<Letter, LetterCoverage>();

        /// <summary>
        /// The axis-aligned box around both glyph boxes of each letter, in the same order as the letters.
        /// </summary>
        private readonly List<PdfRectangle> letterEnvelopes = new List<PdfRectangle>();

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

        /// <summary>
        /// The replacement text (/ActualText) the next glyph receives, or <see langword="null"/> when
        /// no replacement is in effect. It stands for the content of its whole marked-content
        /// sequence, nested sequences included, so the first glyph takes it and it becomes empty for
        /// the rest. See the PDF specification, 14.9.4 "Replacement text".
        /// </summary>
        private string activeActualText;

        /// <summary>
        /// The depth of the sequence that brought <see cref="activeActualText"/>, so that the
        /// matching end can clear it, or 0 when no replacement is in effect.
        /// </summary>
        private int actualTextDepth;

        /// <summary>
        /// The number of sequences open when the content stream being processed began. The stream
        /// can only end sequences it opened itself, so this is the floor it cannot end past.
        /// </summary>
        private int streamMarkedContentDepth;

        /// <summary>
        /// Counts text objects (BT operators), so a combining mark drawn by its own show text
        /// operation can still attach to a base letter drawn earlier in the same text object.
        /// </summary>
        private int textObjectSequence;

        /// <summary>
        /// The text object in which the most recent letter was drawn.
        /// </summary>
        private int lastLetterTextObjectSequence = -1;

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
            IIccProfile? outputIntentProfile,
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
                outputIntentProfile,
                parsingOptions)
        {
        }

        public override PageContent Process(int pageNumberCurrent, IReadOnlyList<IGraphicsStateOperation> operations)
        {
            PageNumber = pageNumberCurrent;
            CloneAllStates();

            ProcessOperations(operations);

            if (occludedLetters.Count > 0)
            {
                letters.RemoveAll(occludedLetters.Contains);
                RemoveOccludedLetters(markedContents);
            }

            return new PageContent(operations,
                letters,
                paths,
                images,
                markedContents,
                PdfScanner,
                FilterProvider,
                ResourceStore,
                ParsingOptions);
        }

        protected override void ProcessOperations(IReadOnlyList<IGraphicsStateOperation> operations)
        {
            var enclosingDepth = markedContentStack.Depth;
            var enclosingActualTextDepth = actualTextDepth;
            var enclosingStreamDepth = streamMarkedContentDepth;

            streamMarkedContentDepth = enclosingDepth;

            try
            {
                base.ProcessOperations(operations);
            }
            finally
            {
                streamMarkedContentDepth = enclosingStreamDepth;

                if (actualTextDepth > enclosingDepth)
                {
                    // The replacement text came from a sequence this stream opened, which it never
                    // closed. Nothing outside the stream is part of that sequence.
                    activeActualText = null;
                }

                actualTextDepth = activeActualText is null ? 0 : enclosingActualTextDepth;
            }
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
            if (activeActualText is not null)
            {
                // The active marked-content sequence specifies replacement text (/ActualText) for
                // extraction. It applies to the whole sequence, so assign it to the first glyph and
                // give the remaining glyphs an empty value to avoid duplicating the replaced text.
                unicode = activeActualText;
                activeActualText = string.Empty;
            }

            var transformedGlyphBounds = PerformantRectangleTransformer
                .Transform(renderingMatrix, textMatrix, transformationMatrix, characterBoundingBox.GlyphBounds);
            
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

                if ((attachTo.TextSequence == TextSequence || lastLetterTextObjectSequence == textObjectSequence)
                    && Diacritics.TryCombineDiacriticWithPreviousLetter(unicode, attachTo.Value, out var newLetter))
                {
                    // TODO: union of bounding boxes.
                    letters.Remove(attachTo);

                    letter = new Letter(
                        newLetter,
                        attachTo.BoundingBox,
                        attachTo.GlyphRectangleLoose,
                        attachTo.StartBaseLine,
                        attachTo.EndBaseLine,
                        attachTo.Width,
                        attachTo.FontSize,
                        attachTo.GetFont()!,
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
                var transformedPdfBounds = PerformantRectangleTransformer
                    .Transform(renderingMatrix,
                        textMatrix,
                        transformationMatrix,
                        new PdfRectangle(0, 0, characterBoundingBox.Width, UserSpaceUnit.PointMultiples));

                var looseBox = PerformantRectangleTransformer
                    .Transform(renderingMatrix,
                        textMatrix,
                        transformationMatrix,
                        new PdfRectangle(0,
                            font.GetDescent(),
                            characterBoundingBox.Width,
                            font.GetAscent()));

                letter = new Letter(
                    unicode,
                    isBboxValid ? transformedGlyphBounds : transformedPdfBounds,
                    looseBox,
                    transformedPdfBounds.BottomLeft,
                    transformedPdfBounds.BottomRight,
                    transformedPdfBounds.Width,
                    fontSize,
                    font,
                    currentState.FontState.TextRenderingMode,
                    currentState.CurrentStrokingColor!,
                    currentState.CurrentNonStrokingColor!,
                    pointSize,
                    TextSequence);
            }

            letters.Add(letter);
            lastLetterTextObjectSequence = textObjectSequence;
            markedContentStack.AddLetter(letter);
        }

        /// <inheritdoc/>
        public override void BeginText()
        {
            textObjectSequence++;
            base.BeginText();
        }

        protected override void RenderXObjectImage(XObjectContentRecord xObjectContentRecord)
        {
            images.Add(Union<XObjectContentRecord, InlineImage>.One(xObjectContentRecord));

            markedContentStack.AddXObject(xObjectContentRecord, PdfScanner, FilterProvider, ResourceStore, ParsingOptions);
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

            if (ParsingOptions.RemoveOccludedText && (CurrentPath.IsFilled || CurrentPath.IsStroked))
            {
                MarkOccludedLetters(currentState);
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

        private void RemoveOccludedLetters(IReadOnlyList<MarkedContentElement> elements)
        {
            foreach (var element in elements)
            {
                // The marked content stack hands each element its own letter list.
                if (element.Letters is List<Letter> elementLetters)
                {
                    elementLetters.RemoveAll(occludedLetters.Contains);
                }

                RemoveOccludedLetters(element.Children);
            }
        }

        /// <summary>
        /// Adds the opaque area the current path paints to the coverage of each earlier letter,
        /// and marks the letters it leaves fully covered.
        /// </summary>
        private void MarkOccludedLetters(CurrentGraphicsState currentState)
        {
            if (letters.Count == 0
                || currentState.BlendMode != BlendMode.Normal
                || currentState.SoftMask != null
                // Optional content may be hidden, so its paint cannot be trusted to cover anything.
                || markedContentStack.IsOpen(NameToken.Oc))
            {
                return;
            }

            var countsFill = CurrentPath.IsFilled
                && currentState.AlphaConstantNonStroking >= 1
                && currentState.CurrentNonStrokingColor is not PatternColor;

            // A dashed stroke leaves gaps, so only solid strokes count.
            var countsStroke = CurrentPath.IsStroked
                && currentState.AlphaConstantStroking >= 1
                && currentState.CurrentStrokingColor is not PatternColor
                && currentState.LineDashPattern.Array.Count == 0;

            var rawBounds = PdfSubpath.GetBoundingRectangle(CurrentPath);
            if ((!countsFill && !countsStroke) || rawBounds is null)
            {
                return;
            }

            var lineWidth = countsStroke ? currentState.LineWidth * currentState.CurrentTransformationMatrix.GetMinimumScale() : 0;
            var reach = new PdfRectangle(rawBounds.Value.Left - lineWidth, rawBounds.Value.Bottom - lineWidth,
                rawBounds.Value.Right + lineWidth, rawBounds.Value.Top + lineWidth);

            // Polygon work is only worth doing for paint that reaches a letter.
            var candidates = GetCandidateLetters(reach);
            if (candidates.Count == 0)
            {
                return;
            }

            // The path can be changed once painted, so take its polygons now and leave combining them until a letter needs it.
            var fillPolygons = countsFill ? CurrentPath.GetFillPolygons() : new List<List<ClipperIntPoint>>();
            var strokePolygons = countsStroke ? CurrentPath.GetStrokePolygons(lineWidth, currentState.CapStyle) : new List<List<ClipperIntPoint>>();
            var clipping = currentState.CurrentClippingPath;
            var paint = new Paint(fillPolygons, CurrentPath.GetClipperFillType(), strokePolygons,
                clipping?.GetFillPolygons(), clipping?.GetClipperFillType() ?? ClipperPolyFillType.NonZero);

            var clipBounds = clipping is null ? null : PdfSubpath.GetBoundingRectangle(clipping);
            var pieceBounds = new List<PdfRectangle>();
            foreach (var polygon in fillPolygons.Concat(strokePolygons))
            {
                var bounds = OcclusionExtensions.GetBounds(polygon);
                if (bounds.HasValue && (clipBounds is null || GetOverlapArea(bounds.Value, clipBounds.Value) > 0))
                {
                    pieceBounds.Add(bounds.Value);
                }
            }

            foreach (var index in candidates)
            {
                var letter = letters[index];
                var envelope = letterEnvelopes[index];

                // The overlap with each piece's bounds caps how much this paint can add to the letter's coverage.
                var bound = 0.0;
                foreach (var pieceBound in pieceBounds)
                {
                    bound += GetOverlapArea(envelope, pieceBound);
                }

                if (bound <= 0)
                {
                    continue;
                }

                if (!letterCoverage.TryGetValue(letter, out var coverage))
                {
                    coverage = new LetterCoverage();
                    letterCoverage[letter] = coverage;
                }

                coverage.Paints.Add(paint);
                coverage.UpperBound = Math.Min(coverage.UpperBound + bound, envelope.Area);
                if (coverage.UpperBound < GetAreaNeeded(letter))
                {
                    continue;
                }

                var envelopePolygon = new List<List<ClipperIntPoint>> { envelope.ToClipperPolygon().ToList() };
                var covered = new List<List<ClipperIntPoint>>();
                foreach (var reaching in coverage.Paints)
                {
                    covered = OcclusionExtensions.Union(covered, OcclusionExtensions.Intersect(reaching.Area, envelopePolygon));
                }

                // Without the font program the glyph box is the whole font bounding box, so also accept the loose box.
                if (IsCovered(covered, letter.BoundingBox) || IsCovered(covered, letter.GlyphRectangleLoose))
                {
                    occludedLetters.Add(letter);
                    letterCoverage.Remove(letter);
                }
                else
                {
                    coverage.Paints.Clear();
                    coverage.Paints.Add(new Paint(covered));
                    coverage.UpperBound = OcclusionExtensions.Area(covered) / (ClippingExtensions.Factor * ClippingExtensions.Factor);
                }
            }
        }

        private List<int> GetCandidateLetters(PdfRectangle reach)
        {
            // A combined diacritic replaces the last letter with one that has the same boxes, so trimming keeps the cache in step.
            if (letterEnvelopes.Count > letters.Count)
            {
                letterEnvelopes.RemoveRange(letters.Count, letterEnvelopes.Count - letters.Count);
            }

            while (letterEnvelopes.Count < letters.Count)
            {
                var letter = letters[letterEnvelopes.Count];
                letterEnvelopes.Add(GetEnvelope(letter.BoundingBox, letter.GlyphRectangleLoose));
            }

            var candidates = new List<int>();
            for (var i = 0; i < letters.Count; i++)
            {
                if (GetOverlapArea(letterEnvelopes[i], reach) > 0 && !occludedLetters.Contains(letters[i]))
                {
                    candidates.Add(i);
                }
            }

            return candidates;
        }

        /// <summary>
        /// The least coverage, in square points, that could leave one of the letter's boxes fully covered.
        /// </summary>
        private static double GetAreaNeeded(Letter letter)
        {
            var needed = double.MaxValue;
            foreach (var box in new[] { letter.BoundingBox, letter.GlyphRectangleLoose })
            {
                var area = GetArea(box);
                if (area > 0)
                {
                    needed = Math.Min(needed, area - GetTolerance(box));
                }
            }

            return needed;
        }

        private static bool IsCovered(List<List<ClipperIntPoint>> coverage, PdfRectangle box)
        {
            var area = GetArea(box);
            if (area == 0)
            {
                return false;
            }

            var scale = ClippingExtensions.Factor * ClippingExtensions.Factor;
            var covered = OcclusionExtensions.Area(OcclusionExtensions.Intersect(coverage, [box.ToClipperPolygon().ToList()])) / scale;
            return covered >= area - GetTolerance(box);
        }

        /// <summary>
        /// The area inside the box's corners. <see cref="PdfRectangle.Area"/> multiplies the side lengths, which overstates a skewed box.
        /// </summary>
        private static double GetArea(PdfRectangle box)
        {
            var corners = new[] { box.BottomLeft, box.BottomRight, box.TopRight, box.TopLeft };
            var twiceArea = 0.0;
            for (var i = 0; i < corners.Length; i++)
            {
                var next = corners[(i + 1) % corners.Length];
                twiceArea += corners[i].X * next.Y - next.X * corners[i].Y;
            }

            return Math.Abs(twiceArea) / 2;
        }

        /// <summary>
        /// A margin of 0.01 along the box edge absorbs rounding in the letter boxes.
        /// </summary>
        private static double GetTolerance(PdfRectangle box)
        {
            return 2 * (Math.Abs(box.Width) + Math.Abs(box.Height)) * 0.01;
        }

        private static double GetOverlapArea(PdfRectangle a, PdfRectangle b)
        {
            var width = Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left);
            var height = Math.Min(a.Top, b.Top) - Math.Max(a.Bottom, b.Bottom);
            return width > 0 && height > 0 ? width * height : 0;
        }

        private static PdfRectangle GetEnvelope(PdfRectangle a, PdfRectangle b)
        {
            var points = new[] { a.BottomLeft, a.BottomRight, a.TopLeft, a.TopRight, b.BottomLeft, b.BottomRight, b.TopLeft, b.TopRight };
            return new PdfRectangle(points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X), points.Max(p => p.Y));
        }

        /// <summary>
        /// The opaque area one fill or stroke painted, combined on first use.
        /// </summary>
        private sealed class Paint
        {
            private readonly List<List<ClipperIntPoint>> fill;
            private readonly ClipperPolyFillType fillType;
            private readonly List<List<ClipperIntPoint>> stroke;
            private readonly List<List<ClipperIntPoint>>? clip;
            private readonly ClipperPolyFillType clipType;
            private List<List<ClipperIntPoint>>? area;

            public Paint(List<List<ClipperIntPoint>> fill,
                ClipperPolyFillType fillType,
                List<List<ClipperIntPoint>> stroke,
                List<List<ClipperIntPoint>>? clip,
                ClipperPolyFillType clipType)
            {
                this.fill = fill;
                this.fillType = fillType;
                this.stroke = stroke;
                this.clip = clip;
                this.clipType = clipType;
            }

            public Paint(List<List<ClipperIntPoint>> area)
            {
                fill = stroke = new List<List<ClipperIntPoint>>();
                this.area = area;
            }

            public List<List<ClipperIntPoint>> Area => area ??= Combine();

            private List<List<ClipperIntPoint>> Combine()
            {
                var painted = OcclusionExtensions.Union(OcclusionExtensions.Union(fill, fillType),
                    OcclusionExtensions.Union(stroke, ClipperPolyFillType.NonZero));

                return clip is null || painted.Count == 0 ? painted : OcclusionExtensions.Intersect(painted, clip, clipType);
            }
        }

        private sealed class LetterCoverage
        {
            /// <summary>
            /// The paint that reaches the letter, kept until the exact coverage is worked out.
            /// </summary>
            public List<Paint> Paints { get; } = new List<Paint>();

            /// <summary>
            /// No less than the area of the letter's envelope covered so far, in square points.
            /// </summary>
            public double UpperBound { get; set; }
        }

        public override void ModifyClippingIntersect(FillingRule clippingRule)
        {
            if (CurrentPath is null)
            {
                return;
            }

            AddCurrentSubpath();
            CurrentPath.SetClipping(clippingRule);

            // Occlusion needs the clip to know how much of a fill is painted.
            if (ParsingOptions.ClipPaths || ParsingOptions.RemoveOccludedText)
            {
                var graphicsState = GetCurrentState();
                var currentClipping = graphicsState.CurrentClippingPath!;
                currentClipping.SetClipping(clippingRule);

                var newClippings = CurrentPath.Clip(currentClipping, ParsingOptions.Logger);
                if (newClippings is null)
                {
                    ParsingOptions.Logger.Warn("Empty clipping path found. Clipping path not updated.");
                }
                else
                {
                    graphicsState.CurrentClippingPath = newClippings;
                }
            }
        }

        protected override void ClipToRectangle(PdfRectangle rectangle, FillingRule clippingRule)
        {
            // https://github.com/apache/pdfbox/blob/f4bfe47de37f6fe69e8f98b164c3546facfd5e91/pdfbox/src/main/java/org/apache/pdfbox/contentstream/PDFStreamEngine.java#L611
            var graphicsState = GetCurrentState();
            rectangle = graphicsState.CurrentTransformationMatrix.Transform(rectangle).Normalise();
            var clip = rectangle.ToPdfPath();
            clip.SetClipping(clippingRule);

            var currentClipping = graphicsState.CurrentClippingPath!;
            currentClipping.SetClipping(clippingRule);
            var newClippings = clip.Clip(currentClipping, ParsingOptions.Logger);

            if (newClippings is null)
            {
                ParsingOptions.Logger.Warn("Empty clipping path found. Clipping path not updated.");
            }
            else
            {
                graphicsState.CurrentClippingPath = newClippings;
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
            if (propertyDictionaryName is not null)
            {
                var actual = ResourceStore.GetMarkedContentPropertiesDictionary(propertyDictionaryName);

                properties = actual ?? properties;
            }

            markedContentStack.Push(name, properties);

            // A marked-content sequence may provide replacement text for extraction via /ActualText
            // (PDF spec, 14.9.4 "Replacement text"). When opted in via ParsingOptions.UseActualText,
            // honour it so that content with no usable mapping in the font.
            if (actualTextDepth == 0 && ParsingOptions.UseActualText
                && properties is not null
                && properties.TryGet(NameToken.ActualText, PdfScanner, out IDataToken<string> actualTextToken))
            {
                // Strip soft hyphens (U+00AD): in replacement text these are conditional hyphens
                // marking potential line-break points and are meant to be invisible when not broken.
                // Keeping them would inject invisible characters mid-word and corrupt extracted text.
                activeActualText = actualTextToken.Data.Replace("\u00ad", string.Empty);
                actualTextDepth = markedContentStack.Depth;
            }
        }

        public override void EndMarkedContent()
        {
            if (markedContentStack.Depth <= streamMarkedContentDepth)
            {
                // An end with no beginning in this content stream. Sequences are balanced within a
                // stream (14.6), so this cannot be ending one an enclosing stream opened, and taking
                // it for that would drop the enclosing sequence's replacement text for everything
                // drawn after this stream returns.
                return;
            }

            if (markedContentStack.Depth == actualTextDepth)
            {
                activeActualText = null;
                actualTextDepth = 0;
            }

            var mc = markedContentStack.Pop(PdfScanner);
            if (mc is not null)
            {
                markedContents.Add(mc);
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
