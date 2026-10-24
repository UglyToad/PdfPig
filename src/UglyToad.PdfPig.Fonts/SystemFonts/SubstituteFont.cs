namespace UglyToad.PdfPig.Fonts.SystemFonts;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
/// gaps in earlier ones. Each program is parsed the first time a glyph is requested.
/// Only glyph outlines come from the substitute. Character codes, Unicode values, widths and bounding boxes keep coming
/// from the document and the Standard 14 Adobe Font Metrics. Safe to use from several threads.
/// </remarks>
public sealed class SubstituteFont
{
    private readonly Lazy<SubstituteProgram?>[] programs;

    /// <summary>
    /// The Standard 14 font these programs substitute.
    /// </summary>
    public Standard14Font Font { get; }

    internal SubstituteFont(Standard14Font font, IReadOnlyList<Func<byte[]>> programLoaders)
    {
        Font = font;
        programs = programLoaders
            .Select(loader => new Lazy<SubstituteProgram?>(() => SubstituteProgram.Load(font, loader), LazyThreadSafetyMode.ExecutionAndPublication))
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

        foreach (var lazy in programs)
        {
            program = lazy.Value;
            if (program is not null && program.TryGetGlyphSpacePath(characterName!, out path))
            {
                return true;
            }
        }

        program = null;
        return false;
    }

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

        public static SubstituteProgram? Load(Standard14Font font, Func<byte[]> loader)
        {
            try
            {
                var bytes = loader();
                if (bytes is null || bytes.Length < 4)
                {
                    return null;
                }

                // PFB (segment marker) or PFA ("%!") is Type 1; a TrueType or OpenType sfnt header is TrueType;
                // anything else is read as a bare CFF program.
                if (bytes[0] == PfbSegmentMarker || (bytes[0] == '%' && bytes[1] == '!'))
                {
                    return new Type1Program(Type1FontParser.Parse(new MemoryInputBytes(bytes), 0, 0, new StackDepthGuard(256)));
                }

                if (IsSfnt(bytes))
                {
                    return new TrueTypeProgram(TrueTypeFontParser.Parse(new TrueTypeDataBytes(bytes)));
                }

                return new CffProgram(CompactFontFormatParser.Parse(new CompactFontFormatData(bytes)).FirstFont);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Could not load a substitute font program for {font}: {ex}");
                return null;
            }
        }

        private static bool IsSfnt(byte[] bytes)
        {
            return (bytes[0] == 0 && bytes[1] == 1 && bytes[2] == 0 && bytes[3] == 0)
                   || (bytes[0] == 't' && bytes[1] == 'r' && bytes[2] == 'u' && bytes[3] == 'e');
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
        private readonly IReadOnlyDictionary<string, int> glyphIndexByName;
        private readonly ConcurrentDictionary<string, IReadOnlyList<PdfSubpath>?> paths = new(StringComparer.Ordinal);

        public TrueTypeProgram(TrueTypeFont font)
        {
            this.font = font;
            FontMatrix = TransformationMatrix.FromValues(1.0 / font.GetUnitsPerEm(), 0, 0, 1.0 / font.GetUnitsPerEm(), 0, 0);

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
            path = paths.GetOrAdd(characterName, name =>
            {
                int glyphIndex = GetGlyphIndex(name);
                return glyphIndex > 0 && font.TableRegister.GlyphTable?.TryGetGlyphPath(glyphIndex, out var p) == true ? p : null;
            });

            return path is not null;
        }

        private int GetGlyphIndex(string name)
        {
            // The post table names first; otherwise the name's Unicode value through the (3,1) cmap.
            if (glyphIndexByName.TryGetValue(name, out int index))
            {
                return index;
            }

            var cmap = font.WindowsUnicodeCMap;
            if (cmap is null)
            {
                return 0;
            }

            string? unicode;
            try
            {
                unicode = GlyphList.AdobeGlyphList.NameToUnicode(name);
            }
            catch
            {
                return 0;
            }

            if (string.IsNullOrEmpty(unicode) || (unicode!.Length > 1 && !char.IsSurrogatePair(unicode, 0)) || unicode.Length > 2)
            {
                return 0;
            }

            int glyphIndex = cmap.CharacterCodeToGlyphIndex(char.ConvertToUtf32(unicode, 0));
            if (glyphIndex == 0 && PrivateUseAlternates.TryGetValue(name, out int alternate))
            {
                glyphIndex = cmap.CharacterCodeToGlyphIndex(alternate);
            }

            return glyphIndex;
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
        return programs.All(p => p.Value is not null);
    }
}
