namespace UglyToad.PdfPig.PdfFonts
{
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
    }
}
