﻿namespace UglyToad.PdfPig.PdfFonts.Simple
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Cmap;
    using Composite;
    using Core;
    using Fonts;
    using Fonts.Encodings;
    using Fonts.TrueType;
    using Tokens;

    internal sealed class TrueTypeSimpleFont : IFont
    {
        private static readonly TransformationMatrix DefaultTransformation =
            TransformationMatrix.FromValues(1 / 1000.0, 0, 0, 1 / 1000.0, 0, 0);

        private readonly FontDescriptor? descriptor;

        private readonly Dictionary<int, CharacterBoundingBox> boundingBoxCache = new();

        private readonly Dictionary<int, string> unicodeValuesCache = new Dictionary<int, string>();

        private readonly Encoding? encoding;

        private readonly TrueTypeFont? font;

        private readonly int firstCharacter;

        private readonly double[] widths;

#nullable disable
        public NameToken Name { get; }
#nullable enable

        public bool IsVertical { get; }

        public FontDetails Details { get; }

        public ToUnicodeCMap ToUnicode { get; set; }

        public TrueTypeSimpleFont(
            NameToken name,
            FontDescriptor? descriptor,
            CMap? toUnicodeCMap,
            Encoding? encoding,
            TrueTypeFont? font,
            int firstCharacter,
            double[] widths)
        {
            this.descriptor = descriptor;
            this.encoding = encoding;
            this.font = font;
            this.firstCharacter = firstCharacter;
            this.widths = widths;

            Name = name;
            IsVertical = false;
            ToUnicode = new ToUnicodeCMap(toUnicodeCMap);

            Details = descriptor?.ToDetails(Name?.Data)
                      ?? FontDetails.GetDefault(Name?.Data);
        }

        public int ReadCharacterCode(IInputBytes bytes, out int codeLength)
        {
            codeLength = 1;
            return bytes.CurrentByte;
        }

        public bool TryGetUnicode(int characterCode, [NotNullWhen(true)] out string? value)
        {
            value = null;

            if (unicodeValuesCache.TryGetValue(characterCode, out value))
            {
                return true;
            }

            // Behaviour specified by the Extraction of Text Content section of the specification.

            // If the font contains a ToUnicode CMap use that.
            if (ToUnicode.CanMapToUnicode && ToUnicode.TryGet(characterCode, out value))
            {
                unicodeValuesCache[characterCode] = value;

                return true;
            }

            if (encoding is null)
            {
                return false;
            }

            // If the font is a simple font that uses one of the predefined encodings MacRomanEncoding, MacExpertEncoding, or WinAnsiEncoding...

            //  Map the character code to a character name.
            var encodedCharacterName = encoding.GetName(characterCode);

            // Look up the character name in the Adobe Glyph List or additional Glyph List.
            try
            {
                value = GlyphList.AdobeGlyphList.NameToUnicode(encodedCharacterName)
                        ?? GlyphList.AdditionalGlyphList.NameToUnicode(encodedCharacterName);
            }
            catch
            {
                return false;
            }

            if (value != null)
            {
                unicodeValuesCache[characterCode] = value;
            }

            return value != null;
        }

        public CharacterBoundingBox GetBoundingBox(int characterCode)
        {
            if (boundingBoxCache.TryGetValue(characterCode, out var cached))
            {
                return cached;
            }

            var fontMatrix = GetFontMatrix();

            var boundingBox = GetBoundingBoxInGlyphSpace(characterCode, out var fromFont);

            var boundingBoxPreTransform = boundingBox.Width;

            if (fromFont)
            {
                boundingBox = fontMatrix.Transform(boundingBox);
            }
            else
            {
                boundingBox = DefaultTransformation.Transform(boundingBox);
            }

            double width;

            var index = characterCode - firstCharacter;
            if (widths != null && index >= 0 && index < widths.Length)
            {
                fromFont = false;
                width = widths[index];
            }
            else if (font != null)
            {
                if (!font.TryGetAdvanceWidth(characterCode, out width))
                {
                    width = boundingBoxPreTransform;
                }
            }
            else if (widths?.Length > 0)
            {
                width = widths[0];
            }
            else
            {
                throw new InvalidOperationException($"Could not retrieve width for character code: {characterCode} in font {Name}.");
            }

            if (fromFont)
            {
                width = fontMatrix.TransformX(width);
            }
            else
            {
                width = DefaultTransformation.TransformX(width);
            }

            var result = new CharacterBoundingBox(boundingBox, width);

            boundingBoxCache[characterCode] = result;

            return result;
        }

        public TransformationMatrix GetFontMatrix()
        {
            var scale = 1000.0;

            if (font?.TableRegister.HeaderTable != null)
            {
                scale = font.GetUnitsPerEm();
            }

            return TransformationMatrix.FromValues(1 / scale, 0, 0, 1 / scale, 0, 0);
        }

        private PdfRectangle GetBoundingBoxInGlyphSpace(int characterCode, out bool fromFont)
        {
            fromFont = true;

            if (font is null)
            {
                return descriptor!.BoundingBox;
            }

            if (font.TryGetBoundingBox(characterCode, CharacterCodeToGlyphId, out var bounds))
            {
                return bounds;
            }

            if (font.TryGetAdvanceWidth(characterCode, out var width))
            {
                return new PdfRectangle(0, 0, width, 0);
            }

            fromFont = false;

            return new PdfRectangle(0, 0, GetWidth(characterCode), 0);
        }

        private int? CharacterCodeToGlyphId(int characterCode)
        {
            static bool HasFlag(FontDescriptorFlags value, FontDescriptorFlags target)
            {
                return (value & target) == target;
            }

            if (descriptor is null || !unicodeValuesCache.TryGetValue(characterCode, out var unicode)
                                   || font!.TableRegister.CMapTable is null
                                   || encoding is null
                                   || !encoding.CodeToNameMap.TryGetValue(characterCode, out var name)
                                   || name is null)
            {
                return null;
            }

            if (string.Equals(name, GlyphList.NotDefined, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            var glyphId = 0;

            if (HasFlag(descriptor.Flags, FontDescriptorFlags.Symbolic) && font.WindowsSymbolCMap != null)
            {
                const int startRangeF000 = 0xF000;
                const int startRangeF100 = 0xF100;
                const int startRangeF200 = 0xF200;

                // (3, 0) - (Windows, Symbol)
                glyphId = font.WindowsSymbolCMap.CharacterCodeToGlyphIndex(characterCode);

                if (glyphId == 0 && characterCode >= 0 && characterCode <= 0xFF)
                {
                    // CMap may use one of the following code ranges, so that we have to add the high byte to get the mapped value.

                    // F000 - F0FF
                    glyphId = font.WindowsSymbolCMap.CharacterCodeToGlyphIndex(characterCode + startRangeF000);

                    if (glyphId == 0)
                    {
                        // F100 - F1FF
                        glyphId = font.WindowsSymbolCMap.CharacterCodeToGlyphIndex(characterCode + startRangeF100);
                    }

                    if (glyphId == 0)
                    {
                        // F200 - F2FF
                        glyphId = font.WindowsSymbolCMap.CharacterCodeToGlyphIndex(characterCode + startRangeF200);
                    }
                }

                // Handle fonts incorrectly set to symbolic.
                if (glyphId == 0 && font.WindowsUnicodeCMap != null && !string.IsNullOrEmpty(unicode))
                {
                    glyphId = font.WindowsUnicodeCMap.CharacterCodeToGlyphIndex(unicode[0]);
                }
            }
            else
            {
                // (3, 1) - (Windows, Unicode)
                if (font.WindowsUnicodeCMap != null && !string.IsNullOrEmpty(unicode))
                {
                    glyphId = font.WindowsUnicodeCMap.CharacterCodeToGlyphIndex(unicode[0]);
                }

                if (glyphId == 0
                    && font.MacRomanCMap != null
                    && MacOsRomanEncoding.Instance.NameToCodeMap.TryGetValue(name, out var macCode))
                {
                    // (1, 0) - (Macintosh, Roman)

                    glyphId = font.MacRomanCMap.CharacterCodeToGlyphIndex(macCode);
                }

                if (glyphId == 0 && font.TableRegister.PostScriptTable != null)
                {
                    for (var i = 0; i < font.TableRegister.PostScriptTable.GlyphNames.Count; i++)
                    {
                        var glyphName = font.TableRegister.PostScriptTable.GlyphNames[i];

                        if (string.Equals(glyphName, name, StringComparison.OrdinalIgnoreCase))
                        {
                            return i;
                        }
                    }
                }
            }

            if (glyphId != 0)
            {
                return glyphId;
            }

            return null;
        }

        private double GetWidth(int characterCode)
        {
            var index = characterCode - firstCharacter;

            if (index < 0 || index >= widths.Length)
            {
                return descriptor!.MissingWidth;
            }

            return widths[index];
        }

        /// <inheritdoc/>
        public bool TryGetPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path)
        {
            if (font is null)
            {
                path = null;
                return false;
            }

            return font.TryGetPath(characterCode, CharacterCodeToGlyphId, out path);
        }

        /// <inheritdoc/>
        public bool TryGetNormalisedPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path)
        {
            if (!TryGetPath(characterCode, out path))
            {
                return false;
            }

            path = GetFontMatrix().Transform(path).ToArray();
            return true;
        }
    }
}
