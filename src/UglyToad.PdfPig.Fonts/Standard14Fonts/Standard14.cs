namespace UglyToad.PdfPig.Fonts.Standard14Fonts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
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
        private static readonly Dictionary<Standard14Font, string> BuilderTypesToNames = new Dictionary<Standard14Font, string>(14);
        private static readonly Dictionary<string, AdobeFontMetrics> Standard14Cache = new Dictionary<string, AdobeFontMetrics>(34);

        /// <summary>
        /// <see cref="Standard14Mapping"/> keyed by <see cref="NormalizeName"/>, for names that are not an exact match.
        /// </summary>
        private static readonly Dictionary<string, string> NormalizedStandard14Mapping = new Dictionary<string, string>(StringComparer.Ordinal);

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

            // Additional names for Arial
            AddAdobeFontMetrics("ArialMT", "Helvetica");
            AddAdobeFontMetrics("Arial-ItalicMT", "Helvetica-Oblique");
            AddAdobeFontMetrics("Arial-BoldMT", "Helvetica-Bold");
            AddAdobeFontMetrics("Arial-BoldMT,Bold", "Helvetica-Bold");
            AddAdobeFontMetrics("Arial-BoldItalicMT", "Helvetica-BoldOblique");

            foreach (var pair in Standard14Mapping)
            {
                var key = NormalizeName(pair.Key);
                if (!NormalizedStandard14Mapping.ContainsKey(key))
                {
                    NormalizedStandard14Mapping[key] = pair.Value;
                }
            }
        }

        private static void AddAdobeFontMetrics(string fontName, Standard14Font? type = null)
        {
            AddAdobeFontMetrics(fontName, fontName, type);
        }

        private static void AddAdobeFontMetrics(string fontName, string afmName, Standard14Font? type = null)
        {
            Standard14Names.Add(fontName);
            Standard14Mapping.Add(fontName, afmName);

            if (type.HasValue)
            {
                BuilderTypesToNames[type.Value] = afmName;
            }

            if (Standard14Cache.TryGetValue(afmName, out var metrics))
            {
                Standard14Cache[fontName] = metrics;
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
                    bytes = new MemoryInputBytes(memory.ToArray());
                }

                Standard14Cache[fontName] = AdobeFontMetricsParser.Parse(bytes, true);
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
            Standard14Cache.TryGetValue(baseName, out var metrics);

            return metrics;
        }

        /// <summary>
        /// Get the Adobe Font Metrics as <see cref="AdobeFontMetrics"/> for a Standard14 font.
        /// </summary>
        public static AdobeFontMetrics GetAdobeFontMetrics(Standard14Font fontType)
        {
            return Standard14Cache[BuilderTypesToNames[fontType]];
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

        /// <summary>
        /// Get the <see cref="Standard14Font"/> which the given font name maps to, including additional names
        /// (e.g. <c>Arial,Bold</c> maps to <see cref="Standard14Font.HelveticaBold"/>).
        /// A name that is not one of those exactly is matched loosely, as PDFBox does when it looks for a font to draw a
        /// non-embedded one with: without a subset tag (<c>ABCDEF+Helvetica</c>), ignoring case, spaces, hyphens and
        /// commas (<c>Times New Roman,Bold</c> and <c>Arial-Bold</c> map to <see cref="Standard14Font.TimesBold"/> and
        /// <see cref="Standard14Font.HelveticaBold"/>).
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="GetAdobeFontMetrics(string)"/> and <see cref="IsFontInStandard14"/>, which only accept the
        /// exact names.
        /// </remarks>
        /// <returns><see langword="true"/> if the name maps to a Standard 14 font.</returns>
        public static bool TryGetStandard14Font(string baseName, out Standard14Font font)
        {
            font = default;

            if (baseName is null
                || (!Standard14Mapping.TryGetValue(baseName, out var mappedName)
                    && !NormalizedStandard14Mapping.TryGetValue(NormalizeName(baseName), out mappedName)))
            {
                return false;
            }

            foreach (var pair in BuilderTypesToNames)
            {
                if (string.Equals(pair.Value, mappedName, StringComparison.Ordinal))
                {
                    font = pair.Key;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Drop a subset tag (everything up to the first '+'), spaces, hyphens and commas, and lower-case the rest.
        /// </summary>
        private static string NormalizeName(string name)
        {
            int plus = name.IndexOf('+');
            var builder = new StringBuilder(name.Length);
            for (int i = plus + 1; i < name.Length; i++)
            {
                char c = name[i];
                if (c != ' ' && c != '-' && c != ',')
                {
                    builder.Append(char.ToLowerInvariant(c));
                }
            }

            return builder.ToString();
        }
    }
}
