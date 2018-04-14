namespace UglyToad.PdfPig.Fonts.TrueType.Glyphs
{
    using System;

    [Flags]
    internal enum CompositeGlyphFlags : ushort
    {
        /// <summary>
        /// If set arguments are words, otherwise they are bytes.
        /// </summary>
        Args1And2AreWords = 1,
        /// <summary>
        /// If set arguments are x y offset values, otherwise they are points.
        /// </summary>
        ArgsAreXAndYValues = 1 << 1,
        /// <summary>
        /// If arguments are x y offset values and this is set then the values are rounded to the closest grid lines before addition to the glyph.
        /// </summary>
        RoundXAndYToGrid = 1 << 2,
        /// <summary>
        /// If set the scale value is read in 2.14 format (between -2 to &lt; 2) and the glyph is scaled before grid-fitting. Otherwise scale is 1.
        /// </summary>
        WeHaveAScale = 1 << 3,
        /// <summary>
        /// Reserved for future use, should be set to 0.
        /// </summary>
        Reserved = 1 << 4,
        /// <summary>
        /// Indicates that there is a glyph following the current one.
        /// </summary>
        MoreComponents = 1 << 5,
        /// <summary>
        /// Indicates that X is scaled differently to Y.
        /// </summary>
        WeHaveAnXAndYScale = 1 << 6,
        /// <summary>
        /// Indicates that there is a 2 by 2 transformation used to scale the component.
        /// </summary>
        WeHaveATwoByTwo = 1 << 7,
        /// <summary>
        /// Indicates that there are instructions for the composite character following the last component.
        /// </summary>
        WeHaveInstructions = 1 << 8,
        /// <summary>
        /// If set this forces advance width and left side bearing for the composite to be equal to those from the original glyph.
        /// </summary>
        UseMyMetrics = 1 << 9
    }
}