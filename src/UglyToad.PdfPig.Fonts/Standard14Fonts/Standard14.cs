namespace UglyToad.PdfPig.Fonts.Standard14Fonts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using AdobeFontMetrics;
    using Core;

    /// <summary>
    /// There are 14 special Type 1 fonts which must be available. The list of PostScript names for these is given in the remarks section.
    /// A font dictionary for a standard 14 font may omit the FirstChar, LastChar, Widths and FontDescriptor entries.
    /// The metrics for these fonts are provided by the Adobe Font Metrics (AFM) files.
    /// </summary>
    /// <remarks>
    /// Standard 14 fonts are deprecated as of PDF 1.5+ however support is still required for backwards compatibility.<br/>
    /// The standard 14 are:<br/>
    /// Times−Roman<br/>
    /// Helvetica<br/>
    /// Courier<br/>
    /// Symbol<br/>
    /// Times−Bold<br/>
    /// Helvetica−Bold<br/>
    /// Courier−Bold<br/>
    /// ZapfDingbats<br/>
    /// Times−Italic<br/>
    /// Helvetica−Oblique<br/>
    /// Courier−Oblique<br/>
    /// Times−BoldItalic<br/>
    /// Helvetica−BoldOblique<br/>
    /// Courier−BoldOblique
    /// </remarks>
    public static class Standard14
    {
        private static readonly HashSet<string> Standard14Names = new HashSet<string>();
        private static readonly Dictionary<string, string> Standard14Mapping = new Dictionary<string, string>(34);
        private static readonly Dictionary<string, AdobeFontMetrics> Standard14AfmMap = new Dictionary<string, AdobeFontMetrics>(34);
        private static readonly Dictionary<Standard14Font, AdobeFontMetrics> Standard14AfmTypeMap = new Dictionary<Standard14Font, AdobeFontMetrics>(14);

        static Standard14()
        {
            AddAdobeFontMetrics("Courier-Bold", Standard14Font.CourierBold);
            AddAdobeFontMetrics("Courier-BoldOblique", Standard14Font.CourierBoldOblique);
            AddAdobeFontMetrics("Courier", Standard14Font.Courier);
            AddAdobeFontMetrics("Courier-Oblique", Standard14Font.CourierOblique);
            AddAdobeFontMetrics("Helvetica", Standard14Font.Helvetica);
            AddAdobeFontMetrics("Helvetica-Bold", Standard14Font.HelveticaBold);
            AddAdobeFontMetrics("Helvetica-BoldOblique", Standard14Font.HelveticaBoldOblique);
            AddAdobeFontMetrics("Helvetica-Oblique", Standard14Font.HelveticaOblique);
            AddAdobeFontMetrics("Symbol", Standard14Font.Symbol);
            AddAdobeFontMetrics("Times-Bold", Standard14Font.TimesBold);
            AddAdobeFontMetrics("Times-BoldItalic", Standard14Font.TimesBoldItalic);
            AddAdobeFontMetrics("Times-Italic", Standard14Font.TimesItalic);
            AddAdobeFontMetrics("Times-Roman", Standard14Font.TimesRoman);
            AddAdobeFontMetrics("ZapfDingbats", Standard14Font.ZapfDingbats);

            // alternative names from Adobe Supplement to the ISO 32000
            AddAdobeFontMetrics("CourierCourierNew", "Courier");
            AddAdobeFontMetrics("CourierNew", "Courier");
            AddAdobeFontMetrics("CourierNew,Italic", "Courier-Oblique");
            AddAdobeFontMetrics("CourierNew,Bold", "Courier-Bold");
            AddAdobeFontMetrics("CourierNew,BoldItalic", "Courier-BoldOblique");
            AddAdobeFontMetrics("Arial", "Helvetica");
            AddAdobeFontMetrics("Arial,Italic", "Helvetica-Oblique");
            AddAdobeFontMetrics("Arial,Bold", "Helvetica-Bold");
            AddAdobeFontMetrics("Arial,BoldItalic", "Helvetica-BoldOblique");
            AddAdobeFontMetrics("TimesNewRoman", "Times-Roman");
            AddAdobeFontMetrics("TimesNewRoman,Italic", "Times-Italic");
            AddAdobeFontMetrics("TimesNewRoman,Bold", "Times-Bold");
            AddAdobeFontMetrics("TimesNewRoman,BoldItalic", "Times-BoldItalic");

            // Acrobat treats these fonts as "standard 14" too
            AddAdobeFontMetrics("Symbol,Italic", "Symbol");
            AddAdobeFontMetrics("Symbol,Bold", "Symbol");
            AddAdobeFontMetrics("Symbol,BoldItalic", "Symbol");
            AddAdobeFontMetrics("Times", "Times-Roman");
            AddAdobeFontMetrics("Times,Italic", "Times-Italic");
            AddAdobeFontMetrics("Times,Bold", "Times-Bold");
            AddAdobeFontMetrics("Times,BoldItalic", "Times-BoldItalic");
        }

        private static void AddAdobeFontMetrics(string fontName, Standard14Font? type = null)
        {
            AddAdobeFontMetrics(fontName, fontName, type);
        }

        private static void AddAdobeFontMetrics(string fontName, string afmName, Standard14Font? type = null)
        {
            Standard14Names.Add(fontName);
            Standard14Mapping.Add(fontName, afmName);

            if (Standard14AfmMap.TryGetValue(afmName, out var metrics))
            {
                Standard14AfmMap[fontName] = metrics;
            }

            try
            {
                var assembly = typeof(Standard14).Assembly;

                var name = $"UglyToad.PdfPig.Fonts.Resources.AdobeFontMetrics.{afmName}.afm";

                IInputBytes bytes;
                using (var memory = new MemoryStream())
                using (var resource = assembly.GetManifestResourceStream(name))
                {
                    if (resource == null)
                    {
                        throw new InvalidOperationException($"Could not find AFM resource with name: {name}.");
                    }

                    resource.CopyTo(memory);
                    bytes = new ByteArrayInputBytes(memory.ToArray());
                }

                Standard14AfmMap[fontName] = AdobeFontMetricsParser.Parse(bytes, true);
                if (type.HasValue)
                {
                    Standard14AfmTypeMap[type.Value] = Standard14AfmMap[fontName];
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not load {fontName} from the AFM files.", ex);
            }
        }

        /// <summary>
        /// Get the Adobe Font Metrics as <see cref="AdobeFontMetrics"/> for a font.
        /// If the font is not found this returns <see langword="null"/>.
        /// </summary>
        public static AdobeFontMetrics GetAdobeFontMetrics(string baseName)
        {
            Standard14AfmMap.TryGetValue(baseName, out var metrics);

            return metrics;
        }

        /// <summary>
        /// Get the Adobe Font Metrics as <see cref="AdobeFontMetrics"/> for a Standard14 font.
        /// </summary>
        public static AdobeFontMetrics GetAdobeFontMetrics(Standard14Font fontType)
        {
            return Standard14AfmTypeMap[fontType];
        }
        
        /// <summary>
        /// Determines if a font with this name is a standard 14 font.
        /// </summary>
        public static bool IsFontInStandard14(string baseName)
        {
            return Standard14Names.Contains(baseName);
        }

        /// <summary>
        /// Returns the set of Standard 14 font names, including additional names.
        /// </summary>
        public static HashSet<string> GetNames()
        {
            return new HashSet<string>(Standard14Names);
        }
        
        /// <summary>
        /// Get the official Standard 14 name of the actual font which the given font name maps to.
        /// </summary>
        public static string GetMappedFontName(string baseName)
        {
            Standard14Mapping.TryGetValue(baseName, out var mappedName);

            return mappedName;
        }
    }
}
