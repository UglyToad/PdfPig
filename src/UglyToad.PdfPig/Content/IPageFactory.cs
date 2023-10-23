namespace UglyToad.PdfPig.Content
{
    using Outline.Destinations;
    using Tokens;

    /// <summary>
    /// Page factory interface.
    /// </summary>
    public interface IPageFactory
    {
        /// <summary>
        /// Create the page.
        /// </summary>
        Page Create(int number,
            DictionaryToken dictionary,
            PageTreeMembers pageTreeMembers,
            NamedDestinations annotationProvider);
    }
}