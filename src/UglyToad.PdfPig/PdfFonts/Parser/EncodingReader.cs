namespace UglyToad.PdfPig.PdfFonts.Parser
{
    using System;
    using System.Collections.Generic;
    using Fonts;
    using Fonts.Encodings;
    using PdfPig.Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal class EncodingReader : IEncodingReader
    {
        private readonly IPdfTokenScanner pdfScanner;

        public EncodingReader(IPdfTokenScanner pdfScanner)
        {
            this.pdfScanner = pdfScanner;
        }

        public Encoding Read(DictionaryToken fontDictionary, FontDescriptor descriptor = null,
            Encoding fontEncoding = null)
        {
            if (!fontDictionary.TryGet(NameToken.Encoding, out var baseEncodingObject))
            {
                return null;
            }

            if (DirectObjectFinder.TryGet(baseEncodingObject, pdfScanner, out NameToken name))
            {
                if (TryGetNamedEncoding(descriptor, name, out var namedEncoding))
                {
                    return namedEncoding;
                }

                if (fontDictionary.TryGet(NameToken.BaseFont, pdfScanner, out NameToken baseFontName))
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

            DictionaryToken encodingDictionary = DirectObjectFinder.Get<DictionaryToken>(baseEncodingObject, pdfScanner);

            var encoding = ReadEncodingDictionary(encodingDictionary, fontEncoding);

            return encoding;
        }

        private Encoding ReadEncodingDictionary(DictionaryToken encodingDictionary, Encoding fontEncoding)
        {
            Encoding baseEncoding;
            if (encodingDictionary.TryGet(NameToken.BaseEncoding, out var baseEncodingToken) && baseEncodingToken is NameToken baseEncodingName)
            {
                if (!Encoding.TryGetNamedEncoding(baseEncodingName, out baseEncoding))
                {
                    throw new InvalidFontFormatException($"No encoding found with name {baseEncodingName} to use as base encoding.");
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

        private static IReadOnlyList<(int, string)> ProcessDifferences(ArrayToken differenceArray)
        {
            var differences = new List<(int, string)>();

            if (differenceArray.Length == 0)
            {
                return differences;
            }

            var activeCode = differenceArray.GetNumeric(0).Int;

            for (int i = 1; i < differenceArray.Data.Count; i++)
            {
                var entry = differenceArray.Data[i];

                if (entry is NumericToken numeric)
                {
                    activeCode = numeric.Int;
                }
                else if (entry is NameToken name)
                {
                    differences.Add((activeCode, name.Data));
                    activeCode++;
                }
                else
                {
                    throw new InvalidFontFormatException($"Unexpected entry in the differences array: {differenceArray}.");
                }
            }

            return differences;
        }

        private static bool TryGetNamedEncoding(FontDescriptor descriptor, NameToken encodingName, out Encoding encoding)
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

            return true;
        }
    }
}

