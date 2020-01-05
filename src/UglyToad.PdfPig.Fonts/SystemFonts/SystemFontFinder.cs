namespace UglyToad.PdfPig.Fonts.SystemFonts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Core;
    using Standard14Fonts;
    using TrueType;
    using TrueType.Parser;

    /// <inheritdoc />
    public class SystemFontFinder : ISystemFontFinder
    {
        private static readonly IReadOnlyDictionary<string, string[]> NameSubstitutes;

        private static readonly object CacheLock = new object();
        private static readonly Dictionary<string, TrueTypeFont> Cache = new Dictionary<string, TrueTypeFont>(StringComparer.OrdinalIgnoreCase);

        static SystemFontFinder()
        {
            var dict = new Dictionary<string, string[]>
            {
                {"Courier", new[] {"CourierNew", "CourierNewPSMT", "LiberationMono", "NimbusMonL-Regu"}},
                {"Courier-Bold", new[] {"CourierNewPS-BoldMT", "CourierNew-Bold", "LiberationMono-Bold", "NimbusMonL-Bold"}},
                {"Courier-Oblique", new[] {"CourierNewPS-ItalicMT", "CourierNew-Italic", "LiberationMono-Italic", "NimbusMonL-ReguObli"}},
                {"Courier-BoldOblique", new[] {"CourierNewPS-BoldItalicMT", "CourierNew-BoldItalic", "LiberationMono-BoldItalic", "NimbusMonL-BoldObli"}},
                {"Helvetica", new[] {"ArialMT", "Arial", "LiberationSans", "NimbusSanL-Regu"}},
                {"Helvetica-Bold", new[] {"Arial-BoldMT", "Arial-Bold", "LiberationSans-Bold", "NimbusSanL-Bold"}},
                {"Helvetica-BoldOblique", new[] {"Arial-BoldItalicMT", "Helvetica-BoldItalic", "LiberationSans-BoldItalic", "NimbusSanL-BoldItal"}},
                {"Helvetica-Oblique", new[] {"Arial-ItalicMT", "Arial-Italic", "Helvetica-Italic", "LiberationSans-Italic", "NimbusSanL-ReguItal"}},
                {"Times-Roman", new[] {"TimesNewRomanPSMT", "TimesNewRoman", "TimesNewRomanPS", "LiberationSerif", "NimbusRomNo9L-Regu"}},
                {"Times-Bold", new[] {"TimesNewRomanPS-BoldMT", "TimesNewRomanPS-Bold", "TimesNewRoman-Bold", "LiberationSerif-Bold", "NimbusRomNo9L-Medi"}},
                {"Times-Italic", new[] {"TimesNewRomanPS-ItalicMT", "TimesNewRomanPS-Italic", "TimesNewRoman-Italic", "LiberationSerif-Italic", "NimbusRomNo9L-ReguItal"}},
                {"TimesNewRomanPS-BoldItalicMT", new[] {"TimesNewRomanPS-BoldItalic", "TimesNewRoman-BoldItalic", "LiberationSerif-BoldItalic", "NimbusRomNo9L-MediItal"}},
                {"Symbol", new[] {"SymbolMT", "StandardSymL"}},
                {"ZapfDingbats", new[] {"ZapfDingbatsITC", "Dingbats", "MS-Gothic"}}
            };

            HashSet<string> names;
            try
            {
                names = Standard14.GetNames();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load the Standard 14 fonts from the assembly's resources.", ex);
            }

            foreach (var name in names)
            {
                if (!dict.ContainsKey(name))
                {
                    var value = Standard14.GetMappedFontName(name);

                    if (dict.TryGetValue(value, out var subs))
                    {
                        dict[name] = subs;
                    }
                    else
                    {
                        dict[name] = new[] { value };
                    }
                }
            }

            NameSubstitutes = dict;
        }

        private readonly Lazy<IReadOnlyList<SystemFontRecord>> availableFonts;

        private readonly Dictionary<string, string> nameToFileNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> readFiles = new HashSet<string>();

        /// <summary>
        /// Create a new <see cref="SystemFontFinder"/>.
        /// </summary>
        public SystemFontFinder()
        {
            ISystemFontLister lister;
#if NETSTANDARD2_0
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                lister = new WindowsSystemFontLister();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                lister = new MacSystemFontLister();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                lister = new LinuxSystemFontLister();
            }
            else
            {
                throw new NotSupportedException($"Unsupported operating system: {RuntimeInformation.OSDescription}.");
            }
#else 
            lister = new WindowsSystemFontLister();
#endif

            availableFonts = new Lazy<IReadOnlyList<SystemFontRecord>>(() => lister.GetAllFonts().ToList());
        }

        /// <inheritdoc />
        public TrueTypeFont GetTrueTypeFont(string name)
        {
            var result = GetTrueTypeFontNamed(name);

            if (result != null)
            {
                return result;
            }

            if (name.Contains("-"))
            {
                result = GetTrueTypeFontNamed(name.Replace("-", string.Empty));

                if (result != null)
                {
                    return result;
                }
            }

            if (name.Contains(","))
            {
                result = GetTrueTypeFontNamed(name.Replace(",", "-"));

                if (result != null)
                {
                    return result;
                }
            }

            foreach (var substituteName in GetSubstituteNames(name))
            {
                result = GetTrueTypeFontNamed(substituteName);

                if (result != null)
                {
                    return result;
                }
            }

            result = GetTrueTypeFontNamed(name + "-Regular");

            return result;
        }

        private IEnumerable<string> GetSubstituteNames(string name)
        {
            name = name.Replace(" ", string.Empty);
            if (NameSubstitutes.TryGetValue(name, out var values))
            {
                return values;
            }

            return EmptyArray<string>.Instance;
        }

        private TrueTypeFont GetTrueTypeFontNamed(string name)
        {
            lock (CacheLock)
            {
                if (Cache.TryGetValue(name, out var cachedResult))
                {
                    return cachedResult;
                }
            }

            if (nameToFileNameMap.TryGetValue(name, out var fileName))
            {
                if (TryReadFile(fileName, false, name, out var result))
                {
                    return result;
                }

                return null;
            }

            var nameCandidates = availableFonts.Value.Where(x => Path.GetFileName(x.Path)?.StartsWith(name[0].ToString(), StringComparison.OrdinalIgnoreCase) == true);

            foreach (var systemFontRecord in nameCandidates)
            {
                if (TryGetTrueTypeFont(name, systemFontRecord, out var font))
                {
                    return font;
                }
            }

            foreach (var record in availableFonts.Value)
            {
                if (TryGetTrueTypeFont(name, record, out var font))
                {
                    return font;
                }

                // TODO: OTF
            }

            return null;
        }

        private bool TryGetTrueTypeFont(string name, SystemFontRecord record, out TrueTypeFont font)
        {
            font = null;
            if (record.Type == SystemFontType.TrueType)
            {
                if (readFiles.Contains(record.Path))
                {
                    return false;
                }

                return TryReadFile(record.Path, true, name, out font);
            }

            return false;
        }

        private bool TryReadFile(string fileName, bool readNameFirst, string fontName, out TrueTypeFont font)
        {
            font = null;
            readFiles.Add(fileName);

            var bytes = File.ReadAllBytes(fileName);

            var data = new TrueTypeDataBytes(new ByteArrayInputBytes(bytes));

            if (readNameFirst)
            {
                var name = TrueTypeFontParser.GetNameTable(data);

                if (name == null)
                {
                    return false;
                }

                var fontNameFromFile = name.GetPostscriptName() ?? name.FontName;

                nameToFileNameMap[fontNameFromFile] = fileName;

                if (!string.Equals(fontNameFromFile, fontName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            data.Seek(0);
            font = TrueTypeFontParser.Parse(data);
            var psName = font.TableRegister.NameTable?.GetPostscriptName() ?? font.Name;

            lock (CacheLock)
            {
                if (!Cache.ContainsKey(psName))
                {
                    Cache[psName] = font;
                }
            }

            return true;
        }
    }
}