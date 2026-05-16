namespace UglyToad.PdfPig.PdfFonts
{
    using System.Diagnostics.CodeAnalysis;
    using Tokens;

    /// <summary>
    /// A Type 3 font, in which glyphs are defined as ordinary PDF content streams ("CharProcs") rather
    /// than vector outlines. Rendering a Type 3 glyph requires processing the corresponding CharProc
    /// content stream against the active graphics state, after replacing the current transformation
    /// matrix with the text rendering matrix and concatenating the font matrix (PDF 1.7 §9.6.4).
    /// </summary>
    public interface IType3Font : IFont
    {
        /// <summary>
        /// The Type 3 font's <c>/Resources</c> dictionary, used by all of its CharProc content streams.
        /// May be <c>null</c> if the font does not declare resources.
        /// </summary>
        DictionaryToken? Type3Resources { get; }

        /// <summary>
        /// Resolve a character code to its CharProc content stream via the font's encoding.
        /// </summary>
        /// <param name="characterCode">The PDF character code (not a Unicode code point).</param>
        /// <param name="charProcStream">The CharProc stream when found, otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if a CharProc was resolved for the given character code.</returns>
        bool TryGetCharProc(int characterCode, [NotNullWhen(true)] out StreamToken? charProcStream);
    }
}
