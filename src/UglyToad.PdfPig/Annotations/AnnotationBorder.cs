namespace UglyToad.PdfPig.Annotations
{
    using System.Collections.Generic;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// A border for a PDF <see cref="Annotation"/> object.
    /// </summary>
    public class AnnotationBorder
    {
        /// <summary>
        /// The default border style if not specified.
        /// </summary>
        public static AnnotationBorder Default { get; } = new AnnotationBorder(0, 0, 1, null);

        /// <summary>
        /// The horizontal corner radius in user space units.
        /// </summary>
        public decimal HorizontalCornerRadius { get; }

        /// <summary>
        /// The vertical corner radius in user space units.
        /// </summary>
        public decimal VerticalCornerRadius { get; }

        /// <summary>
        /// The width of the border in user space units.
        /// </summary>
        public decimal BorderWidth { get; }

        /// <summary>
        /// The dash pattern for the border lines if provided. Optional.
        /// </summary>
        [CanBeNull]
        public IReadOnlyList<decimal> LineDashPattern { get; }

        /// <summary>
        /// Create a new <see cref="AnnotationBorder"/>.
        /// </summary>
        public AnnotationBorder(decimal horizontalCornerRadius, decimal verticalCornerRadius, decimal borderWidth, IReadOnlyList<decimal> lineDashPattern)
        {
            HorizontalCornerRadius = horizontalCornerRadius;
            VerticalCornerRadius = verticalCornerRadius;
            BorderWidth = borderWidth;
            LineDashPattern = lineDashPattern;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{HorizontalCornerRadius} {VerticalCornerRadius} {BorderWidth}";
        }
    }
}