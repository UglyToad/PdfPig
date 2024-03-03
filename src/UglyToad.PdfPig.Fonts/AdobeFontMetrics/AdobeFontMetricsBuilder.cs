namespace UglyToad.PdfPig.Fonts.AdobeFontMetrics
{
    using Core;
    using System.Collections.Generic;
    using System.Linq;

    internal class AdobeFontMetricsBuilder
    {
        public double AfmVersion { get; }

        public List<string> Comments { get; }

        public List<AdobeFontMetricsIndividualCharacterMetric> CharacterMetrics { get; } 
            = new List<AdobeFontMetricsIndividualCharacterMetric>();

        /// <summary>
        /// Name of the font as seen by PostScript.
        /// </summary>
        public string FontName { get; set; }

        /// <summary>
        /// The full text name of the font.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// The name of the typeface family for the font.
        /// </summary>
        public string FamilyName { get; set; }

        /// <summary>
        /// The weight of the font.
        /// </summary>
        public string Weight { get; set; }

        /// <summary>
        /// Angle in degrees counter-clockwise from vertical of vertical strokes of the font.
        /// </summary>
        public double ItalicAngle { get; set; }

        /// <summary>
        /// Whether the font is monospaced or not.
        /// </summary>
        public bool IsFixedPitch { get; set; }

        /// <summary>
        /// The dimensions of the font bounding box.
        /// </summary>
        public PdfRectangle PdfBoundingBox { get; private set; }

        /// <summary>
        /// Distance from the baseline for underlining.
        /// </summary>
        public double UnderlinePosition { get; set; }

        /// <summary>
        /// The stroke width for underlining.
        /// </summary>
        public double UnderlineThickness { get; set; }

        /// <summary>
        /// Version identifier for the font program.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Font name trademark or copyright notice.
        /// </summary>
        public string Notice { get; set; }

        public string EncodingScheme { get; set; }

        /// <summary>
        /// Code describing mapping scheme for a non base font.
        /// </summary>
        public int MappingScheme { get; set; }

        /// <summary>
        /// The character set of this font.
        /// </summary>
        public string CharacterSet { get; set; }

        public bool IsBaseFont { get; set; } = true;

        /// <summary>
        /// The y-value of the top of a capital H.
        /// </summary>
        public double CapHeight { get; set; }

        /// <summary>
        /// The y-value of the top of lowercase x.
        /// </summary>
        public double XHeight { get; set; }

        /// <summary>
        /// Generally the y-value of the top of lowercase d.
        /// </summary>
        public double Ascender { get; set; }

        /// <summary>
        /// The y-value of the bottom of lowercase p.
        /// </summary>
        public double Descender { get; set; }

        /// <summary>
        /// Width of horizontal stems.
        /// </summary>
        public double StdHw { get; set; }

        /// <summary>
        /// Width of vertical stems.
        /// </summary>
        public double StdVw { get; set; }

        public int EscapeCharacter { get; set; }

        public AdobeFontMetricsCharacterSize CharacterWidth { get; private set; }

        public int Characters { get; set; }

        public AdobeFontMetricsVector VVector { get; private set; }

        public bool IsFixedV { get; set; }

        public AdobeFontMetricsBuilder(double afmVersion)
        {
            AfmVersion = afmVersion;
            Comments = new List<string>();
        }

        public void SetBoundingBox(double x1, double y1, double x2, double y2)
        {
            PdfBoundingBox = new PdfRectangle(x1, y1, x2, y2);
        }

        public void SetCharacterWidth(double x, double y)
        {
            CharacterWidth = new AdobeFontMetricsCharacterSize(x, y);
        }

        public void SetVVector(double x, double y)
        {
            VVector = new AdobeFontMetricsVector(x, y);
        }

        public AdobeFontMetrics Build()
        {
            var dictionary = CharacterMetrics.ToDictionary(x => x.Name);

            return new AdobeFontMetrics(AfmVersion, Comments, 0, FontName, FullName,
                FamilyName, Weight, PdfBoundingBox, Version, Notice, EncodingScheme,
                MappingScheme, EscapeCharacter, CharacterSet, Characters, IsBaseFont, VVector, 
                IsFixedV, CapHeight, XHeight, Ascender, Descender, UnderlinePosition, UnderlineThickness,
                ItalicAngle, CharacterWidth, StdHw, StdVw, dictionary);
        }
    }
}