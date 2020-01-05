namespace UglyToad.PdfPig.Fonts.AdobeFontMetrics
{
    using System.Collections.Generic;
    using Core;

    /// <summary>
    /// The global metrics for a font program and the metrics of each character.
    /// </summary>
    public class AdobeFontMetrics
    {
        /// <summary>
        /// Version of the Adobe Font Metrics specification used to generate this file.
        /// </summary>
        public decimal AfmVersion { get; }

        /// <summary>
        /// Any comments in the file.
        /// </summary>
        public IReadOnlyList<string> Comments { get; }

        /// <summary>
        /// The writing directions described by these metrics.
        /// </summary>
        public AdobeFontMetricsWritingDirections MetricSets { get; }

        /// <summary>
        /// Font name.
        /// </summary>
        public string FontName { get; }

        /// <summary>
        /// Font full name.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Font family name.
        /// </summary>
        public string FamilyName { get; }

        /// <summary>
        /// Font weight.
        /// </summary>
        public string Weight { get; }

        /// <summary>
        /// Minimum bounding box for all characters in the font.
        /// </summary>
        public PdfRectangle BoundingBox { get; }

        /// <summary>
        /// Font program version identifier.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Font name trademark or copyright notice.
        /// </summary>
        public string Notice { get; }

        /// <summary>
        /// String indicating the default encoding vector for this font program.
        /// Common ones are AdobeStandardEncoding and JIS12-88-CFEncoding.
        /// Special font programs might state FontSpecific.
        /// </summary>
        public string EncodingScheme { get; }

        /// <summary>
        /// Describes the mapping scheme.
        /// </summary>
        public int MappingScheme { get; }

        /// <summary>
        /// The bytes value of the escape-character used if this font is escape-mapped.
        /// </summary>
        public int EscapeCharacter { get; }

        /// <summary>
        /// Describes the character set of this font.
        /// </summary>
        public string CharacterSet { get; }

        /// <summary>
        /// The number of characters in this font.
        /// </summary>
        public int Characters { get; }

        /// <summary>
        /// Whether this is a base font.
        /// </summary>
        public bool IsBaseFont { get; }

        /// <summary>
        /// A vector from the origin of writing direction 0 to direction 1.
        /// </summary>
        public AdobeFontMetricsVector VVector { get; }

        /// <summary>
        /// Whether <see cref="VVector"/> is the same for every character in this font.
        /// </summary>
        public bool IsFixedV { get; }

        /// <summary>
        /// Usually the y-value of the top of capital 'H'.
        /// </summary>
        public decimal CapHeight { get; }

        /// <summary>
        /// Usually the y-value of the top of lowercase 'x'.
        /// </summary>
        public decimal XHeight { get; }

        /// <summary>
        /// Usually the y-value of the top of lowercase 'd'.
        /// </summary>
        public decimal Ascender { get; }

        /// <summary>
        /// Usually the y-value of the bottom of lowercase 'p'.
        /// </summary>
        public decimal Descender { get; }

        /// <summary>
        /// Distance from the baseline for underlining.
        /// </summary>
        public decimal UnderlinePosition { get; }

        /// <summary>
        /// Width of the line for underlining.
        /// </summary>
        public decimal UnderlineThickness { get; }

        /// <summary>
        /// Angle in degrees counter-clockwise from the vertical of the vertical linea.
        /// Zero for non-italic fonts.
        /// </summary>
        public decimal ItalicAngle { get; }

        /// <summary>
        /// If present all characters have this width and height.
        /// </summary>
        public AdobeFontMetricsCharacterSize CharacterWidth { get; }

        /// <summary>
        /// Horizontal stem width.
        /// </summary>
        public decimal HorizontalStemWidth { get; }

        /// <summary>
        /// Vertical stem width.
        /// </summary>
        public decimal VerticalStemWidth { get; }

        /// <summary>
        /// Metrics for the individual characters.
        /// </summary>
        public IReadOnlyDictionary<string, AdobeFontMetricsIndividualCharacterMetric> CharacterMetrics { get; }

        /// <summary>
        /// Create a new <see cref="AdobeFontMetrics"/>.
        /// </summary>
        public AdobeFontMetrics(decimal afmVersion, IReadOnlyList<string> comments, int metricSets, string fontName,
            string fullName,
            string familyName,
            string weight,
            PdfRectangle boundingBox,
            string version,
            string notice,
            string encodingScheme,
            int mappingScheme,
            int escapeCharacter,
            string characterSet,
            int characters,
            bool isBaseFont,
            AdobeFontMetricsVector vVector,
            bool isFixedV,
            decimal capHeight,
            decimal xHeight,
            decimal ascender,
            decimal descender,
            decimal underlinePosition,
            decimal underlineThickness,
            decimal italicAngle,
            AdobeFontMetricsCharacterSize characterWidth,
            decimal horizontalStemWidth,
            decimal verticalStemWidth,
            IReadOnlyDictionary<string, AdobeFontMetricsIndividualCharacterMetric> characterMetrics)
        {
            AfmVersion = afmVersion;
            Comments = comments;
            MetricSets = (AdobeFontMetricsWritingDirections)metricSets;
            FontName = fontName;
            FullName = fullName;
            FamilyName = familyName;
            Weight = weight;
            BoundingBox = boundingBox;
            Version = version;
            Notice = notice;
            EncodingScheme = encodingScheme;
            MappingScheme = mappingScheme;
            EscapeCharacter = escapeCharacter;
            CharacterSet = characterSet;
            Characters = characters;
            IsBaseFont = isBaseFont;
            VVector = vVector;
            IsFixedV = isFixedV;
            CapHeight = capHeight;
            XHeight = xHeight;
            Ascender = ascender;
            Descender = descender;
            UnderlinePosition = underlinePosition;
            UnderlineThickness = underlineThickness;
            ItalicAngle = italicAngle;
            CharacterWidth = characterWidth;
            HorizontalStemWidth = horizontalStemWidth;
            VerticalStemWidth = verticalStemWidth;
            CharacterMetrics = characterMetrics;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"AFM Font {FontName ?? FullName} with {Characters} characters.";
        }
    }
}
