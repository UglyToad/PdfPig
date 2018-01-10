// ReSharper disable RedundantDefaultMemberInitializer
namespace UglyToad.PdfPig.Graphics
{
    using Core;
    using Cos;
    using PdfPig.Core;

    /// <summary>
    /// The current state of text related parameters for a content stream.
    /// </summary>
    internal class CurrentFontState : IDeepCloneable<CurrentFontState>
    {
        /// <summary>
        /// A value in unscaled text space units which is added to the horizontal (or vertical if in vertical writing mode)
        /// glyph displacement.
        /// </summary>
        /// <remarks>
        /// In horizontal writing mode a positive value will expand the distance between letters/glyphs.
        /// Default value 0.
        /// </remarks>
        public decimal CharacterSpacing { get; set; } = 0;

        /// <summary>
        /// As for <see cref="CharacterSpacing"/> but applies only for the space character (32).
        /// </summary>
        /// <remarks>
        /// Default value 0.
        /// </remarks>
        public decimal WordSpacing { get; set; } = 0;

        /// <summary>
        /// Adjusts the width of glyphs/letters by stretching (or compressing) them horizontally.
        /// Value is a percentage of the normal width.
        /// </summary>
        public decimal HorizontalScaling { get; set; } = 100;

        /// <summary>
        /// The vertical distance in unscaled text space units between the baselines of lines of text.
        /// </summary>
        public decimal Leading { get; set; }

        public CosName FontName { get; set; }

        public decimal FontSize { get; set; }

        /// <summary>
        /// The <see cref="RenderingMode"/> for glyph outlines.
        /// </summary>
        /// <remarks>
        /// When the rendering mode requires filling the current non-stroking color in the state is used.<br/>
        /// When the rendering mode requires stroking the current stroking color in the state is used.<br/>
        /// The rendering mode has no impact on Type 3 fonts.
        /// </remarks>
        public RenderingMode RenderingMode { get; set; } = RenderingMode.Fill;

        /// <summary>
        /// The distance in unscaled text space units to move the default baseline either up or down.
        /// </summary>
        /// <remarks>
        /// Always applies to the vertical coordinate irrespective or writing mode.
        /// </remarks>
        public decimal Rise { get; set; }

        /// <summary>
        /// Are all glpyhs in a text object treated as a single elementary object for the purpose of the transparent imaging model?
        /// </summary>
        public bool Knockout { get; set; }

        public CurrentFontState DeepClone()
        {
            return new CurrentFontState
            {
                CharacterSpacing = CharacterSpacing,
                RenderingMode = RenderingMode,
                Rise = Rise,
                Leading = Leading,
                WordSpacing = WordSpacing,
                FontName = FontName,
                FontSize = FontSize,
                HorizontalScaling = HorizontalScaling,
                Knockout = Knockout
            };
        }
    }
}