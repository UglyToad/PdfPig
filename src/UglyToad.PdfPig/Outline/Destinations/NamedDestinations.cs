namespace UglyToad.PdfPig.Outline.Destinations
{
    using System.Collections.Generic;
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

        private readonly IReadOnlyDictionary<NameToken, ExplicitDestination>? nameObjects;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="namedDestinations"></param>
        /// <param name="pages"></param>
        internal NamedDestinations(IReadOnlyDictionary<string, ExplicitDestination> namedDestinations, Pages pages,
            IReadOnlyDictionary<NameToken, ExplicitDestination>? nameObjects = null)
        {
            this.namedDestinations = namedDestinations;
            this.pages = pages;
            this.nameObjects = nameObjects;
        }

        internal bool TryGet(string name, [NotNullWhen(true)] out ExplicitDestination? destination)
        {
            if (nameObjects != null)
            {
                return nameObjects.TryGetValue(NameToken.Create(name), out destination);
            }
            return namedDestinations.TryGetValue(name, out destination);
        }

        internal bool TryGet(NameToken name, [NotNullWhen(true)] out ExplicitDestination? destination)
        {
            // /Dests dictionary keys are name identities. /Names tree keys are text strings.
            return nameObjects != null
                ? nameObjects.TryGetValue(name, out destination)
                : namedDestinations.TryGetValue(name.Data, out destination);
        }

        internal bool TryGetExplicitDestination(ArrayToken explicitDestinationArray, ILog log, bool isRemoteDestination, [NotNullWhen(true)] out ExplicitDestination? destination)
        {
            return NamedDestinationsProvider.TryGetExplicitDestination(explicitDestinationArray, pages, log, isRemoteDestination, out destination);
        }
    }
}
