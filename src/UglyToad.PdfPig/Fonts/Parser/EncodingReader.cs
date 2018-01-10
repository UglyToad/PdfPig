namespace UglyToad.PdfPig.Fonts.Parser
{
    using System.Collections.Generic;
    using ContentStream;
    using Cos;
    using Encodings;
    using Exceptions;
    using IO;
    using PdfPig.Parser;
    using PdfPig.Parser.Parts;

    internal class EncodingReader : IEncodingReader
    {
        private readonly IPdfObjectParser pdfObjectParser;

        public EncodingReader(IPdfObjectParser pdfObjectParser)
        {
            this.pdfObjectParser = pdfObjectParser;
        }

        public Encoding Read(PdfDictionary fontDictionary, IRandomAccessRead reader, bool isLenientParsing, FontDescriptor descriptor = null)
        {
            if (!fontDictionary.TryGetValue(CosName.ENCODING, out var baseEncodingObject))
            {
                return null;
            }

            if (baseEncodingObject is CosName name)
            {
                return GetNamedEncoding(descriptor, name);
            }

            PdfDictionary encodingDictionary;
            if (baseEncodingObject is CosObject reference)
            {
                encodingDictionary = DirectObjectFinder.Find<PdfDictionary>(reference, pdfObjectParser, reader, isLenientParsing);
            }
            else if (baseEncodingObject is PdfDictionary dictionary)
            {
                encodingDictionary = dictionary;
            }
            else
            {
                throw new InvalidFontFormatException($"The font encoding was not a named entry or dictionary, instead it was: {baseEncodingObject}.");
            }

            var encoding = ReadEncodingDictionary(encodingDictionary, reader, isLenientParsing);

            return encoding;
        }

        private Encoding ReadEncodingDictionary(PdfDictionary encodingDictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            Encoding baseEncoding;
            if (encodingDictionary.TryGetName(CosName.BASE_ENCODING, out var baseEncodingName))
            {
                if (!Encoding.TryGetNamedEncoding(baseEncodingName, out baseEncoding))
                {
                    throw new InvalidFontFormatException($"No encoding found with name {baseEncodingName} to use as base encoding.");
                }
            }
            else
            {
                // TODO: This isn't true for non-symbolic fonts or latin fonts (based on OS?) see section 5.5.5
                baseEncoding = StandardEncoding.Instance;
            }

            if (!encodingDictionary.TryGetValue(CosName.DIFFERENCES, out var differencesBase))
            {
                return baseEncoding;
            }

            var differenceArray = differencesBase as COSArray;
            if (differenceArray == null)
            {
                if (differencesBase is CosObject differencesObj)
                {
                    differenceArray = DirectObjectFinder.Find<COSArray>(differencesObj, pdfObjectParser, reader, isLenientParsing);
                }
                else
                {
                    throw new InvalidFontFormatException($"Differences was not an array: {differencesBase}.");
                }
            }

            var differences = ProcessDifferences(differenceArray);

            var newEncoding = new DifferenceBasedEncoding(baseEncoding, differences);

            return newEncoding;
        }

        private static IReadOnlyList<(int, string)> ProcessDifferences(COSArray differenceArray)
        {
            var activeCode = differenceArray.getInt(0);
            var differences = new List<(int, string)>();

            for (int i = 1; i < differenceArray.Count; i++)
            {
                var entry = differenceArray.get(i);

                if (entry is ICosNumber numeric)
                {
                    activeCode = numeric.AsInt();
                }
                else if (entry is CosName name)
                {
                    differences.Add((activeCode, name.Name));
                    activeCode++;
                }
                else
                {
                    throw new InvalidFontFormatException($"Unexpected entry in the differences array: {differenceArray}.");
                }
            }

            return differences;
        }

        private static Encoding GetNamedEncoding(FontDescriptor descriptor, CosName encodingName)
        {
            Encoding encoding;
            // Symbolic fonts default to standard encoding.
            if (descriptor?.Flags.HasFlag(FontFlags.Symbolic) == true)
            {
                encoding = StandardEncoding.Instance;
            }

            if (!Encoding.TryGetNamedEncoding(encodingName, out encoding))
            {
                // TODO: PDFBox would not throw here.
                throw new InvalidFontFormatException($"Unrecognised encoding name: {encodingName}");
            }

            return encoding;
        }
    }
}

