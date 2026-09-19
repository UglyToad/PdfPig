namespace UglyToad.PdfPig.PdfFonts
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Core;
    using Tokens;

    /// <summary>
    /// Font base interface.
    /// </summary>
    public interface IFont
    {
        /// <summary>
        /// The font name.
        /// </summary>
        NameToken? Name { get; }

        /// <summary>
        /// Is the font vertical.
        /// </summary>
        bool IsVertical { get; }

        /// <summary>
        /// The font details.
        /// </summary>
        FontDetails Details { get; }

        /// <summary>
        /// Read the character code.
        /// </summary>
        int ReadCharacterCode(IInputBytes bytes, out int codeLength);

        /// <summary>
        /// Try get the unicode value.
        /// </summary>
        bool TryGetUnicode(int characterCode, [NotNullWhen(true)] out string? value);

        /// <summary>
        /// Get the font bounding box.
        /// </summary>
        CharacterBoundingBox GetBoundingBox(int characterCode);

        /// <summary>
        /// Get the font transformation matrix.
        /// </summary>
        TransformationMatrix GetFontMatrix();

        /// <summary>
        /// Retrieves the descent value of the font, adjusted by the font matrix.
        /// </summary>
        /// <returns>
        /// A <see cref="double"/> representing the descent of the font, 
        /// which is the distance from the baseline to the lowest point of the font's glyphs.
        /// </returns>
        double GetDescent();

        /// <summary>
        /// Retrieves the ascent value of the font, adjusted byt the font matrix.
        /// </summary>
        /// <returns>
        /// A <see cref="double"/> representing the ascent of the font, 
        /// which is the distance from the baseline to the highest point of the font's glyphs.
        /// </returns>
        double GetAscent();

        /// <summary>
        /// Returns the glyph path for the given character code.
        /// </summary>
        /// <param name="characterCode">Character code in a PDF. Not to be confused with unicode.</param>
        /// <param name="path">The glyph path for the given character code.</param>
        bool TryGetPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path);

        /// <summary>
        /// Returns the normalised glyph path for the given character code in a PDF.
        /// </summary>
        /// <param name="characterCode">Character code in a PDF. Not to be confused with unicode.</param>
        /// <param name="path">The normalized glyph path for the given character code.</param>
        bool TryGetNormalisedPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path);

        /// <summary>
        /// Resolve a character code to the index of the glyph in this font's <b>embedded font program</b>, using
        /// the same rules <see cref="TryGetPath"/> uses to select the outline (PDF 32000-1 9.6.6.4 for simple
        /// TrueType fonts, the CIDToGIDMap for CIDFontType2, the CFF charset for CFF-based fonts).
        /// This lets a consumer that loads the embedded font program itself (a GPU glyph atlas, DirectWrite, ...)
        /// draw exactly the glyph PdfPig would, without re-deriving the encoding.
        /// </summary>
        /// <param name="characterCode">Character code in a PDF. Not to be confused with unicode.</param>
        /// <param name="glyphIndex">
        /// The glyph index in the embedded program; 0 (.notdef) is a successful resolution and returns <see langword="true"/>.
        /// </param>
        /// <returns>
        /// <see langword="false"/> when the font has no embedded program addressable by glyph index (Standard 14 without
        /// a font file, Type 3, classic Type 1 whose glyphs are addressed by name) or the code cannot be resolved.
        /// </returns>
        bool TryGetGlyphIndex(int characterCode, out int glyphIndex);
    }
}
