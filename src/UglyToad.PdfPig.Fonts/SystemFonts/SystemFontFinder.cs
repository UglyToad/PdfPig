namespace UglyToad.PdfPig.Fonts.SystemFonts;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Standard14Fonts;
using System.Runtime.InteropServices;
using TrueType;
using TrueType.Parser;

/// <inheritdoc />
public sealed class SystemFontFinder : ISystemFontFinder
{
    private static readonly IReadOnlyDictionary<string, string[]> NameSubstitutes;
    private static readonly Lazy<IReadOnlyList<SystemFontRecord>> AvailableFonts;

    private static readonly ConcurrentDictionary<string, TrueTypeFont> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Fonts grouped by the upper-case first character of their filename (e.g. 'A' → all fonts
    /// whose file starts with 'a' or 'A'). Built lazily from <see cref="AvailableFonts"/> so the
    /// O(n) grouping is paid only once, turning the per-lookup first-letter scan into an O(1) dict
    /// lookup.
    /// </summary>
    private static readonly Lazy<IReadOnlyDictionary<char, SystemFontRecord[]>> FontsByFirstChar;

    /// <summary>
    /// The instance of <see cref="SystemFontFinder"/>.
    /// </summary>
    public static readonly ISystemFontFinder Instance = new SystemFontFinder();

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
                {"Times-BoldItalic", new[] {"TimesNewRomanPS-BoldItalicMT", "TimesNewRomanPS-BoldItalic", "TimesNewRoman-BoldItalic", "LiberationSerif-BoldItalic", "NimbusRomNo9L-MediItal"}},
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

        ISystemFontLister lister;
#if NETSTANDARD2_0_OR_GREATER || NET
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
#if NET
            else if (OperatingSystem.IsAndroid())
            {
                lister = new AndroidSystemFontLister();
            }
            else if (OperatingSystem.IsBrowser())
            {
                lister = new BrowserSystemFontLister();
            }
            else if (OperatingSystem.IsMacCatalyst())
            {
                lister = new MacSystemFontLister();
            }
            else if (OperatingSystem.IsIOS())
            {
                lister = new IOSSystemFontLister();
            }
#endif
        else
        {
            throw new NotSupportedException($"Unsupported operating system: {RuntimeInformation.OSDescription}.");
        }
#elif NETFRAMEWORK
            lister = new WindowsSystemFontLister();
#else
#error Missing ISystemFontLister for target framework
#endif

        AvailableFonts = new Lazy<IReadOnlyList<SystemFontRecord>>(() => lister.GetAllFonts().ToArray());

        FontsByFirstChar = new Lazy<IReadOnlyDictionary<char, SystemFontRecord[]>>(() =>
        {
            var fonts = AvailableFonts.Value;
            var byChar = new Dictionary<char, List<SystemFontRecord>>();

            foreach (var record in fonts)
            {
                var fn = Path.GetFileName(record.Path);
                if (string.IsNullOrEmpty(fn))
                {
                    continue;
                }

                var key = char.ToUpperInvariant(fn[0]);
                if (!byChar.TryGetValue(key, out var list))
                {
                    byChar[key] = list = new List<SystemFontRecord>();
                }

                list.Add(record);
            }

            var result = new Dictionary<char, SystemFontRecord[]>(byChar.Count);
            foreach (var kvp in byChar)
            {
                result[kvp.Key] = kvp.Value.ToArray();
            }

            return result;
        });
    }

    private readonly ConcurrentDictionary<string, string> nameToFileNameMap = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    // Tracks font file paths that have already been scanned for name matching, so we never open
    // the same file twice during a search.  Value is always 0 – the dictionary is used as a
    // lock-free concurrent set.
    private readonly ConcurrentDictionary<string, byte> readFiles = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);

    /// <summary>
    /// Create a new <see cref="SystemFontFinder"/>.
    /// </summary>
    private SystemFontFinder()
    { }

    /// <inheritdoc />
    public TrueTypeFont? GetTrueTypeFont(string name)
    {
        var result = GetTrueTypeFontNamed(name);

        if (result is not null)
        {
            return result;
        }

        if (name.Contains('-'))
        {
            result = GetTrueTypeFontNamed(name.Replace("-", string.Empty));

            if (result != null)
            {
                return result;
            }
        }

        if (name.Contains(','))
        {
            result = GetTrueTypeFontNamed(name.Replace(',', '-'));

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

    private static string[] GetSubstituteNames(string name)
    {
        if (name.Contains(' '))
        {
            name = name.Replace(" ", string.Empty);
        }

        if (NameSubstitutes.TryGetValue(name, out var values))
        {
            return values;
        }

        return Array.Empty<string>();
    }

    private TrueTypeFont? GetTrueTypeFontNamed(string name)
    {
        if (Cache.TryGetValue(name, out var cachedResult))
        {
            return cachedResult;
        }

        if (nameToFileNameMap.TryGetValue(name, out var fileName))
        {
            if (TryReadFile(fileName, false, name, out var result))
            {
                return result;
            }

            return null;
        }

        // First pass: fonts whose filename starts with the same letter as the requested name –
        // the most likely match.  FontsByFirstChar is built once, making this O(1) vs the
        // previous O(n) LINQ scan that also allocated a string for name[0].ToString().
        char firstChar = char.ToUpperInvariant(name[0]);

        if (FontsByFirstChar.Value.TryGetValue(firstChar, out var candidates))
        {
            foreach (var record in candidates)
            {
                if (TryGetTrueTypeFont(name, record, out var font))
                {
                    return font;
                }
            }
        }

        // Second pass: all remaining fonts (those whose filename does NOT start with the same
        // letter).  We skip first-char matches to avoid re-processing what was already tried
        // above – the original code iterated all fonts a second time without this guard.
        // GetFirstFileNameChar avoids allocating a substring on every iteration.
        foreach (var record in AvailableFonts.Value)
        {
#if NET
            char localFirstChar = Path.GetFileName(record.Path.AsSpan())[0];
#else
            char localFirstChar = Path.GetFileName(record.Path)[0];
#endif

            if (char.ToUpperInvariant(localFirstChar) == firstChar)
            {
                continue; // Already tried in first pass
            }

            if (TryGetTrueTypeFont(name, record, out var font))
            {
                return font;
            }

            // TODO: OTF
        }

        return null;
    }

    private bool TryGetTrueTypeFont(string name, SystemFontRecord record, out TrueTypeFont? font)
    {
        font = null;

        if (record.Type == SystemFontType.TrueType)
        {
            if (readFiles.ContainsKey(record.Path))
            {
                return false;
            }

            return TryReadFile(record.Path, true, name, out font);
        }

        return false;
    }

    private bool TryReadFile(string fileName, bool readNameFirst, string fontName, out TrueTypeFont? font)
    {
        font = null;

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(fileName);
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine(e);
            return false;
        }

        var data = new TrueTypeDataBytes(bytes);

        string? psName = null;

        if (readNameFirst)
        {
            var nameTable = TrueTypeFontParser.GetNameTable(data);

            if (nameTable is null)
            {
                readFiles.TryAdd(fileName, 0);
                return false;
            }

            psName = nameTable.GetPostscriptName();
            string? fontNameFromFile = psName ?? nameTable.FontName;

            nameToFileNameMap.TryAdd(fontNameFromFile, fileName);

            if (!string.Equals(fontNameFromFile, fontName, StringComparison.OrdinalIgnoreCase))
            {
                readFiles.TryAdd(fileName, 0);
                return false;
            }

            data.Seek(0);
        }

        font = TrueTypeFontParser.Parse(data);
        psName ??= font.TableRegister.NameTable?.GetPostscriptName() ?? font.Name;

        Cache.TryAdd(psName, font);
        readFiles.TryAdd(fileName, 0);

        return true;
    }
}
