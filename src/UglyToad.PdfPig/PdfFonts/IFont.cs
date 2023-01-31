namespace UglyToad.PdfPig.PdfFonts
{
    using Core;
    using System.Collections.Generic;
    using Tokens;

    internal interface IFont
    {
        NameToken Name { get; }

        bool IsVertical { get; }

        FontDetails Details { get; }

        int ReadCharacterCode(IInputBytes bytes, out int codeLength);

        bool TryGetUnicode(int characterCode, out string value);

        CharacterBoundingBox GetBoundingBox(int characterCode);

        TransformationMatrix GetFontMatrix();

        /// <summary>
        /// Returns the glyph path for the given character code.
        /// </summary>
        /// <param name="characterCode">Character code in a PDF. Not to be confused with unicode.</param>
        /// <param name="path">The glyph path for the given character code.</param>
        bool TryGetPath(int characterCode, out IReadOnlyList<PdfSubpath> path);

        /// <summary>
        /// Returns the normalised glyph path for the given character code in a PDF.
        /// </summary>
        /// <param name="characterCode">Character code in a PDF. Not to be confused with unicode.</param>
        /// <param name="path">The normalized glyph path for the given character code.</param>
        bool TryGetNormalisedPath(int characterCode, out IReadOnlyList<PdfSubpath> path);
    }
}
