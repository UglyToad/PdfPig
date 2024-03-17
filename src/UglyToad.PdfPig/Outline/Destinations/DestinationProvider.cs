namespace UglyToad.PdfPig.Outline.Destinations
{
    using System.Diagnostics.CodeAnalysis;
    using Logging;
    using Tokenization.Scanner;
    using Tokens;

    internal static class DestinationProvider
    {
        /// <summary>
        /// Get explicit destination or a named destination (Ref 12.3.2.3) from dictionary
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="destinationToken">Token name, can be D or Dest</param>
        /// <param name="namedDestinations"></param>
        /// <param name="pdfScanner"></param>
        /// <param name="log"></param>
        /// <param name="isRemoteDestination">in case we are looking up a destination for a GoToR (Go To Remote) action: pass in true
        /// to enforce a check for indirect page references (which is not allowed for GoToR)</param>
        /// <param name="destination"></param>
        /// <returns></returns>
        internal static bool TryGetDestination(
            DictionaryToken dictionary,
            NameToken destinationToken,
            NamedDestinations namedDestinations,
            IPdfTokenScanner pdfScanner,
            ILog log,
            bool isRemoteDestination,
            [NotNullWhen(true)] out ExplicitDestination? destination)
        {
            if (dictionary.TryGet(destinationToken, pdfScanner, out ArrayToken? destArray))
            {
                return namedDestinations.TryGetExplicitDestination(destArray, log, isRemoteDestination, out destination);
            }
            if (dictionary.TryGet(destinationToken, pdfScanner, out IDataToken<string>? destStringToken))
            {
                return namedDestinations.TryGet(destStringToken.Data, out destination);
            }
            destination = null;
            return false;
        }

    }
}
