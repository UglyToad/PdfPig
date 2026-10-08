namespace UglyToad.PdfPig.Graphics
{
    using Content;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// Tracks the marked-content sequences (14.6) open while content streams are processed, and the
    /// state they carry: whether the content is hidden optional content (8.11.3) and the replacement
    /// text in effect (14.9.4).
    /// </summary>
    internal sealed class MarkedContentTracker
    {
        /// <summary>
        /// Null when hidden optional content is not skipped: content is then never hidden.
        /// </summary>
        private readonly OptionalContentState? optionalContent;
        private readonly bool useActualText;
        private readonly IPdfTokenScanner scanner;

        /// <summary>
        /// Number of open marked-content sequences (BMC/BDC).
        /// </summary>
        private int depth;

        /// <summary>
        /// Number of sequences opened by the streams enclosing the current content stream.
        /// Sequences are balanced within a content stream (14.6), so an EMC cannot end one of those.
        /// </summary>
        private int floor;

        /// <summary>
        /// Depth of the outermost open sequence that hides its content, 0 when content is visible.
        /// Nested sequences are not evaluated once hidden: a visible group inside a hidden one stays hidden.
        /// </summary>
        private int hiddenFromDepth;

        /// <summary>
        /// The replacement text the next glyph receives, null when none is in effect.
        /// It stands for the content of its whole sequence, nested sequences included,
        /// so the first glyph takes it and it becomes empty for the rest.
        /// </summary>
        private string? actualText;

        /// <summary>
        /// Depth of the sequence that brought actualText, 0 when none is in effect.
        /// </summary>
        private int actualTextDepth;

        public MarkedContentTracker(OptionalContentState? optionalContent, bool useActualText, IPdfTokenScanner scanner)
        {
            this.optionalContent = optionalContent;
            this.useActualText = useActualText;
            this.scanner = scanner;
        }

        public bool IsHidden => hiddenFromDepth > 0;

        /// <summary>
        /// Whether an EMC would end a sequence opened by the current content stream.
        /// </summary>
        public bool CanEnd => depth > floor;

        /// <summary>
        /// A marked-content sequence begins (BMC/BDC).
        /// </summary>
        /// <param name="tag">The marked-content tag.</param>
        /// <param name="properties">The property list, already resolved from the resources when given by name.</param>
        public void Begin(NameToken tag, DictionaryToken? properties)
        {
            depth++;

            if (!IsHidden && optionalContent is not null && tag.Equals(NameToken.Oc) &&
                !optionalContent.IsVisible(properties))
            {
                hiddenFromDepth = depth;
            }

            if (actualTextDepth == 0 && useActualText && properties is not null &&
                properties.TryGet(NameToken.ActualText, scanner, out IDataToken<string>? actualTextToken))
            {
                // Strip soft hyphens (U+00AD): in replacement text these are conditional hyphens marking
                // potential line-break points and are meant to be invisible when not broken. Keeping them
                // would inject invisible characters mid-word and corrupt extracted text.
                actualText = actualTextToken.Data.Replace("\u00ad", string.Empty);
                actualTextDepth = depth;
            }
        }

        /// <summary>
        /// A marked-content sequence ends (EMC).
        /// </summary>
        /// <returns><see langword="false"/> when the EMC has no matching BMC/BDC in the current content
        /// stream, in which case it is ignored.</returns>
        public bool End()
        {
            if (!CanEnd)
            {
                return false;
            }

            if (depth == hiddenFromDepth)
            {
                hiddenFromDepth = 0;
            }

            if (depth == actualTextDepth)
            {
                actualText = null;
                actualTextDepth = 0;
            }

            depth--;
            return true;
        }

        /// <summary>
        /// The text a glyph is extracted as: the replacement text in effect for the first glyph of its
        /// sequence, empty for the following ones, otherwise the glyph's own <paramref name="unicode"/>.
        /// </summary>
        public string ApplyActualText(string unicode)
        {
            if (actualText is null)
            {
                return unicode;
            }

            unicode = actualText;
            actualText = string.Empty;
            return unicode;
        }

        /// <summary>
        /// Whether content carrying the <c>/OC</c> entry of the given XObject or annotation dictionary
        /// (8.11.3.3) is shown.
        /// </summary>
        public bool IsVisible(DictionaryToken dictionary)
        {
            return optionalContent is null ||
                   !dictionary.TryGet(NameToken.Oc, scanner, out DictionaryToken? oc) ||
                   optionalContent.IsVisible(oc);
        }

        /// <summary>
        /// A content stream (the page's or a form XObject's) starts. Returns the enclosing floor, to pass to
        /// <see cref="ExitStream"/> once every sequence the stream left open has been ended.
        /// </summary>
        public int EnterStream()
        {
            int enclosingFloor = floor;
            floor = depth;
            return enclosingFloor;
        }

        /// <summary>
        /// A content stream ends.
        /// </summary>
        public void ExitStream(int enclosingFloor)
        {
            floor = enclosingFloor;
        }
    }
}