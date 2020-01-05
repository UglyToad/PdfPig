namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;

    /// <summary>
    /// A Compact Font Format (CFF) font program as described in The Compact Font Format specification (Adobe Technical Note #5176).
    /// A CFF font may contain multiple fonts and achieves compression by sharing details between fonts in the set.
    /// </summary>
    public class CompactFontFormatFontCollection
    {
        /// <summary>
        /// The decoded header table for this font.
        /// </summary>
        public CompactFontFormatHeader Header { get; }

        /// <summary>
        /// The individual fonts contained in this font keyed by name.
        /// </summary>
        public IReadOnlyDictionary<string, CompactFontFormatFont> Fonts { get; }

        /// <summary>
        /// Create a new <see cref="CompactFontFormatFontCollection"/>.
        /// </summary>
        /// <param name="header">The header table for the font.</param>
        /// <param name="fontSet">The fonts in this font program.</param>
        public CompactFontFormatFontCollection(CompactFontFormatHeader header, IReadOnlyDictionary<string, CompactFontFormatFont> fontSet)
        {
            Header = header;
            Fonts = fontSet ?? throw new ArgumentNullException(nameof(fontSet));
        }

        /// <summary>
        /// Get the first font matrix in the font collection.
        /// </summary>
        public TransformationMatrix GetFirstTransformationMatrix()
        {
            foreach (var font in Fonts)
            {
                return font.Value.FontMatrix;
            }

            return TransformationMatrix.Identity;
        }

        /// <summary>
        /// Get the bounding box for a character if the font contains a corresponding glyph.
        /// </summary>
        public PdfRectangle? GetCharacterBoundingBox(string characterName)
        {
            return Fonts.First().Value.GetCharacterBoundingBox(characterName);
        }
    }
}
