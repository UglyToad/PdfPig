namespace UglyToad.PdfPig.PdfFonts.CidFonts
{
    using System.Collections.Generic;

    /// <summary>
    /// Specifies mapping from character identifiers to glyph indices.
    /// Can either be defined as a name in which case it must be Identity or a stream which defines the mapping.
    /// </summary>
    internal class CharacterIdentifierToGlyphIndexMap
    {
        private readonly bool isIdentity;
        private readonly int[] map;

        public CharacterIdentifierToGlyphIndexMap()
        {
            isIdentity = true;
            map = null;
        }

        public CharacterIdentifierToGlyphIndexMap(IReadOnlyList<byte> streamBytes)
        {
            var numberOfEntries = streamBytes.Count / 2;

            map = new int[numberOfEntries];
            var offset = 0;

            for (var i = 0; i < numberOfEntries; i++)
            {
                var glyphIndex = (streamBytes[offset] << 8) | streamBytes[offset + 1];
                map[i] = glyphIndex;

                offset += 2;
            }
        }

        public int? GetGlyphIndex(int characterIdentifier)
        {
            if (isIdentity)
            {
                return characterIdentifier;
            }

            if (characterIdentifier >= map.Length || characterIdentifier < 0)
            {
                return 0;
            }

            return map[characterIdentifier];
        }
    }
}
