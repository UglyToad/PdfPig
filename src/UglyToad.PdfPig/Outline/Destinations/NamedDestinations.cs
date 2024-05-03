namespace UglyToad.PdfPig.Outline.Destinations
{
    using System.Diagnostics.CodeAnalysis;
    using Content;
    using Logging;
    using Tokens;

    /// <summary>
    /// Named destinations in a PDF document
    /// </summary>
    public class NamedDestinations
    {
        /// <summary>
        /// Dictionary containing explicit destinations, keyed by name
        /// </summary>
        private readonly IReadOnlyDictionary<string, ExplicitDestination> namedDestinations;

        /// <summary>
        /// Pages are required for getting explicit destinations
        /// </summary>
        private readonly Pages pages;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="namedDestinations"></param>
        /// <param name="pages"></param>
        internal NamedDestinations(IReadOnlyDictionary<string, ExplicitDestination> namedDestinations, Pages pages)
        {
            this.namedDestinations = namedDestinations;
            this.pages = pages;
        }

        internal bool TryGet(string name, [NotNullWhen(true)] out ExplicitDestination? destination)
        {
            return namedDestinations.TryGetValue(name, out destination);
        }

        internal bool TryGetExplicitDestination(ArrayToken explicitDestinationArray, ILog log, bool isRemoteDestination, [NotNullWhen(true)] out ExplicitDestination? destination)
        {
            return NamedDestinationsProvider.TryGetExplicitDestination(explicitDestinationArray, pages, log, isRemoteDestination, out destination);
        }
    }
}
