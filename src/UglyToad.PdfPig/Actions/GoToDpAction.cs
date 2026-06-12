namespace UglyToad.PdfPig.Actions
{
    using Tokens;
    
    /// <summary>
    /// Go-to-document-part action (PDF 2.0), navigating to a document part in the current document.
    /// </summary>
    public sealed class GoToDpAction : PdfAction
    {
        /// <summary>
        /// The target document part dictionary (the <c>Dp</c> entry), or <see langword="null"/> if it
        /// could not be resolved to a dictionary.
        /// </summary>
        public DictionaryToken? DocumentPart { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="documentPart">The target document part dictionary.</param>
        public GoToDpAction(DictionaryToken? documentPart) : base(ActionType.GoToDp)
        {
            DocumentPart = documentPart;
        }
    }
}
