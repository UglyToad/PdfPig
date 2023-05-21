namespace UglyToad.PdfPig.Actions
{
    using Core;
    using Logging;
    using Outline;
    using Tokenization.Scanner;
    using Tokens;
    using Outline.Destinations;
    using Util;

    internal static class ActionProvider
    {
        /// <summary>
        /// Get an action (A) from dictionary. If GoTo, GoToR or GoToE, also fetches the action destination.
        /// </summary>
        internal static bool TryGetAction(DictionaryToken dictionary,
            NamedDestinations namedDestinations,
            IPdfTokenScanner pdfScanner,
            ILog log,
            out PdfAction result)
        {
            result = null;

            if (!dictionary.TryGet(NameToken.A, pdfScanner, out DictionaryToken actionDictionary))
            {
                return false;
            }

            if (!actionDictionary.TryGet(NameToken.S, pdfScanner, out NameToken actionType))
            {
                throw new PdfDocumentFormatException($"No action type (/S) specified for action: {actionDictionary}.");
            }

            if (actionType.Equals(NameToken.GoTo))
            {
                // For GoTo, D(estination) is required
                if (DestinationProvider.TryGetDestination(actionDictionary,
                        NameToken.D,
                        namedDestinations,
                        pdfScanner,
                        log,
                        false,
                        out var destination))
                {
                    result = new GoToAction(destination);
                    return true;
                }
            }
            else if (actionType.Equals(NameToken.GoToR))
            {
                // For GoToR, F(ile) and D(estination) are required
                if (actionDictionary.TryGetOptionalStringDirect(NameToken.F, pdfScanner, out var filename)
                    && DestinationProvider.TryGetDestination(actionDictionary,
                        NameToken.D,
                        namedDestinations,
                        pdfScanner,
                        log,
                        true,
                        out var destination))
                {
                    result = new GoToRAction(destination, filename);
                    return true;
                }
            }
            else if (actionType.Equals(NameToken.GoToE))
            {
                // For GoToE, D(estination) is required
                if (DestinationProvider.TryGetDestination(actionDictionary,
                        NameToken.D,
                        namedDestinations,
                        pdfScanner,
                        log,
                        true,
                        out var destination))
                {
                    // F(ile specification) is optional
                    if (!actionDictionary.TryGetOptionalStringDirect(NameToken.F,
                            pdfScanner,
                            out var fileSpecification))
                    {
                        fileSpecification = null;
                    }

                    result = new GoToEAction(destination, fileSpecification);
                    return true;
                }
            }
            else if (actionType.Equals(NameToken.Uri))
            {
                if (!actionDictionary.TryGetOptionalStringDirect(NameToken.Uri, pdfScanner, out var uri))
                {
                    uri = null;
                }
                result = new UriAction(uri);
                return true;
            }
            return false;
        }
    }
}
