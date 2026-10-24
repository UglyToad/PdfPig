namespace UglyToad.PdfPig.Fonts.SystemFonts;

using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AdobeFontMetrics;
using CompactFontFormat;
using Core;
using Standard14Fonts;
using TrueType;
using TrueType.Parser;
using Type1;
using Type1.Parser;

/// <summary>
/// The font programs registered with <see cref="SystemFontFinder.AddSubstituteFont(Standard14Font, Func{byte[]}[])"/>
/// to draw the glyphs of a Standard 14 font that a document uses without embedding it.
/// </summary>
/// <remarks>
/// Programs are tried in registration order: a glyph comes from the first program that has it, so later programs fill
/// gaps in earlier ones. Each program is parsed the first time a glyph is requested. A font collection (TTC/OTC)
/// contributes the faces whose PostScript name is the substituted font's (e.g. <c>Helvetica-Bold</c> from a
/// Helvetica.ttc), or all its faces in collection order if none is.
/// Only glyph outlines come from the substitute. Character codes, Unicode values, widths and bounding boxes keep coming
/// from the document and the Standard 14 Adobe Font Metrics. Safe to use from several threads.
/// </remarks>
public sealed class SubstituteFont
{
    // One entry per registered program; a font collection gives several, one per face it contributes.
    private readonly Lazy<IReadOnlyList<SubstituteProgram>>[] programs;

    /// <summary>
    /// The Standard 14 font these programs substitute.
    /// </summary>
    public Standard14Font Font { get; }

    internal SubstituteFont(Standard14Font font, IReadOnlyList<Func<byte[]>> programLoaders)
    {
        Font = font;
        programs = programLoaders
            .Select(loader => new Lazy<IReadOnlyList<SubstituteProgram>>(() => SubstituteProgram.Load(font, loader), LazyThreadSafetyMode.ExecutionAndPublication))
            .ToArray();
    }

    /// <summary>
    /// Get the outline of the glyph called <paramref name="characterName"/> in the glyph space of a font whose font
    /// matrix is <paramref name="targetFontMatrix"/>.
    /// </summary>
    /// <returns><see langword="false"/> if no program has such a glyph. Never returns the .notdef glyph.</returns>
    public bool TryGetPath(string? characterName, in TransformationMatrix targetFontMatrix, out IReadOnlyList<PdfSubpath>? path)
    {
        if (!TryGetGlyph(characterName, out var program, out path))
        {
            return false;
        }

        if (!program!.FontMatrix.Equals(targetFontMatrix))
        {
            path = program.FontMatrix.Multiply(targetFontMatrix.Inverse()).Transform(path!).ToArray();
        }

        return true;
    }

    /// <summary>
    /// Get the outline of the glyph called <paramref name="characterName"/> in text space (1 unit = 1 em).
    /// </summary>
    /// <returns><see langword="false"/> if no program has such a glyph. Never returns the .notdef glyph.</returns>
    public bool TryGetNormalisedPath(string? characterName, out IReadOnlyList<PdfSubpath>? path)
    {
        if (!TryGetGlyph(characterName, out var program, out path))
        {
            return false;
        }

        path = program!.FontMatrix.Transform(path!).ToArray();
        return true;
    }

    /// <summary>
    /// Whether any program has a glyph called <paramref name="characterName"/>.
    /// </summary>
    public bool HasGlyph(string? characterName)
    {
        return TryGetGlyph(characterName, out _, out _);
    }

    private bool TryGetGlyph(string? characterName, out SubstituteProgram? program, out IReadOnlyList<PdfSubpath>? path)
    {
        program = null;
        path = null;

        if (string.IsNullOrEmpty(characterName)
            || string.Equals(characterName, GlyphList.NotDefined, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        AlternativeNames.TryGetValue(characterName!, out var alternativeName);

        foreach (var lazy in programs)
        {
            foreach (var candidate in lazy.Value)
            {
                if (candidate.TryGetGlyphSpacePath(characterName!, out path)
                    || (alternativeName is not null && candidate.TryGetGlyphSpacePath(alternativeName, out path)))
                {
                    program = candidate;
                    return true;
                }
            }
        }

        program = null;
        return false;
    }

    /// <summary>
    /// Other names fonts commonly give a Standard 14 glyph, tried when a program has no glyph by the standard name:
    /// ligatures named after their components, and the ellipsis as misspelled in ArialMT (as PDFBox's PDType1Font).
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> AlternativeNames = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["ff"] = "f_f",
        ["ffi"] = "f_f_i",
        ["ffl"] = "f_f_l",
        ["fi"] = "f_i",
        ["fl"] = "f_l",
        ["st"] = "s_t",
        ["IJ"] = "I_J",
        ["ij"] = "i_j",
        ["ellipsis"] = "elipsis"
    };

    /// <summary>
    /// One substitute font program, whatever its format, with glyphs addressed by name.
    /// </summary>
    private abstract class SubstituteProgram
    {
        private const byte PfbSegmentMarker = 0x80;

        public TransformationMatrix FontMatrix { get; protected set; }

        /// <summary>
        /// The glyph's outline in this program's glyph space; <see langword="false"/> if it has no such glyph.
        /// </summary>
        public bool TryGetGlyphSpacePath(string characterName, out IReadOnlyList<PdfSubpath>? path)
        {
            try
            {
                return TryGetPathCore(characterName, out path);
            }
            catch
            {
                // A broken glyph description: treat the glyph as missing.
                path = null;
                return false;
            }
        }

        protected abstract bool TryGetPathCore(string characterName, out IReadOnlyList<PdfSubpath>? path);

        /// <summary>
        /// Parse a registered program: one program, several for a font collection, none if it cannot be read.
        /// </summary>
        public static IReadOnlyList<SubstituteProgram> Load(Standard14Font font, Func<byte[]> loader)
        {
            try
            {
                var bytes = loader();
                if (bytes is null || bytes.Length < 4)
                {
                    return [];
                }

                // PFB (segment marker) or PFA ("%!") is Type 1; an sfnt header is TrueType or OpenType (glyf or CFF
                // outlines); "ttcf" is a collection of those; anything else is read as a bare CFF program.
                if (bytes[0] == PfbSegmentMarker || (bytes[0] == '%' && bytes[1] == '!'))
                {
                    return [new Type1Program(Type1FontParser.Parse(new MemoryInputBytes(bytes), 0, 0, new StackDepthGuard(256)))];
                }

                if (TrueTypeFontParser.IsCollection(bytes))
                {
                    return LoadCollection(font, bytes);
                }

                if (IsSfnt(bytes))
                {
                    return [new TrueTypeProgram(TrueTypeFontParser.Parse(new TrueTypeDataBytes(bytes)), font)];
                }

                return [new CffProgram(CompactFontFormatParser.Parse(new CompactFontFormatData(bytes)).FirstFont)];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Could not load a substitute font program for {font}: {ex}");
                return [];
            }
        }

        /// <summary>
        /// The faces of a TrueType/OpenType collection whose PostScript name is <paramref name="font"/>'s, or all of
        /// them if none is. A face that cannot be read is skipped.
        /// </summary>
        private static IReadOnlyList<SubstituteProgram> LoadCollection(Standard14Font font, byte[] bytes)
        {
            var matching = new List<SubstituteProgram>();
            var others = new List<SubstituteProgram>();
            foreach (var face in TrueTypeFontParser.ParseCollection(new TrueTypeDataBytes(bytes)))
            {
                try
                {
                    var postScriptName = face.TableRegister.NameTable?.GetPostscriptName();
                    bool isMatch = postScriptName is not null
                                   && Standard14.TryGetStandard14Font(postScriptName, out var faceFont)
                                   && faceFont == font;

                    (isMatch ? matching : others).Add(new TrueTypeProgram(face, font));
                }
                catch (Exception ex)
                {
                    // A face without outlines that can be read.
                    System.Diagnostics.Debug.WriteLine($"Could not read a face of a substitute font collection for {font}: {ex}");
                }
            }

            return matching.Count > 0 ? matching : others;
        }

        private static bool IsSfnt(ReadOnlySpan<byte> bytes)
        {
            var tag = bytes.Slice(0, 4);
            return BinaryPrimitives.ReadUInt32BigEndian(tag) == 0x00010000
                   || tag.SequenceEqual("true"u8)
                   || tag.SequenceEqual("OTTO"u8);
        }
    }

    private sealed class Type1Program : SubstituteProgram
    {
        private readonly Type1Font font;

        public Type1Program(Type1Font font)
        {
            this.font = font;
            FontMatrix = font.FontMatrix;
        }

        protected override bool TryGetPathCore(string characterName, out IReadOnlyList<PdfSubpath>? path)
        {
            path = font.ContainsNamedCharacter(characterName) ? font.GetCharacterPath(characterName) : null;
            return path is not null;
        }
    }

    private sealed class CffProgram : SubstituteProgram
    {
        private readonly CompactFontFormatFont font;

        public CffProgram(CompactFontFormatFont font)
        {
            this.font = font;
            FontMatrix = font.FontMatrix;
        }

        protected override bool TryGetPathCore(string characterName, out IReadOnlyList<PdfSubpath>? path)
        {
            // Check the name first: the CFF interpreter silently draws .notdef for an unknown one.
            path = font.TryGetGlyphIndexByName(characterName, out _) ? font.GetCharacterPath(characterName) : null;
            return path is not null;
        }
    }

    private sealed class TrueTypeProgram : SubstituteProgram
    {
        private readonly TrueTypeFont font;
        private readonly bool isZapfDingbats;

        // The outlines of an OpenType font with PostScript outlines (CFF table, "OTTO"); null for glyf outlines.
        private readonly CompactFontFormatFont? cff;
        private readonly IReadOnlyDictionary<string, int> glyphIndexByName;
        private readonly ConcurrentDictionary<string, IReadOnlyList<PdfSubpath>?> paths = new(StringComparer.Ordinal);

        // The substituted font's built-in encoding, to address a (3,0) symbol cmap by character code.
        private readonly Encodings.Encoding? builtInEncoding;

        public TrueTypeProgram(TrueTypeFont font, Standard14Font substituted)
        {
            this.font = font;
            isZapfDingbats = substituted == Standard14Font.ZapfDingbats;
            if (font.WindowsSymbolCMap is not null)
            {
                builtInEncoding = new AdobeFontMetricsEncoding(Standard14.GetAdobeFontMetrics(substituted));
            }

            cff = font.CffFontCollection?.FirstFont;
            if (cff is null && font.TableRegister.GlyphTable is null)
            {
                // A CFF table the parser could not read, or no outlines at all.
                throw new InvalidFontFormatException("The font has no glyph outlines that can be read.");
            }

            FontMatrix = cff?.FontMatrix
                         ?? TransformationMatrix.FromValues(1.0 / font.GetUnitsPerEm(), 0, 0, 1.0 / font.GetUnitsPerEm(), 0, 0);

            var names = new Dictionary<string, int>(StringComparer.Ordinal);
            var glyphNames = font.TableRegister.PostScriptTable?.GlyphNames;
            if (glyphNames is not null)
            {
                for (int i = 0; i < glyphNames.Count; i++)
                {
                    if (!string.IsNullOrEmpty(glyphNames[i]) && !names.ContainsKey(glyphNames[i]))
                    {
                        names[glyphNames[i]] = i;
                    }
                }
            }

            glyphIndexByName = names;
        }

        protected override bool TryGetPathCore(string characterName, out IReadOnlyList<PdfSubpath>? path)
        {
            path = paths.GetOrAdd(characterName, GetPath);
            return path is not null;
        }

        private IReadOnlyList<PdfSubpath>? GetPath(string name)
        {
            // CFF glyphs have names of their own; check them before the post table and cmaps.
            if (cff is not null && cff.TryGetGlyphIndexByName(name, out int cffGlyphIndex) && cffGlyphIndex > 0)
            {
                return cff.GetCharacterPath(name);
            }

            int glyphIndex = GetGlyphIndex(name);
            if (glyphIndex <= 0)
            {
                return null;
            }

            if (cff is not null)
            {
                // A glyph index from the cmap addresses the CFF glyph at that position in its charset.
                var cffName = cff.GetCharacterName(glyphIndex, true);
                return !string.IsNullOrEmpty(cffName)
                       && !string.Equals(cffName, GlyphList.NotDefined, StringComparison.Ordinal)
                       && cff.TryGetPath(cffName, out var cffPath)
                    ? cffPath
                    : null;
            }

            return font.TableRegister.GlyphTable!.TryGetGlyphPath(glyphIndex, out var p) ? p : null;
        }

        private int GetGlyphIndex(string name)
        {
            // The post table names first, then the name's Unicode value through the (3,1) cmap, then its code in the
            // substituted font's built-in encoding through the (3,0) cmap.
            if (glyphIndexByName.TryGetValue(name, out int index))
            {
                return index;
            }

            var unicodeCmap = font.WindowsUnicodeCMap;
            if (unicodeCmap is not null && TryGetCodePoint(name, out int codePoint))
            {
                int glyphIndex = unicodeCmap.CharacterCodeToGlyphIndex(codePoint);
                if (glyphIndex == 0 && PrivateUseAlternates.TryGetValue(name, out int alternate))
                {
                    glyphIndex = unicodeCmap.CharacterCodeToGlyphIndex(alternate);
                }

                if (glyphIndex > 0)
                {
                    return glyphIndex;
                }
            }

            var symbolCmap = font.WindowsSymbolCMap;
            int code = builtInEncoding?.GetCode(name) ?? -1;
            if (symbolCmap is null || code < 0)
            {
                return 0;
            }

            // Symbol fonts usually map their codes into U+F000-F0FF, some map the bare code.
            int symbolGlyphIndex = symbolCmap.CharacterCodeToGlyphIndex(0xF000 + code);
            return symbolGlyphIndex > 0 ? symbolGlyphIndex : symbolCmap.CharacterCodeToGlyphIndex(code);
        }

        private bool TryGetCodePoint(string name, out int codePoint)
        {
            codePoint = 0;

            string? unicode;
            try
            {
                // ZapfDingbats glyph names (a1, a2...) are only in its own glyph list.
                unicode = isZapfDingbats ? GlyphList.ZapfDingbats.NameToUnicode(name) : null;
                unicode ??= GlyphList.AdobeGlyphList.NameToUnicode(name);
            }
            catch
            {
                return false;
            }

            if (string.IsNullOrEmpty(unicode) || (unicode!.Length > 1 && !char.IsSurrogatePair(unicode, 0)) || unicode.Length > 2)
            {
                return false;
            }

            codePoint = char.ConvertToUtf32(unicode, 0);
            return true;
        }

        /// <summary>
        /// Standard 14 glyph names that the Adobe Glyph List maps into the private use area, which Unicode fonts do not
        /// fill, with the standard code point of the same mark.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, int> PrivateUseAlternates = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["commaaccent"] = 0x0326 // COMBINING COMMA BELOW (AGL: U+F6C3)
        };
    }

    /// <summary>
    /// Parse every program now; <see langword="false"/> if any cannot be read.
    /// </summary>
    internal bool LoadAll()
    {
        return programs.All(p => p.Value.Count > 0);
    }
}
