namespace UglyToad.PdfPig.Util
{
    using System.Collections.Generic;
    using Filters;
    using Logging;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using Graphics.Colors.Icc;
    
    /// <summary>
    /// Resolves an output intent, and its embedded ICC profile, from a <c>/OutputIntents</c> array (see 14.11.5, "Output intents").
    /// <para>
    /// A PDF/X file characterises its device colour (DeviceCMYK / DeviceRGB / DeviceGray) through the output
    /// intent's <c>/DestOutputProfile</c>.
    /// </para>
    /// </summary>
    internal static class OutputIntentParser
    {
        // TODO - Can we use IEnumerable<> instead of IReadOnlyList<> to make the call lazy?

        /// <summary>
        /// Parse every entry of the <c>/OutputIntents</c> array of the given dictionary, in the order the
        /// array wrote them. This works for both the document catalog and a page object (PDF 2.0, Table 31),
        /// which may each carry <c>/OutputIntents</c>.
        /// </summary>
        public static IReadOnlyList<OutputIntent> CreateAll(DictionaryToken dictionary, IPdfTokenScanner scanner,
            ILookupFilterProvider filterProvider, IIccProfileService? iccProfileService,
            IccProfileCache iccProfileCache, ILog? log = null)
        {
            if (!dictionary.TryGet(NameToken.OutputIntents, scanner, out ArrayToken? outputIntents))
            {
                return [];
            }

            var results = new List<OutputIntent>(outputIntents.Data.Count);

            foreach (var entry in outputIntents.Data)
            {
                if (!DirectObjectFinder.TryGet(entry, scanner, out DictionaryToken? intentDictionary))
                {
                    continue;
                }

                results.Add(Create(intentDictionary, scanner, filterProvider, iccProfileService,
                    iccProfileCache, log));
            }

            return results;
        }

        /// <summary>
        /// Parse one output intent dictionary. Absent entries stay <see langword="null"/> rather than
        /// becoming empty strings: <c>/S</c> and <c>/OutputConditionIdentifier</c> are required, so their
        /// absence is itself worth reporting.
        /// </summary>
        private static OutputIntent Create(DictionaryToken intentDictionary, IPdfTokenScanner scanner,
            ILookupFilterProvider filterProvider, IIccProfileService? iccProfileService,
            IccProfileCache iccProfileCache, ILog? log)
        {
            string? name = null;
            if (intentDictionary.TryGet(NameToken.S, scanner, out NameToken? nameToken))
            {
                name = nameToken.Data;
            }

            string? outputCondition = null;
            if (intentDictionary.TryGet(NameToken.OutputCondition, scanner, out StringToken? outputConditionToken))
            {
                outputCondition = outputConditionToken?.Data;
            }

            string? outputConditionIdentifier = null;
            if (intentDictionary.TryGet(NameToken.OutputConditionIdentifier, scanner, out StringToken? outputConditionIdentifierToken))
            {
                outputConditionIdentifier = outputConditionIdentifierToken.Data;
            }

            string? registryName = null;
            if (intentDictionary.TryGet(NameToken.RegistryName, scanner, out StringToken? registryNameToken))
            {
                registryName = registryNameToken.Data;
            }

            string? info = null;
            if (intentDictionary.TryGet(NameToken.Info, scanner, out StringToken? infoToken))
            {
                info = infoToken?.Data;
            }

            IccProfileReference? destOutputProfileRef = null;
            if (intentDictionary.TryGet(NameToken.DestOutputProfileRef, scanner, out DictionaryToken? refDictionary))
            {
                destOutputProfileRef = ParseProfileReference(refDictionary, scanner);
            }

            intentDictionary.TryGet(NameToken.MixingHints, scanner, out DictionaryToken? mixingHints);
            intentDictionary.TryGet(NameToken.SpectralData, scanner, out DictionaryToken? spectralData);

            IIccProfile? profile = TryParseDestOutputProfile(intentDictionary, scanner, filterProvider,
                iccProfileService, iccProfileCache, log);

            return new OutputIntent(name, outputCondition, outputConditionIdentifier, registryName, info,
                profile, destOutputProfileRef, mixingHints, spectralData);
        }

        private static IIccProfile? TryParseDestOutputProfile(DictionaryToken intentDictionary,
            IPdfTokenScanner scanner, ILookupFilterProvider filterProvider, IIccProfileService? iccProfileService,
            IccProfileCache iccProfileCache, ILog? log)
        {
            if (iccProfileService is null)
            {
                return null;
            }

            // The unresolved token is kept because it is the cache key: when it is an indirect reference the
            // cache can recognise the profile without touching the stream at all.
            if (!intentDictionary.TryGet(NameToken.DestOutputProfile, out var profileToken)
                || !DirectObjectFinder.TryGet(profileToken, scanner, out StreamToken? profileStream))
            {
                return null;
            }

            // Shared with the /ICCBased colour space path: a PDF/X file routinely points its /DestOutputProfile
            // and an /ICCBased colour space at the same stream object, and a page-level output intent is
            // resolved once per page and again on every re-render of that page.
            return iccProfileCache.GetOrParse(profileToken, profileStream, filterProvider, scanner,
                iccProfileService, log);
        }

        private static IccProfileReference ParseProfileReference(DictionaryToken refDictionary, IPdfTokenScanner scanner)
        {
            string? profileCS = null;
            if (refDictionary.TryGet(NameToken.ProfileCS, scanner, out StringToken? profileCsString))
            {
                profileCS = profileCsString.Data;
            }
            else if (refDictionary.TryGet(NameToken.ProfileCS, scanner, out NameToken? profileCsName))
            {
                profileCS = profileCsName.Data;
            }

            string? profileName = null;
            if (refDictionary.TryGet(NameToken.ProfileName, scanner, out StringToken? profileNameString))
            {
                profileName = profileNameString.Data;
            }

            byte[]? iccVersion = null;
            if (refDictionary.TryGet(NameToken.IccVersion, scanner, out StringToken? iccVersionString))
            {
                iccVersion = iccVersionString.GetBytes();
            }

            byte[]? checkSum = null;
            if (refDictionary.TryGet(NameToken.CheckSum, scanner, out StringToken? checkSumString))
            {
                checkSum = checkSumString.GetBytes();
            }

            refDictionary.TryGet(NameToken.ColorantTable, scanner, out DictionaryToken? colorantTable);
            refDictionary.TryGet(NameToken.Urls, scanner, out ArrayToken? urls);

            return new IccProfileReference(profileCS, profileName, iccVersion, checkSum, colorantTable, urls);
        }
    }
}
