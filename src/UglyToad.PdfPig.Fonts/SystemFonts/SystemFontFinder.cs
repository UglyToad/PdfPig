namespace UglyToad.PdfPig.Fonts.SystemFonts;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CompactFontFormat;
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

    private static readonly ConcurrentDictionary<Standard14Font, SubstituteFont> SubstituteFonts = new();

    /// <summary>
    /// Fonts grouped by the upper-case first character of their filename (e.g. 'A' → all fonts
    /// whose file starts with 'a' or 'A'). Built lazily from <see cref="AvailableFonts"/> so the
    /// O(n) grouping is paid only once, turning the per-lookup first-letter scan into an O(1) dict
    /// lookup.
    /// </summary>
    private static readonly Lazy<IReadOnlyDictionary<char, SystemFontRecord[]>> FontsByFirstChar;

    /// <summary>
    /// Additional directories where to search fonts
    /// </summary>
    private static readonly HashSet<string> AdditionalFontSearchDirectories = new HashSet<string>();
    private static readonly object FontSearchDirectoryLock = new();

    /// <summary>
    /// The instance of <see cref="SystemFontFinder"/>.
    /// </summary>
    public static readonly ISystemFontFinder Instance = new SystemFontFinder();

    /// <summary>
    /// A finder that finds no font, so a document reads the same whatever fonts the machine has installed.
    /// </summary>
    public static ISystemFontFinder None { get; } = new NoSystemFontFinder();

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

        AvailableFonts = new Lazy<IReadOnlyList<SystemFontRecord>>(() =>
        {
            lock (FontSearchDirectoryLock)
            {
                return lister.GetAllFonts(AdditionalFontSearchDirectories).ToArray();
            }
        });

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

    // Font name to the file holding it and, in a font collection (.ttc), the index of its face; -1 for a single font.
    private readonly ConcurrentDictionary<string, (string Path, int FaceIndex)> nameToFileNameMap = new(StringComparer.OrdinalIgnoreCase);

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

        if (nameToFileNameMap.TryGetValue(name, out var location))
        {
            var found = location.FaceIndex < 0
                ? TryReadFile(location.Path, false, name, out var result)
                : TryReadCollectionFace(location.Path, location.FaceIndex, out result);

            return found ? result : null;
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
        }

        return null;
    }

    private bool TryGetTrueTypeFont(string name, SystemFontRecord record, out TrueTypeFont? font)
    {
        font = null;

        // OpenType files are read the same way: the parser handles TrueType and CFF outlines alike.
        bool isCollection;
        switch (record.Type)
        {
            case SystemFontType.TrueType:
            case SystemFontType.OpenType:
                isCollection = false;
                break;
            case SystemFontType.TrueTypeCollection:
            case SystemFontType.OpenTypeCollection:
                isCollection = true;
                break;
            default:
                return false;
        }

        if (readFiles.ContainsKey(record.Path))
        {
            return false;
        }

        return isCollection
            ? TryReadCollection(record.Path, name, out font)
            : TryReadFile(record.Path, true, name, out font);
    }

    /// <summary>
    /// Map the name of every face of the font collection <paramref name="fileName"/>, reading only their name tables,
    /// and parse the face called <paramref name="fontName"/> if there is one.
    /// </summary>
    private bool TryReadCollection(string fileName, string fontName, out TrueTypeFont? font)
    {
        font = null;

        if (!TryReadCollectionOffsets(fileName, out var data, out var offsets))
        {
            readFiles.TryAdd(fileName, 0);
            return false;
        }

        // Map every face before parsing one: the file is then marked as read, so a later search for another of its
        // faces finds it through the map.
        int match = -1;
        for (int i = 0; i < offsets.Count; i++)
        {
            try
            {
                data.Seek(offsets[i]);
                var nameTable = TrueTypeFontParser.GetNameTable(data);
                var faceName = nameTable?.GetPostscriptName() ?? nameTable?.FontName;
                if (string.IsNullOrEmpty(faceName))
                {
                    continue;
                }

                nameToFileNameMap.TryAdd(faceName!, (fileName, i));
                if (match < 0 && string.Equals(faceName, fontName, StringComparison.OrdinalIgnoreCase))
                {
                    match = i;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }
        }

        readFiles.TryAdd(fileName, 0);

        return match >= 0 && TryParseCollectionFace(data, offsets[match], out font);
    }

    /// <summary>
    /// Parse face <paramref name="faceIndex"/> of the font collection <paramref name="fileName"/>.
    /// </summary>
    private static bool TryReadCollectionFace(string fileName, int faceIndex, out TrueTypeFont? font)
    {
        font = null;

        return TryReadCollectionOffsets(fileName, out var data, out var offsets)
               && faceIndex < offsets.Count
               && TryParseCollectionFace(data, offsets[faceIndex], out font);
    }

    private static bool TryReadCollectionOffsets(string fileName, out TrueTypeDataBytes data, out IReadOnlyList<long> offsets)
    {
        try
        {
            data = new TrueTypeDataBytes(File.ReadAllBytes(fileName));
            offsets = TrueTypeFontParser.ReadCollectionOffsets(data);
            return true;
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine(e);
            data = null!;
            offsets = [];
            return false;
        }
    }

    private static bool TryParseCollectionFace(TrueTypeDataBytes data, long offset, out TrueTypeFont? font)
    {
        // Table offsets in a collection are from the start of the file, so the face is parsed in place.
        data.Seek(offset);
        font = TrueTypeFontParser.Parse(data);
        if (IsCidKeyedCff(font))
        {
            font = null;
            return false;
        }

        var psName = font.TableRegister.NameTable?.GetPostscriptName() ?? font.Name;
        if (psName is not null)
        {
            Cache.TryAdd(psName, font);
        }

        return true;
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

            nameToFileNameMap.TryAdd(fontNameFromFile, (fileName, -1));

            if (!string.Equals(fontNameFromFile, fontName, StringComparison.OrdinalIgnoreCase))
            {
                readFiles.TryAdd(fileName, 0);
                return false;
            }

            data.Seek(0);
        }

        font = TrueTypeFontParser.Parse(data);
        readFiles.TryAdd(fileName, 0);
        if (IsCidKeyedCff(font))
        {
            font = null;
            return false;
        }

        psName ??= font.TableRegister.NameTable?.GetPostscriptName() ?? font.Name;

        Cache.TryAdd(psName, font);

        return true;
    }

    /// <summary>
    /// Whether the font has CID-keyed CFF outlines, which glyphs cannot be read from correctly yet: such a font is
    /// not returned, as if it was not installed.
    /// </summary>
    /// <remarks>
    /// TODO (CID-keyed CFF, mostly CJK OpenType fonts such as Source Han / Noto Sans CJK .otf and .otc/.ttc):
    /// <see cref="TrueTypeFont"/> resolves a character code to a glyph index (cmap or supplied mapping), then asks the
    /// CFF for that glyph's name with <c>CompactFontFormatFont.GetCharacterName(glyphIndex, isCid: true)</c>. For a
    /// CID-keyed CFF that calls <c>Charset.GetNameByStringId(glyphIndex)</c>, i.e. it looks up the glyph whose CID
    /// equals the glyph index: right only when the charset maps glyph indexes to themselves, which installed CJK fonts
    /// generally do not, so glyphs would come out wrong. It is also a linear scan of the charset on every lookup.
    /// To fix: give the CFF font a path/bounding-box lookup by glyph index (CharStrings INDEX position, which is what
    /// the cmap gives), use it from TrueTypeFont's CFF branch, check the embedded CFF-in-OpenType CID case
    /// (https://github.com/UglyToad/PdfPig/issues/1320, P2P-33713919.pdf) still renders, then remove this check.
    /// </remarks>
    private static bool IsCidKeyedCff(TrueTypeFont font)
    {
        var cff = font.CffFontCollection?.FirstFont;
        return cff is CompactFontFormatCidFont || cff?.Charset.IsCidCharset == true;
    }

    /// <summary>
    /// Register the font programs to draw <paramref name="font"/> with whenever a document uses it without embedding
    /// it, replacing any previous registration. Without one, such glyphs have no outline and renderers fall back to an
    /// installed font, so output varies between machines.
    /// </summary>
    /// <param name="font">The Standard 14 font to substitute.</param>
    /// <param name="fontPrograms">
    /// One or more Compact Font Format (CFF), Adobe Type 1 (PFB or PFA), TrueType or OpenType (TrueType or CFF outlines)
    /// font programs, or TrueType/OpenType collections (TTC/OTC), from which the faces whose PostScript name is
    /// <paramref name="font"/>'s are used, or all faces if none is. Each glyph comes from
    /// the first program that has it, looked up by its glyph name (TrueType: post table name, else its Unicode value
    /// through the (3,1) cmap, else its code in <paramref name="font"/>'s built-in encoding through the (3,0) cmap).
    /// A program without the standard name is also searched for the usual alternative: ligatures by their components
    /// (<c>f_i</c> for <c>fi</c>, <c>f_f_l</c> for <c>ffl</c>...) and <c>elipsis</c>, ArialMT's spelling of <c>ellipsis</c>.
    /// </param>
    /// <remarks>
    /// Process-wide. Applies to fonts read after the call: documents keep the fonts they have already loaded.
    /// </remarks>
    /// <exception cref="ArgumentException">A program cannot be parsed.</exception>
    public static void AddSubstituteFont(Standard14Font font, params byte[][] fontPrograms)
    {
        if (fontPrograms is null || fontPrograms.Length == 0 || fontPrograms.Any(p => p is null))
        {
            throw new ArgumentNullException(nameof(fontPrograms));
        }

        var substitute = new SubstituteFont(font, fontPrograms.Select(p => (Func<byte[]>)(() => p)).ToArray());
        if (!substitute.LoadAll())
        {
            throw new ArgumentException($"Could not parse a substitute font program for {font}.", nameof(fontPrograms));
        }

        SubstituteFonts[font] = substitute;
    }

    /// <summary>
    /// Register the font programs to draw <paramref name="font"/> with, like
    /// <see cref="AddSubstituteFont(Standard14Font, byte[][])"/>, but read each program only the first time a document
    /// needs one of its glyphs. A program that fails to load is skipped.
    /// </summary>
    /// <param name="font">The Standard 14 font to substitute.</param>
    /// <param name="fontProgramLoaders">Functions returning the font programs, in lookup order. Called at most once each.</param>
    public static void AddSubstituteFont(Standard14Font font, params Func<byte[]>[] fontProgramLoaders)
    {
        if (fontProgramLoaders is null || fontProgramLoaders.Length == 0 || fontProgramLoaders.Any(p => p is null))
        {
            throw new ArgumentNullException(nameof(fontProgramLoaders));
        }

        SubstituteFonts[font] = new SubstituteFont(font, fontProgramLoaders);
    }

    /// <summary>
    /// Remove the substitute registered for <paramref name="font"/> with <see cref="AddSubstituteFont(Standard14Font, byte[][])"/>.
    /// </summary>
    /// <returns><see langword="true"/> if there was one.</returns>
    public static bool RemoveSubstituteFont(Standard14Font font)
    {
        return SubstituteFonts.TryRemove(font, out _);
    }

    /// <summary>
    /// Get the substitute registered for the Standard 14 font <paramref name="fontName"/> maps to, including additional
    /// names such as <c>Symbol,Bold</c> and loose matches such as <c>ABCDEF+Helvetica</c> or <c>Arial-Bold</c>
    /// (see <see cref="Standard14.TryGetStandard14Font"/>).
    /// </summary>
    public static bool TryGetSubstituteFont(string? fontName, out SubstituteFont? substitute)
    {
        substitute = null;

        return !SubstituteFonts.IsEmpty
               && fontName is not null
               && Standard14.TryGetStandard14Font(fontName, out var font)
               && SubstituteFonts.TryGetValue(font, out substitute);
    }

    /// <summary>
    /// Add a directory to the list of additional directories where to search for fonts.
    /// </summary>
    /// <param name="directory"></param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the available fonts have already been initialized and the font search directory is added too late.
    /// </exception>
    public static void AddFontSearchDirectory(string directory)
    {
        lock (FontSearchDirectoryLock)
        {
            if (AvailableFonts.IsValueCreated)
            {
                throw new InvalidOperationException("A font search directory cannot be added after the font collection has been initialized.");
            }

            if (!string.IsNullOrWhiteSpace(directory))
            {
                AdditionalFontSearchDirectories.Add(directory);
            }
        }
    }

    private sealed class NoSystemFontFinder : ISystemFontFinder
    {
        public TrueTypeFont? GetTrueTypeFont(string name) => null;
    }
}
