namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Collections.Generic;
    using Filters;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using Graphics.Colors.Icc;
    
    /// <summary>
    /// Resolves an output intent, and its embedded ICC profile, from a <c>/OutputIntents</c> array
    /// (see 14.11.5, "Output intents").
    /// <para>
    /// A PDF/X file characterises its device colour (DeviceCMYK / DeviceRGB / DeviceGray) through the output
    /// intent's <c>/DestOutputProfile</c>. PdfPig parses and exposes that profile but does not currently
    /// convert device colours through it - device colour spaces behave identically whether or not an output
    /// intent is present - so what this produces is descriptive: the output condition a document was prepared
    /// for, and a profile a caller may drive itself.
    /// </para>
    /// </summary>
    internal static class OutputIntentParser
    {
        /// <summary>
        /// Try to resolve and parse the most usable output intent from the
        /// <c>/OutputIntents</c> array of the given dictionary. This works for both the document
        /// catalog and a page object (PDF 2.0, Table 31), which may each carry <c>/OutputIntents</c>.
        /// <para>
        /// Where several entries are present, one embedding a usable <c>/DestOutputProfile</c> always wins;
        /// among equally usable entries the <c>/S</c> subtype decides, preferring <c>GTS_PDFX</c>, then
        /// <c>GTS_PDFA1</c>, then array order.
        /// </para>
        /// </summary>
        public static OutputIntent? Create(DictionaryToken dictionary, IPdfTokenScanner scanner,
            ILookupFilterProvider filterProvider, IIccProfileService? iccProfileService,
            IccProfileByteCache iccProfileCache)
        {
            if (iccProfileService is null)
            {
                return null;
            }

            if (!dictionary.TryGet(NameToken.OutputIntents, scanner, out ArrayToken? outputIntents))
            {
                return null;
            }
            
            var ranked = new List<(int Rank, int Index, DictionaryToken Dictionary)>(outputIntents.Data.Count);

            for (int i = 0; i < outputIntents.Data.Count; i++)
            {
                if (!DirectObjectFinder.TryGet(outputIntents.Data[i], scanner, out DictionaryToken? entryDictionary))
                {
                    continue;
                }

                ranked.Add((GetSubtypeRank(entryDictionary, scanner), i, entryDictionary));
            }

            ranked.Sort(static (a, b) => a.Rank != b.Rank ? a.Rank.CompareTo(b.Rank) : a.Index.CompareTo(b.Index));
            
            OutputIntent? fallback = null;

            foreach (var (_, _, intentDictionary) in ranked)
            {
                string name = "";
                if (intentDictionary.TryGet(NameToken.S, scanner, out NameToken? nameToken))
                {
                    name = nameToken.Data;
                }

                string? outputCondition = null;
                if (intentDictionary.TryGet(NameToken.OutputCondition, scanner, out StringToken? outputConditionToken))
                {
                    outputCondition = outputConditionToken?.Data;
                }

                string outputConditionIdentifier = "";
                if (intentDictionary.TryGet(NameToken.OutputConditionIdentifier, scanner, out StringToken? outputConditionIdentifierToken))
                {
                    outputConditionIdentifier = outputConditionIdentifierToken.Data;
                }

                string registryName = "";
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
                    iccProfileService, iccProfileCache);

                var outputIntent = new OutputIntent(name, outputCondition, outputConditionIdentifier, registryName, info,
                    profile, destOutputProfileRef, mixingHints, spectralData);

                if (profile is not null)
                {
                    return outputIntent;
                }

                fallback ??= outputIntent;
            }

            return fallback;
        }

        private static int GetSubtypeRank(DictionaryToken intentDictionary, IPdfTokenScanner scanner)
        {
            if (!intentDictionary.TryGet(NameToken.S, scanner, out NameToken? subtype))
            {
                return 2; // Other
            }

            return subtype.Data switch // Lower is better
            {
                "GTS_PDFX" => 0,
                "GTS_PDFA1" => 1,
                _ => 2 // Other
            };
        }

        private static IIccProfile? TryParseDestOutputProfile(DictionaryToken intentDictionary,
            IPdfTokenScanner scanner, ILookupFilterProvider filterProvider, IIccProfileService iccProfileService,
            IccProfileByteCache iccProfileCache)
        {
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
            var bytes = iccProfileCache.GetOrDecode(profileToken, profileStream, filterProvider, scanner);

            if (bytes.IsEmpty)
            {
                return null;
            }

            return iccProfileService.TryGetProfile(bytes, out var profile) ? profile : null;
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
