namespace UglyToad.PdfPig.Writer
{
    using UglyToad.PdfPig.Actions;
    using UglyToad.PdfPig.Annotations;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// Represents a link annotation that can be added to a PDF page.
    /// Link annotations provide clickable areas that can trigger actions such as navigating to another page or opening a URL.
    /// </summary>
    public sealed class LinkAnnotation
    {
        /// <summary>
        /// Gets the border style for the link annotation.
        /// This is overwritten by the <see cref="AnnotationBorder"/> if both are provided.
        /// </summary>
        public AnnotationBorder? AnnotationBorder { get; }

        /// <summary>
        /// Gets the border style for the link annotation.
        /// </summary>
        public BorderStyle? Border { get; }

        /// <summary>
        /// Gets the width of the border for the link annotation.
        /// </summary>
        public int? BorderWidth { get; }

        /// <summary>
        /// Gets the rectangle defining the location and size of the link annotation on the page.
        /// </summary>
        public PdfRectangle Rect { get; }

        /// <summary>
        /// Gets the quadrilaterals defining the clickable regions of the link.
        /// These are typically used to define precise clickable areas that may not be rectangular.
        /// </summary>
        public IReadOnlyList<QuadPointsQuadrilateral> QuadPoints { get; }

        /// <summary>
        /// Gets the action to be performed when the link is activated.
        /// </summary>
        public PdfAction Action { get; }

        /// <summary>
        /// Specifies the border style for a link annotation.
        /// </summary>
        public enum BorderStyle
        {
            /// <summary>
            /// A solid border.
            /// </summary>
            Solid,

            /// <summary>
            /// A dashed border.
            /// </summary>
            Dashed,

            /// <summary>
            /// A simulated embossed border that appears to be raised above the surface of the page.
            /// </summary>
            Beveled,

            /// <summary>
            /// A simulated engraved border that appears to be recessed below the surface of the page.
            /// </summary>
            Inset,

            /// <summary>
            /// An underline border drawn along the bottom of the annotation rectangle.
            /// </summary>
            Underline,
        }

        /// <summary>
        /// Creates a new <see cref="LinkAnnotation"/> instance.
        /// </summary>
        /// <param name="action">The action to be performed when the link is activated.</param>
        /// <param name="rect">The rectangle defining the location and size of the link on the page.</param>
        /// <param name="annotationBorder">The border style for the link annotation. Optional, overwritten by <see cref="Border"/>.</param>
        /// <param name="borderStyle">The border style for the link annotation. Optional.</param>
        /// <param name="borderWidth">The width of the border for the link annotation. Optional.</param>
        /// <param name="quadPoints">The quadrilaterals defining the clickable regions. Optional.</param>
        public LinkAnnotation(
            PdfAction action,
            PdfRectangle rect,
            AnnotationBorder? annotationBorder = null,
            BorderStyle? borderStyle = null,
            int? borderWidth = null,
            IReadOnlyList<QuadPointsQuadrilateral>? quadPoints = null)
        {
            Action = action;
            Rect = rect;
            AnnotationBorder = annotationBorder;
            Border = borderStyle;
            BorderWidth = borderWidth;
            QuadPoints = quadPoints ?? new List<QuadPointsQuadrilateral>();
        }

        /// <summary>
        /// Converts this link annotation to a PDF dictionary token representation.
        /// </summary>
        /// <returns>A <see cref="DictionaryToken"/> representing this link annotation in PDF format.</returns>
        public DictionaryToken ToToken()
        {
            var dict = new Dictionary<NameToken, IToken>
            {
                [NameToken.Type] = NameToken.Annot,
                [NameToken.Subtype] = NameToken.Link,
                [NameToken.Rect] = new ArrayToken([
                    new NumericToken(Rect.BottomLeft.X),
                    new NumericToken(Rect.BottomLeft.Y),
                    new NumericToken(Rect.TopRight.X),
                    new NumericToken(Rect.TopRight.Y)
                ]),
            };

            if (QuadPoints.Count > 0)
            {
                var quadPointsArray = new List<NumericToken>();
                foreach (var quad in QuadPoints)
                {
                    foreach (var point in quad.Points)
                    {
                        quadPointsArray.Add(new NumericToken(point.X));
                        quadPointsArray.Add(new NumericToken(point.Y));
                    }
                }

                dict.Add(NameToken.Quadpoints, new ArrayToken(quadPointsArray));
            }

            if (AnnotationBorder != null)
            {
                var borderArray = new List<IToken>
                {
                    new NumericToken(AnnotationBorder.HorizontalCornerRadius),
                    new NumericToken(AnnotationBorder.VerticalCornerRadius),
                    new NumericToken(AnnotationBorder.BorderWidth),
                };

                if (AnnotationBorder.LineDashPattern != null && AnnotationBorder.LineDashPattern.Count > 0)
                {
                    var dashArray = new List<NumericToken>();
                    foreach (var dash in AnnotationBorder.LineDashPattern)
                    {
                        dashArray.Add(new NumericToken(dash));
                    }
                    borderArray.Add(new ArrayToken(dashArray));
                }
                dict.Add(NameToken.Border, new ArrayToken(borderArray));
            }

            if (Border != null)
            {
                dict.Add(NameToken.Bs, new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    [NameToken.S] = Border switch
                    {
                        BorderStyle.Solid => NameToken.S,
                        BorderStyle.Dashed => NameToken.D,
                        BorderStyle.Beveled => NameToken.B,
                        BorderStyle.Inset => NameToken.I,
                        BorderStyle.Underline => NameToken.U,
                        _ => NameToken.S,
                    },
                    [NameToken.W] = new NumericToken(BorderWidth ?? 1)
                }));
            }



            return new DictionaryToken(dict);
        }
    }
}