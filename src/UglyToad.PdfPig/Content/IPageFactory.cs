namespace UglyToad.PdfPig.Content
{
    using Outline.Destinations;
    using System.Threading;
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
        /// <param name="number">The page number, starts at 1.</param>
        /// <param name="dictionary">The page dictionary.</param>
        /// <param name="pageTreeMembers">The values the page inherits from its ancestors in the page tree.</param>
        /// <param name="namedDestinations">The document's named destinations.</param>
        /// <param name="token">The cancellation token. Implementations should throw
        /// <see cref="System.OperationCanceledException"/> once it is cancelled, and must leave the
        /// shared resource store as they found it when they do.</param>
        TPage Create(int number,
            DictionaryToken dictionary,
            PageTreeMembers pageTreeMembers,
            NamedDestinations namedDestinations,
            CancellationToken token);
    }
}