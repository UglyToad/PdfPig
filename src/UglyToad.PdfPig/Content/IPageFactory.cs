namespace UglyToad.PdfPig.Content
{
    using Outline.Destinations;
    using Tokens;

    /// <summary>
    /// Page factory interface.
    /// </summary>
    /// <typeparam name="TPage">The type of page the page factory creates.</typeparam>
    public interface IPageFactory<out TPage>
    {
        /// <summary>
        /// Create the page.
        /// </summary>
        TPage Create(int number,
            DictionaryToken dictionary,
            PageTreeMembers pageTreeMembers,
            NamedDestinations namedDestinations);
    }
}