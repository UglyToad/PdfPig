namespace UglyToad.PdfPig.Fonts.Simple
{
    using System;
    using System.Collections.Generic;
    using Cmap;
    using Composite;
    using Core;
    using Encodings;
    using Geometry;
    using IO;
    using Tokens;
    using TrueType;
    using Util.JetBrains.Annotations;

    internal class TrueTypeSimpleFont : IFont
    {
        private static readonly TransformationMatrix DefaultTransformation =
            TransformationMatrix.FromValues(1m / 1000m, 0, 0, 1m / 1000m, 0, 0);

        private readonly FontDescriptor descriptor;

        private readonly Dictionary<int, CharacterBoundingBox> boundingBoxCache
            = new Dictionary<int, CharacterBoundingBox>();

        private readonly Dictionary<int, string> unicodeValuesCache = new Dictionary<int, string>();

        [CanBeNull] private readonly Encoding encoding;

        [CanBeNull] private readonly TrueTypeFontProgram fontProgram;

        private readonly int firstCharacter;

        private readonly decimal[] widths;

        public NameToken Name { get; }

        public bool IsVertical { get; }

        [NotNull]
        public ToUnicodeCMap ToUnicode { get; set; }

        public TrueTypeSimpleFont(NameToken name,
            FontDescriptor descriptor,
            [CanBeNull] CMap toUnicodeCMap,
            [CanBeNull] Encoding encoding,
            [CanBeNull] TrueTypeFontProgram fontProgram,
            int firstCharacter,
            decimal[] widths)
        {
            this.descriptor = descriptor;
            this.encoding = encoding;
            this.fontProgram = fontProgram;
            this.firstCharacter = firstCharacter;
            this.widths = widths;

            Name = name;
            IsVertical = false;
            ToUnicode = new ToUnicodeCMap(toUnicodeCMap);
        }

        public int ReadCharacterCode(IInputBytes bytes, out int codeLength)
        {
            codeLength = 1;
            return bytes.CurrentByte;
        }

        public bool TryGetUnicode(int characterCode, out string value)
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

            if (encoding == null)
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

            decimal width;

            var index = characterCode - firstCharacter;
            if (widths != null && index >= 0 && index < widths.Length)
            {
                fromFont = false;
                width = widths[index];
            }
            else if (fontProgram != null)
            {
                if (!fontProgram.TryGetBoundingAdvancedWidth(characterCode, out width))
                {
                    width = boundingBoxPreTransform;
                }
            }
            else
            {
                throw new InvalidOperationException($"Could not retrieve width for character code: {characterCode} in font {Name}.");
            }

            if (fromFont)
            {
                width = fontMatrix.Transform(new PdfVector(width, 0)).X;
            }
            else
            {
                width = DefaultTransformation.Transform(new PdfVector(width, 0)).X;
            }

            var result = new CharacterBoundingBox(boundingBox, width);

            boundingBoxCache[characterCode] = result;

            return result;
        }

        public TransformationMatrix GetFontMatrix()
        {
            var scale = 1000m;

            if (fontProgram?.TableRegister.HeaderTable != null)
            {
                scale = fontProgram.GetFontMatrixMultiplier();
            }

            return TransformationMatrix.FromValues(1m / scale, 0, 0, 1m / scale, 0, 0);
        }

        private PdfRectangle GetBoundingBoxInGlyphSpace(int characterCode, out bool fromFont)
        {
            fromFont = true;

            if (fontProgram == null)
            {
                return descriptor.BoundingBox;
            }

            if (fontProgram.TryGetBoundingBox(characterCode, CharacterCodeToGlyphId, out var bounds))
            {
                return bounds;
            }

            if (fontProgram.TryGetBoundingAdvancedWidth(characterCode, out var width))
            {
                return new PdfRectangle(0, 0, width, 0);
            }

            fromFont = false;

            return new PdfRectangle(0, 0, GetWidth(characterCode), 0);
        }

        private int? CharacterCodeToGlyphId(int characterCode)
        {
            bool HasFlag(FontDescriptorFlags value, FontDescriptorFlags target)
            {
                return (value & target) == target;
            }

            if (descriptor == null || !unicodeValuesCache.TryGetValue(characterCode, out var unicode)
                                   || fontProgram.TableRegister.CMapTable == null
                                   || encoding == null
                                   || !encoding.CodeToNameMap.TryGetValue(characterCode, out var name)
                                   || name == null)
            {
                return null;
            }

            if (string.Equals(name, ".notdef", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            var glyphId = 0;

            if (HasFlag(descriptor.Flags, FontDescriptorFlags.Symbolic) && fontProgram.WindowsSymbolCMap != null)
            {
                const int startRangeF000 = 0xF000;
                const int startRangeF100 = 0xF100;
                const int startRangeF200 = 0xF200;

                // (3, 0) - (Windows, Symbol)
                glyphId = fontProgram.WindowsSymbolCMap.CharacterCodeToGlyphIndex(characterCode);

                if (glyphId == 0 && characterCode >= 0 && characterCode <= 0xFF)
                {
                    // CMap may use one of the following code ranges, so that we have to add the high byte to get the mapped value.

                    // F000 - F0FF
                    glyphId = fontProgram.WindowsSymbolCMap.CharacterCodeToGlyphIndex(characterCode + startRangeF000);

                    if (glyphId == 0)
                    {
                        // F100 - F1FF
                        glyphId = fontProgram.WindowsSymbolCMap.CharacterCodeToGlyphIndex(characterCode + startRangeF100);
                    }

                    if (glyphId == 0)
                    {
                        // F200 - F2FF
                        glyphId = fontProgram.WindowsSymbolCMap.CharacterCodeToGlyphIndex(characterCode + startRangeF200);
                    }
                }

                // Handle fonts incorrectly set to symbolic.
                if (glyphId == 0 && fontProgram.WindowsUnicodeCMap != null && !string.IsNullOrEmpty(unicode))
                {
                    glyphId = fontProgram.WindowsUnicodeCMap.CharacterCodeToGlyphIndex(unicode[0]);
                }
            }
            else
            {
                // (3, 1) - (Windows, Unicode)
                if (fontProgram.WindowsUnicodeCMap != null && !string.IsNullOrEmpty(unicode))
                {
                    glyphId = fontProgram.WindowsUnicodeCMap.CharacterCodeToGlyphIndex(unicode[0]);
                }

                if (glyphId == 0
                    && fontProgram.MacRomanCMap != null
                    && MacOsRomanEncoding.Instance.NameToCodeMap.TryGetValue(name, out var macCode))
                {
                    // (1, 0) - (Macintosh, Roman)

                    glyphId = fontProgram.MacRomanCMap.CharacterCodeToGlyphIndex(macCode);
                }

                if (glyphId == 0 && fontProgram.TableRegister.PostScriptTable != null)
                {
                    for (var i = 0; i < fontProgram.TableRegister.PostScriptTable.GlyphNames.Length; i++)
                    {
                        var glyphName = fontProgram.TableRegister.PostScriptTable.GlyphNames[i];

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

        private decimal GetWidth(int characterCode)
        {
            var index = characterCode - firstCharacter;

            if (index < 0 || index >= widths.Length)
            {
                return descriptor.MissingWidth;
            }

            return widths[index];
        }
    }
}

