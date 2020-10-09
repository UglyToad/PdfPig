namespace UglyToad.PdfPig.PdfFonts
{
    using Core;
    using System.Collections.Generic;
    using Tokens;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Tokenization.Scanner;

    /// <summary>
    /// IFont
    /// </summary>
    public interface IFont
    {
        /// <summary>
        /// Name
        /// </summary>
        NameToken Name { get; }

        /// <summary>
        /// IsVertical
        /// </summary>
        bool IsVertical { get; }

        /// <summary>
        /// Details
        /// </summary>
        FontDetails Details { get; }

        /// <summary>
        /// TryGetDecodedFontBytes
        /// </summary>
        bool TryGetDecodedFontBytes(IPdfTokenScanner pdfTokenScanner, IFilterProvider filterProvider, out IReadOnlyList<byte> bytes);

        /// <summary>
        /// ReadCharacterCode
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="codeLength"></param>
        /// <returns></returns>
        int ReadCharacterCode(IInputBytes bytes, out int codeLength);

        /// <summary>
        /// TryGetUnicode
        /// </summary>
        /// <param name="characterCode"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool TryGetUnicode(int characterCode, out string value);

        /// <summary>
        /// GetBoundingBox
        /// </summary>
        /// <param name="characterCode"></param>
        /// <returns></returns>
        CharacterBoundingBox GetBoundingBox(int characterCode);

        /// <summary>
        /// TryGetPath
        /// </summary>
        /// <param name="characterCode"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        bool TryGetPath(int characterCode, out IReadOnlyList<PdfSubpath> path);

        /// <summary>
        /// GetFontMatrix
        /// </summary>
        /// <returns></returns>
        TransformationMatrix GetFontMatrix();
    }
}
