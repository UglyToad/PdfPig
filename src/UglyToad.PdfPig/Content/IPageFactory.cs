namespace UglyToad.PdfPig.Content
{
    using Outline;
    using Tokens;

    /// <summary>
    /// Page factory interface.
    /// </summary>
    /// <typeparam name="TPage">The type of page the page factory creates.</typeparam>
    public interface IPageFactory<TPage>
    {
        /// <summary>
        /// Create the page.
        /// </summary>
        TPage Create(int number,
            DictionaryToken dictionary,
            PageTreeMembers pageTreeMembers,
            NamedDestinations annotationProvider,
            IParsingOptions parsingOptions);
    }
}