namespace UglyToad.PdfPig.PdfFonts.CidFonts
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Collections.Generic;
    using Core;
    using Geometry;
    using Tokens;

    /// <summary>
    /// A CID font contains glyph descriptions accessed by
    /// CID (character identifier) as character selectors.
    /// </summary>
    /// <remarks>
    /// A CID font contains information about a CIDFont program but is
    /// not itself a font. It can only be a descendant of a Type 0 font.
    /// </remarks>
    internal interface ICidFont
    {
        /// <summary>
        /// <see cref="NameToken.Font"/>
        /// </summary>
        NameToken Type { get; }

        /// <summary>
        /// Either Type0 (Adobe Type 1 font) or Type2 (TrueType font).
        /// </summary>
        NameToken SubType { get; }

        /// <summary>
        /// The PostScript name of the CIDFont.
        /// </summary>
        NameToken BaseFont { get; }

        /// <summary>
        /// The definition of the character collection for the font.
        /// </summary>
        CharacterIdentifierSystemInfo SystemInfo { get; }

        FontDetails Details { get; }

        TransformationMatrix FontMatrix { get; }

        CidFontType CidFontType { get; }

        FontDescriptor Descriptor { get; }

        double GetWidthFromDictionary(int cid);

        double GetWidthFromFont(int characterIdentifier);

        PdfRectangle GetBoundingBox(int characterIdentifier);

        PdfVector GetPositionVector(int characterIdentifier);

        PdfVector GetDisplacementVector(int characterIdentifier);

        TransformationMatrix GetFontMatrix(int characterIdentifier);

        double GetDescent();

        double GetAscent();

        /// <summary>
        /// Returns the glyph path for the given character code.
        /// </summary>
        /// <param name="characterCode">Character code in a PDF. Not to be confused with unicode.</param>
        /// <param name="path">The glyph path for the given character code.</param>
        bool TryGetPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path);

        /// <summary>
        /// Returns the glyph path for the given character code.
        /// </summary>
        /// <param name="characterCode">Character code in a PDF. Not to be confused with unicode.</param>
        /// <param name="characterCodeToGlyphId"></param>
        /// <param name="path">The glyph path for the given character code.</param>
        bool TryGetPath(int characterCode, Func<int, int?> characterCodeToGlyphId, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path);

        /// <summary>
        /// Returns the normalised glyph path for the given character code in a PDF.
        /// </summary>
        /// <param name="characterCode">Character code in a PDF. Not to be confused with unicode.</param>
        /// <param name="path">The normalized glyph path for the given character code.</param>
        bool TryGetNormalisedPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path);

        /// <summary>
        /// Returns the normalised glyph path for the given character code in a PDF.
        /// </summary>
        /// <param name="characterCode">Character code in a PDF. Not to be confused with unicode.</param>
        /// <param name="characterCodeToGlyphId"></param>
        /// <param name="path">The normalized glyph path for the given character code.</param>
        bool TryGetNormalisedPath(int characterCode, Func<int, int?> characterCodeToGlyphId, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path);
    }
}