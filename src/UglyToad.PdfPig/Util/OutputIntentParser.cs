namespace UglyToad.PdfPig.Util
{
    using System;
    using Filters;
    using Graphics.Colors;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using UglyToad.PdfPig.Graphics.Colors.Icc;

    /// <summary>
    /// Resolves the document's output intent ICC profile from the catalog's
    /// <c>/OutputIntents</c> array. PDF/X files characterize their device colour
    /// (DeviceCMYK / DeviceRGB / DeviceGray) through the output intent's
    /// <c>/DestOutputProfile</c>; rendering those device colours through that profile
    /// (rather than a fixed approximation) is what keeps colour-managed content and
    /// device-colour content visually consistent.
    /// </summary>
    internal static class OutputIntentParser
    {
        /// <summary>
        /// Try to resolve and parse the first usable output intent from the
        /// <c>/OutputIntents</c> array of the given dictionary. This works for both the document
        /// catalog and a page object (PDF 2.0, Table 31), which may each carry <c>/OutputIntents</c>.
        /// </summary>
        public static OutputIntent? Create(DictionaryToken dictionary,
            IPdfTokenScanner scanner,
            ILookupFilterProvider filterProvider,
            IIccProfileService? iccProfileService)
        {
            if (iccProfileService is null)
            {
                return null;
            }

            if (!dictionary.TryGet(NameToken.OutputIntents, scanner, out ArrayToken? outputIntents) || outputIntents is null)
            {
                return null;
            }

            // An output intent that embeds a usable DestOutputProfile is preferred, because only that
            // can drive colour management. The first such entry is returned immediately. Otherwise the
            // first parsable entry is kept as a fallback so a reference-only output intent (DestOutputProfileRef,
            // PDF 2.0) and its metadata are still surfaced rather than silently dropped.
            OutputIntent? fallback = null;

            foreach (var entry in outputIntents.Data)
            {
                if (!DirectObjectFinder.TryGet(entry, scanner, out DictionaryToken? intentDictionary) ||
                    intentDictionary is null)
                {
                    continue;
                }

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
                if (intentDictionary.TryGet(NameToken.Create("DestOutputProfileRef"), scanner, out DictionaryToken? refDictionary) && refDictionary is not null)
                {
                    destOutputProfileRef = ParseProfileReference(refDictionary, scanner);
                }

                intentDictionary.TryGet(NameToken.MixingHints, scanner, out DictionaryToken? mixingHints);
                intentDictionary.TryGet(NameToken.Create("SpectralData"), scanner, out DictionaryToken? spectralData);

                // The embedded profile is optional and parsed leniently: a missing or unreadable
                // DestOutputProfile leaves the colour-management transform null but must not abort
                // resolution of the remaining entries.
                IIccProfile? profile = TryParseDestOutputProfile(intentDictionary, scanner, filterProvider, iccProfileService);

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

        private static IIccProfile? TryParseDestOutputProfile(DictionaryToken intentDictionary,
            IPdfTokenScanner scanner, ILookupFilterProvider filterProvider, IIccProfileService iccProfileService)
        {
            if (!intentDictionary.TryGet(NameToken.DestOutputProfile, scanner, out StreamToken? profileStream) ||
                profileStream is null)
            {
                return null;
            }

            Memory<byte> bytes;
            try
            {
                bytes = profileStream.Decode(filterProvider, scanner);
            }
            catch
            {
                return null;
            }

            int components = GetComponentCount(profileStream, scanner, bytes.Span);
            if (components <= 0)
            {
                return null;
            }

            return iccProfileService.TryGetProfile(bytes, components, out var profile) ? profile : null;
        }

        private static IccProfileReference ParseProfileReference(DictionaryToken refDictionary, IPdfTokenScanner scanner)
        {
            string? profileCS = null;
            if (refDictionary.TryGet(NameToken.Create("ProfileCS"), scanner, out StringToken? profileCsString))
            {
                profileCS = profileCsString.Data;
            }
            else if (refDictionary.TryGet(NameToken.Create("ProfileCS"), scanner, out NameToken? profileCsName))
            {
                profileCS = profileCsName.Data;
            }

            string? profileName = null;
            if (refDictionary.TryGet(NameToken.Create("ProfileName"), scanner, out StringToken? profileNameString))
            {
                profileName = profileNameString.Data;
            }

            byte[]? iccVersion = null;
            if (refDictionary.TryGet(NameToken.Create("ICCVersion"), scanner, out StringToken? iccVersionString))
            {
                iccVersion = iccVersionString.GetBytes();
            }

            byte[]? checkSum = null;
            if (refDictionary.TryGet(NameToken.Create("CheckSum"), scanner, out StringToken? checkSumString))
            {
                checkSum = checkSumString.GetBytes();
            }

            refDictionary.TryGet(NameToken.Create("ColorantTable"), scanner, out DictionaryToken? colorantTable);
            refDictionary.TryGet(NameToken.Create("URLs"), scanner, out ArrayToken? urls);

            return new IccProfileReference(profileCS, profileName, iccVersion, checkSum, colorantTable, urls);
        }

        private static int GetComponentCount(StreamToken profileStream, IPdfTokenScanner scanner, ReadOnlySpan<byte> bytes)
        {
            // /N is optional on a DestOutputProfile stream; prefer it when present.
            if (profileStream.StreamDictionary.TryGet(NameToken.N, scanner, out NumericToken? n) && n is not null)
            {
                return n.Int;
            }

            // Otherwise derive from the ICC profile header's data colour space
            // (ICC.1 §7.2.6, bytes 16-19) once the 'acsp' signature is confirmed.
            if (bytes.Length < 20 || !(bytes[36] == (byte)'a' && bytes[37] == (byte)'c' && bytes[38] == (byte)'s' && bytes[39] == (byte)'p'))
            {
                return 0;
            }

            // 4-character data colour space signature.
            uint sig = (uint)((bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19]);
            return sig switch
            {
                0x47524159 => 1, // 'GRAY'
                0x52474220 => 3, // 'RGB '
                0x4C616220 => 3, // 'Lab '
                0x58595A20 => 3, // 'XYZ '
                0x434D594B => 4, // 'CMYK'
                _ => 0
            };
        }


    }
}
