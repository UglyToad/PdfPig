#nullable disable

namespace UglyToad.PdfPig.Actions
{
    /// <summary>
    /// Action to open a URI
    /// </summary>
    public class UriAction : PdfAction
    {
        /// <summary>
        /// URI to open
        /// </summary>
        public string Uri { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="uri">URI to open</param>
        public UriAction(string uri) : base(ActionType.URI)
        {
            Uri = uri;
        }
    }
}
