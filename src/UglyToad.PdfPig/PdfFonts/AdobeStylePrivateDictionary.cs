namespace UglyToad.PdfPig.PdfFonts
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Util;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Holds common properties between Type 1 and Compact Font Format private dictionaries.
    /// </summary>
    internal abstract class AdobeStylePrivateDictionary
    {
        /// <summary>
        /// Default value of <see cref="BlueScale"/>.
        /// </summary>
        public static readonly decimal DefaultBlueScale = 0.039625m;

        /// <summary>
        /// Default value of <see cref="ExpansionFactor"/>.
        /// </summary>
        public static readonly decimal DefaultExpansionFactor = 0.06m;

        /// <summary>
        /// Default value of <see cref="BlueFuzz"/>.
        /// </summary>
        public const int DefaultBlueFuzz = 1;

        /// <summary>
        /// Default value of <see cref="BlueShift"/>.
        /// </summary>
        public const int DefaultBlueShift = 7;

        /// <summary>
        /// Default value of <see cref="LanguageGroup"/>.
        /// </summary>
        public const int DefaultLanguageGroup = 0;
        
        /// <summary>
        /// Required. An array containing an even number of integers.
        /// The first pair is the baseline overshoot position and the baseline.
        /// All following pairs describe top-zones.
        /// </summary>
        [NotNull]
        public IReadOnlyList<int> BlueValues { get; }

        /// <summary>
        /// Optional: Pairs of integers similar to <see cref="BlueValues"/>.
        /// These only describe bottom zones.
        /// </summary>
        [NotNull]
        public IReadOnlyList<int> OtherBlues { get; }

        /// <summary>
        /// Optional: Integer pairs similar to <see cref="BlueValues"/> however these
        /// are used to enforce consistency across a font family when there are small differences (&lt;1px) in
        /// font alignment.
        /// </summary>
        [NotNull]
        public IReadOnlyList<int> FamilyBlues { get; }

        /// <summary>
        /// Optional: Integer pairs similar to <see cref="OtherBlues"/> however these
        /// are used to enforce consistency across a font family with small differences 
        /// in alignment similarly to <see cref="FamilyBlues"/>.
        /// </summary>
        [NotNull]
        public IReadOnlyList<int> FamilyOtherBlues { get; }

        /// <summary>
        /// Optional: The point size at which overshoot suppression stops.
        /// The value is a related to the number of pixels tall that one character space unit will be
        /// before overshoot suppression is switched off. Overshoot suppression enforces features to snap
        /// to alignment zones when the point size is below that affected by this value.
        /// Default: 0.039625
        /// </summary>
        /// <example>
        /// A blue scale of 0.039625 switches overshoot suppression off at 10 points
        /// on a 300 dpi device using the formula (for 300 dpi):
        /// BlueScale = (pointsize - 0.49)/240
        /// For example, if you wish overshoot suppression to turn off at 11
        /// points on a 300-dpi device, you should set BlueScale to
        /// (11 − 0.49) ÷ 240 or 0.04379
        /// </example>
        public decimal BlueScale { get; }

        /// <summary>
        /// Optional: The character space distance beyond the flat position of alignment zones
        /// at which overshoot enforcement occurs.
        /// Default: 7
        /// </summary>
        public int BlueShift { get; }

        /// <summary>
        /// Optional: The number of character space units to extend an alignment zone
        /// on a horizontal stem.
        /// If the top or bottom of a horizontal stem is within BlueFuzz units outside a top-zone
        /// then the stem top/bottom is treated as if it were within the zone.
        /// Default: 1
        /// </summary>
        public int BlueFuzz { get; }

        /// <summary>
        /// Optional: The dominant width of horizontal stems vertically in character space units.
        /// </summary>
        public decimal? StandardHorizontalWidth { get; }

        /// <summary>
        /// Optional: The dominant width of vertical stems horizontally in character space units.
        /// </summary>
        public decimal? StandardVerticalWidth { get; }

        /// <summary>
        /// Optional: Up to 12 numbers with the most common widths for horizontal stems vertically in character space units.
        /// </summary>
        [NotNull]
        public IReadOnlyList<decimal> StemSnapHorizontalWidths { get; }

        /// <summary>
        /// Optional: Up to 12 numbers with the most common widths for vertical stems horizontally in character space units.
        /// </summary>
        [NotNull]
        public IReadOnlyList<decimal> StemSnapVerticalWidths { get; }

        /// <summary>
        /// Optional: At small sizes at low resolutions this controls whether bold characters should appear thicker using
        /// special techniques.
        /// </summary>
        public bool ForceBold { get; }

        /// <summary>
        /// Optional: Language group 0 includes Latin, Greek and Cyrillic as well as similar alphabets.
        /// Language group 1 includes Chinese, Japanese Kanji and Korean Hangul as well as similar alphabets.
        /// Default: 0
        /// </summary>
        public int LanguageGroup { get; }

        /// <summary>
        /// Optional: The limit for changing the size of a character bounding box for
        /// <see cref="LanguageGroup"/> 1 counters during font processing.
        /// </summary>
        public decimal ExpansionFactor { get; }
        
        /// <summary>
        /// Creates a new <see cref="AdobeStylePrivateDictionary"/>.
        /// </summary>
        /// <param name="builder">The builder used to gather property values.</param>
        protected AdobeStylePrivateDictionary([NotNull] BaseBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            BlueValues = builder.BlueValues ?? EmptyArray<int>.Instance;
            OtherBlues = builder.OtherBlues ?? EmptyArray<int>.Instance;
            FamilyBlues = builder.FamilyBlues ?? EmptyArray<int>.Instance;
            FamilyOtherBlues = builder.FamilyOtherBlues ?? EmptyArray<int>.Instance;
            BlueScale = builder.BlueScale ?? DefaultBlueScale;
            BlueFuzz = builder.BlueFuzz ?? DefaultBlueFuzz;
            BlueShift = builder.BlueShift ?? DefaultBlueShift;
            StandardHorizontalWidth = builder.StandardHorizontalWidth;
            StandardVerticalWidth = builder.StandardVerticalWidth;
            StemSnapHorizontalWidths = builder.StemSnapHorizontalWidths ?? EmptyArray<decimal>.Instance;
            StemSnapVerticalWidths = builder.StemSnapVerticalWidths ?? EmptyArray<decimal>.Instance;
            ForceBold = builder.ForceBold ?? false;
            LanguageGroup = builder.LanguageGroup ?? DefaultLanguageGroup;
            ExpansionFactor = builder.ExpansionFactor ?? DefaultExpansionFactor;
        }

        /// <summary>
        /// A mutable builder which can set any property of the private dictionary and performs no validation.
        /// </summary>
        public abstract class BaseBuilder
        {
            /// <summary>
            /// <see cref="AdobeStylePrivateDictionary.BlueValues"/>.
            /// </summary>
            public IReadOnlyList<int> BlueValues { get; set; }

            /// <summary>
            /// <see cref="AdobeStylePrivateDictionary.OtherBlues"/>.
            /// </summary>
            public IReadOnlyList<int> OtherBlues { get; set; }

            /// <summary>
            /// <see cref="AdobeStylePrivateDictionary.FamilyBlues"/>.
            /// </summary>
            public IReadOnlyList<int> FamilyBlues { get; set; }

            /// <summary>
            /// <see cref="AdobeStylePrivateDictionary.FamilyOtherBlues"/>.
            /// </summary>
            public IReadOnlyList<int> FamilyOtherBlues { get; set; }

            /// <summary>
            /// <see cref="AdobeStylePrivateDictionary.BlueScale"/>.
            /// </summary>
            public decimal? BlueScale { get; set; }

            /// <summary>
            /// <see cref="AdobeStylePrivateDictionary.BlueShift"/>.
            /// </summary>
            public int? BlueShift { get; set; }

            /// <summary>
            /// <see cref="AdobeStylePrivateDictionary.BlueFuzz"/>.
            /// </summary>
            public int? BlueFuzz { get; set; }

            /// <summary>
            /// <see cref="AdobeStylePrivateDictionary.StandardVerticalWidth"/>.
            /// </summary>
            public decimal? StandardHorizontalWidth { get; set; }

            /// <summary>
            /// <see cref="AdobeStylePrivateDictionary.StandardVerticalWidth"/>.
            /// </summary>
            public decimal? StandardVerticalWidth { get; set; }

            /// <summary>
            /// <see cref="AdobeStylePrivateDictionary.StemSnapHorizontalWidths"/>.
            /// </summary>
            public IReadOnlyList<decimal> StemSnapHorizontalWidths { get; set; }

            /// <summary>
            /// <see cref="AdobeStylePrivateDictionary.StemSnapVerticalWidths"/>.
            /// </summary>
            public IReadOnlyList<decimal> StemSnapVerticalWidths { get; set; }

            /// <summary>
            /// <see cref="AdobeStylePrivateDictionary.ForceBold"/>.
            /// </summary>
            public bool? ForceBold { get; set; }

            /// <summary>
            /// <see cref="AdobeStylePrivateDictionary.LanguageGroup"/>.
            /// </summary>
            public int? LanguageGroup { get; set; }
            
            /// <summary>
            /// <see cref="AdobeStylePrivateDictionary.ExpansionFactor"/>.
            /// </summary>
            public decimal? ExpansionFactor { get; set; }
        }
    }
}
