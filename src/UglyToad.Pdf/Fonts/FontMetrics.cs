namespace UglyToad.Pdf.Fonts
{
    using System.Collections.Generic;
    using Geometry;

    internal class FontMetrics
    {
        public decimal AfmVersion { get; }

        public IReadOnlyList<string> Comments { get; }

        public int MetricSets { get; }

        public string FontName { get; }

        public string FullName { get; }

        public string FamilyName { get; }

        public string Weight { get; }

        public PdfRectangle BoundingBox { get; }

        public string Version { get; }

        public string Notice { get; }

        public string EncodingScheme { get; }

        public int MappingScheme { get; }

        public int EscapeCharacter { get; }

        public string CharacterSet { get; }

        public int Characters { get; }

        public bool IsBaseFont { get; }

        public PdfVector VVector { get; }

        public bool IsFixedV { get; }

        public decimal CapHeight { get; }

        public decimal XHeight { get; }

        public decimal Ascender { get; }

        public decimal Descender { get; }

        public decimal UnderlinePosition { get; }

        public decimal UnderlineThickness { get; }

        public decimal ItalicAngle { get; }

        public CharacterWidth CharacterWidth { get; }

        public decimal HorizontalStemWidth { get; }

        public decimal VerticalStemWidth { get; }

        public IReadOnlyDictionary<string, IndividualCharacterMetric> CharacterMetrics { get; }

        public FontMetrics(decimal afmVersion, IReadOnlyList<string> comments, int metricSets, string fontName, string fullName, string familyName, string weight, PdfRectangle boundingBox, string version, string notice, string encodingScheme, int mappingScheme, int escapeCharacter, string characterSet, int characters, bool isBaseFont, PdfVector vVector, bool isFixedV, decimal capHeight, decimal xHeight, decimal ascender, decimal descender, decimal underlinePosition, decimal underlineThickness, decimal italicAngle, CharacterWidth characterWidth, decimal horizontalStemWidth, decimal verticalStemWidth, IReadOnlyDictionary<string, IndividualCharacterMetric> characterMetrics)
        {
            AfmVersion = afmVersion;
            Comments = comments;
            MetricSets = metricSets;
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
    }
}
