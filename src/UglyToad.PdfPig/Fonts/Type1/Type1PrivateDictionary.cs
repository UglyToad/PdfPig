namespace UglyToad.PdfPig.Fonts.Type1
{
    using System;
    using System.Collections.Generic;
    using Parser;

    /// <summary>
    /// The Private dictionary for a Type 1 font contains hints that apply across all characters in the font. These hints
    /// help preserve properties of character outline shapes when rendered at smaller sizes and lower resolutions.
    /// These hints help ensure that the shape is as close as possible to the original design even where the character
    /// must be represented in few pixels.
    /// Note that subroutines are also defined in the private dictionary however for the purposes of this API they are
    /// stored on the parent <see cref="Type1FontProgram"/>.
    /// </summary>
    internal class Type1PrivateDictionary
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
        /// Optional: Uniquely identifies this font.
        /// </summary>
        public int? UniqueId { get; set; }

        /// <summary>
        /// Required. An array containing an even number of integers.
        /// The first pair is the baseline overshoot position and the baseline.
        /// All following pairs describe top-zones.
        /// </summary>
        public IReadOnlyList<int> BlueValues { get; }

        /// <summary>
        /// Optional: Pairs of integers similar to <see cref="BlueValues"/>.
        /// These only describe bottom zones.
        /// </summary>
        public IReadOnlyList<int> OtherBlues { get; }

        /// <summary>
        /// Optional: Integer pairs similar to <see cref="BlueValues"/> however these
        /// are used to enforce consistency across a font family when there are small differences (&lt;1px) in
        /// font alignment.
        /// </summary>
        public IReadOnlyList<int> FamilyBlues { get; }

        /// <summary>
        /// Optional: Integer pairs similar to <see cref="OtherBlues"/> however these
        /// are used to enforce consistency across a font family with small differences 
        /// in alignment similarly to <see cref="FamilyBlues"/>.
        /// </summary>
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
        public IReadOnlyList<decimal> StemSnapHorizontalWidths { get; }
        
        /// <summary>
        /// Optional: Up to 12 numbers with the most common widths for vertical stems horizontally in character space units.
        /// </summary>
        public IReadOnlyList<decimal> StemSnapVerticalWidths { get; }

        /// <summary>
        /// Optional: At small sizes at low resolutions this controls whether bold characters should appear thicker using
        /// special techniques.
        /// </summary>
        public bool ForceBold { get; }

        /// <summary>
        /// Optional: Language group 0 includes Latin, Greek and Cyrillic as well as similar alphabets.
        /// Language group 1 includes Chinese, Japanese Kanji and Korean Hangul as well as similar alphabets.
        /// If language group is 1 then <see cref="RoundStemUp"/> should also be set.
        /// Default: 0
        /// </summary>
        public int LanguageGroup { get; }

        /// <summary>
        /// Optional: Indicates the number of random bytes used for charstring encryption/decryption.
        /// Default: 4
        /// </summary>
        public int LenIv { get; }

        /// <summary>
        /// Optional: Preserved for backwards compatibility. Must be set if the <see cref="LanguageGroup"/> is 1.
        /// </summary>
        public bool? RoundStemUp { get; }

        /// <summary>
        /// Optional: The limit for changing the size of a character bounding box for
        /// <see cref="LanguageGroup"/> 1 counters during font processing.
        /// </summary>
        public decimal ExpansionFactor { get; }

        /// <summary>
        /// Required: Backwards compatibility.
        /// Default: 5839
        /// </summary>
        public int Password { get; } = 5839;

        /// <summary>
        /// Required: Backwards compatibility.
        /// Default: {16 16}
        /// </summary>
        public MinFeature MinFeature { get; } = new MinFeature(16, 16);

        /// <summary>
        /// Creates a new <see cref="Type1PrivateDictionary"/>.
        /// </summary>
        /// <param name="builder">The builder used to gather property values.</param>
        public Type1PrivateDictionary(Builder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            UniqueId = builder.UniqueId;
            BlueValues = builder.BlueValues ?? Array.Empty<int>();
            OtherBlues = builder.OtherBlues ?? Array.Empty<int>();
            FamilyBlues = builder.FamilyBlues ?? Array.Empty<int>();
            FamilyOtherBlues = builder.FamilyOtherBlues ?? Array.Empty<int>();
            BlueScale = builder.BlueScale ?? DefaultBlueScale;
            BlueFuzz = builder.BlueFuzz ?? DefaultBlueFuzz;
            BlueShift = builder.BlueShift ?? DefaultBlueShift;
            StandardHorizontalWidth = builder.StandardHorizontalWidth;
            StandardVerticalWidth = builder.StandardVerticalWidth;
            StemSnapHorizontalWidths = builder.StemSnapHorizontalWidths ?? Array.Empty<decimal>();
            StemSnapVerticalWidths = builder.StemSnapVerticalWidths ?? Array.Empty<decimal>();
            ForceBold = builder.ForceBold ?? false;
            LanguageGroup = builder.LanguageGroup ?? DefaultLanguageGroup;
            RoundStemUp = builder.RoundStemUp;
            LenIv = builder.LenIv;
            ExpansionFactor = builder.ExpansionFactor ?? DefaultExpansionFactor;
        }

        /// <summary>
        /// A mutable builder which can set any property of the private dictionary and performs no validation.
        /// </summary>
        public class Builder
        {
            /// <summary>
            /// Temporary storage for the Rd procedure tokens.
            /// </summary>
            public object Rd { get; set; }

            /// <summary>
            /// Temporary storage for the No Access Put procedure tokens.
            /// </summary>
            public object NoAccessPut { get; set; }

            /// <summary>
            /// Temporary storage for the No Access Def procedure tokens.
            /// </summary>
            public object NoAccessDef { get; set; }

            /// <summary>
            /// Temporary storage for the decrypted but raw bytes of the subroutines in this private dictionary.
            /// </summary>
            public IReadOnlyList<Type1CharstringDecryptedBytes> Subroutines { get; set; }

            /// <summary>
            /// Temporary storage for the tokens of the other subroutine procedures.
            /// </summary>
            public object[] OtherSubroutines { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.UniqueId"/>.
            /// </summary>
            public int? UniqueId { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.BlueValues"/>.
            /// </summary>
            public IReadOnlyList<int> BlueValues { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.OtherBlues"/>.
            /// </summary>
            public IReadOnlyList<int> OtherBlues { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.FamilyBlues"/>.
            /// </summary>
            public IReadOnlyList<int> FamilyBlues { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.FamilyOtherBlues"/>.
            /// </summary>
            public IReadOnlyList<int> FamilyOtherBlues { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.BlueScale"/>.
            /// </summary>
            public decimal? BlueScale { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.BlueShift"/>.
            /// </summary>
            public int? BlueShift { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.BlueFuzz"/>.
            /// </summary>
            public int? BlueFuzz { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.StandardVerticalWidth"/>.
            /// </summary>
            public decimal? StandardHorizontalWidth { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.StandardVerticalWidth"/>.
            /// </summary>
            public decimal? StandardVerticalWidth { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.StemSnapHorizontalWidths"/>.
            /// </summary>
            public IReadOnlyList<decimal> StemSnapHorizontalWidths { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.StemSnapVerticalWidths"/>.
            /// </summary>
            public IReadOnlyList<decimal> StemSnapVerticalWidths { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.ForceBold"/>.
            /// </summary>
            public bool? ForceBold { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.LanguageGroup"/>.
            /// </summary>
            public int? LanguageGroup { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.Password"/>.
            /// </summary>
            public int? Password { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.LenIv"/>.
            /// </summary>
            public int LenIv { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.MinFeature"/>.
            /// </summary>
            public MinFeature MinFeature { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.RoundStemUp"/>.
            /// </summary>
            public bool? RoundStemUp { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.ExpansionFactor"/>.
            /// </summary>
            public decimal? ExpansionFactor { get; set; }

            /// <summary>
            /// Generate a <see cref="Type1PrivateDictionary"/> from the values in this builder.
            /// </summary>
            /// <returns>The generated <see cref="Type1PrivateDictionary"/>.</returns>
            public Type1PrivateDictionary Build()
            {
                return new Type1PrivateDictionary(this);
            }
        }
    }
}
