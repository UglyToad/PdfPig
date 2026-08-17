namespace UglyToad.PdfPig.PdfFonts.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Fonts;
    using Fonts.Encodings;
    using PdfPig.Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;

    internal class EncodingReader : IEncodingReader
    {
        private readonly IPdfTokenScanner pdfScanner;
        private readonly ParsingOptions parsingOptions;

        public EncodingReader(IPdfTokenScanner pdfScanner, ParsingOptions parsingOptions)
        {
            this.pdfScanner = pdfScanner;
            this.parsingOptions = parsingOptions;
        }

        public Encoding? Read(
            DictionaryToken fontDictionary,
            FontDescriptor? descriptor = null,
            Encoding? fontEncoding = null)
        {
            if (!fontDictionary.TryGet(NameToken.Encoding, out var baseEncodingObject))
            {
                return null;
            }

            if (DirectObjectFinder.TryGet(baseEncodingObject, pdfScanner, out NameToken? name))
            {
                if (TryGetNamedEncoding(descriptor, name, out var namedEncoding))
                {
                    return namedEncoding;
                }

                if (fontDictionary.TryGet(NameToken.BaseFont, pdfScanner, out NameToken? baseFontName))
                {
                    if (string.Equals(baseFontName.Data, "ZapfDingbats", StringComparison.OrdinalIgnoreCase))
                    {
                        return ZapfDingbatsEncoding.Instance;
                    }

                    if (string.Equals(baseFontName.Data, "Symbol", StringComparison.OrdinalIgnoreCase))
                    {
                        return SymbolEncoding.Instance;
                    }

                    return WinAnsiEncoding.Instance;
                }
            }

            DictionaryToken? encodingDictionary = DirectObjectFinder.Get<DictionaryToken>(baseEncodingObject, pdfScanner);

            var encoding = ReadEncodingDictionary(encodingDictionary, fontEncoding);

            return encoding;
        }

        private Encoding? ReadEncodingDictionary(DictionaryToken? encodingDictionary, Encoding? fontEncoding)
        {
            if (encodingDictionary is null)
            {
                return null;
            }
            
            Encoding? baseEncoding;
            if (encodingDictionary.TryGet(NameToken.BaseEncoding, out var baseEncodingToken) && baseEncodingToken is NameToken baseEncodingName)
            {
                if (!Encoding.TryGetNamedEncoding(baseEncodingName, out baseEncoding))
                {
                    if (!parsingOptions.UseLenientParsing)
                    {
                        throw new InvalidFontFormatException($"No encoding found with name '{baseEncodingName}' to use as base encoding.");
                    }

                    if (NameToken.PdfDocEncoding.Equals(baseEncodingName))
                    {
                        // PdfDocEncoding is not valid here, but we are using LenientParsing
                        parsingOptions.Logger.Warn($"'{NameToken.PdfDocEncoding}' encoding found to use as base encoding, using it even if not valid.");
                        baseEncoding = PdfDocEncoding.Instance;
                    }
                    else
                    {
                        // We treat the 'baseEncodingName' as absent
                        parsingOptions.Logger.Warn($"No encoding found with name '{baseEncodingName}' to use as base encoding, falling back to the font encoding.");
                        baseEncoding = fontEncoding ?? StandardEncoding.Instance;
                    }
                }
            }
            else
            {
                // TODO: This isn't true for non-symbolic fonts or latin fonts (based on OS?) see section 5.5.5
                baseEncoding = fontEncoding ?? StandardEncoding.Instance;
            }

            if (!encodingDictionary.TryGet(NameToken.Differences, out var differencesBase))
            {
                return baseEncoding;
            }

            var differenceArray = DirectObjectFinder.Get<ArrayToken>(differencesBase, pdfScanner);

            var differences = ProcessDifferences(differenceArray);

            var newEncoding = new DifferenceBasedEncoding(baseEncoding, differences);

            return newEncoding;
        }

        private static IReadOnlyList<(int, string)> ProcessDifferences(ArrayToken? differenceArray)
        {
            var differences = new List<(int, string)>();

            if (differenceArray is null || differenceArray.Length == 0)
            {
                return differences;
            }

            var currentIndex = -1;
            foreach (var differenceEntry in differenceArray.Data)
            {
                if (differenceEntry is NumericToken number)
                {
                    currentIndex = number.Int;
                }
                else if (differenceEntry is NameToken name)
                {
                    differences.Add((currentIndex, name.Data));
                    currentIndex++;
                }
                else
                {
                    throw new InvalidFontFormatException($"Unexpected entry in the differences array: {differenceArray}.");
                }
            }

            return differences;
        }

        private static bool TryGetNamedEncoding(FontDescriptor? descriptor, NameToken encodingName, [NotNullWhen(true)] out Encoding? encoding)
        {
            encoding = null;
            // Symbolic fonts default to standard encoding.
            if (descriptor?.Flags.HasFlag(FontDescriptorFlags.Symbolic) == true)
            {
                encoding = StandardEncoding.Instance;
            }

            if (!Encoding.TryGetNamedEncoding(encodingName, out encoding))
            {
                return false;
            }

            return encoding is not null;
        }
    }
}