namespace UglyToad.Pdf.Fonts.Parser.Handlers
{
    using System;
    using Cmap;
    using ContentStream;
    using Core;
    using Cos;
    using Encodings;
    using Exceptions;
    using Filters;
    using Geometry;
    using IO;
    using Pdf.Parser;
    using Pdf.Parser.Parts;
    using Simple;

    internal class Type3FontHandler : IFontHandler
    {
        private readonly IPdfObjectParser pdfObjectParser;
        private readonly CMapCache cMapCache;
        private readonly IFilterProvider filterProvider;
        private readonly IEncodingReader encodingReader;

        public Type3FontHandler(IPdfObjectParser pdfObjectParser, CMapCache cMapCache, IFilterProvider filterProvider, IEncodingReader encodingReader)
        {
            this.pdfObjectParser = pdfObjectParser;
            this.cMapCache = cMapCache;
            this.filterProvider = filterProvider;
            this.encodingReader = encodingReader;
        }

        public IFont Generate(PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            var boundingBox = GetBoundingBox(dictionary);

            var fontMatrix = GetFontMatrix(dictionary, reader, isLenientParsing);

            var firstCharacter = FontDictionaryAccessHelper.GetFirstCharacter(dictionary);
            var lastCharacter = FontDictionaryAccessHelper.GetLastCharacter(dictionary);
            var widths = FontDictionaryAccessHelper.GetWidths(pdfObjectParser, dictionary, reader, isLenientParsing);
            
            Encoding encoding = encodingReader.Read(dictionary, reader, isLenientParsing);

            CMap toUnicodeCMap = null;
            if (dictionary.TryGetItemOfType(CosName.TO_UNICODE, out CosObject toUnicodeObj))
            {
                var toUnicode = pdfObjectParser.Parse(toUnicodeObj.ToIndirectReference(), reader, isLenientParsing) as PdfRawStream;

                var decodedUnicodeCMap = toUnicode?.Decode(filterProvider);

                if (decodedUnicodeCMap != null)
                {
                    toUnicodeCMap = cMapCache.Parse(new ByteArrayInputBytes(decodedUnicodeCMap), isLenientParsing);
                }
            }
            
            return new Type3Font(CosName.TYPE3, boundingBox, fontMatrix, encoding, firstCharacter,
                lastCharacter, widths, toUnicodeCMap);
        }

        private TransformationMatrix GetFontMatrix(PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            if (!dictionary.TryGetValue(CosName.FONT_MATRIX, out var matrixObject))
            {
                throw new InvalidFontFormatException($"No font matrix found: {dictionary}.");
            }

            COSArray matrixArray;
            if (matrixObject is COSArray arr)
            {
                matrixArray = arr;
            }
            else if (matrixObject is CosObject obj)
            {
                matrixArray = DirectObjectFinder.Find<COSArray>(obj, pdfObjectParser, reader, isLenientParsing);
            }
            else
            {
                throw new InvalidFontFormatException($"The font matrix object was not an array or reference to an array: {matrixObject}.");
            }

            return TransformationMatrix.FromValues(GetDecimal(matrixArray, 0), GetDecimal(matrixArray, 1),
                GetDecimal(matrixArray, 2), GetDecimal(matrixArray, 3), GetDecimal(matrixArray, 4), GetDecimal(matrixArray, 5));
        }

        private Encoding GetEncoding(CosBase baseObject, IRandomAccessRead reader, bool isLenientParsing)
        {
            if (baseObject is CosObject obj)
            {
                baseObject = pdfObjectParser.Parse(obj.ToIndirectReference(), reader, isLenientParsing);
            }

            if (baseObject is CosName encodingName)
            {
                
            }
            else if (baseObject is PdfDictionary dictionary)
            {
                
            }
            else
            {
                throw new InvalidFontFormatException("");
            }

            throw new NotImplementedException();
        }

        private static decimal GetDecimal(COSArray array, int index)
        {
            if (index >= array.Count)
            {
                throw new InvalidFontFormatException($"The array did not contain enough entries to be the font matrix: {array}.");
            }

            var item = array.get(index) as ICosNumber;

            if (item == null)
            {
                throw new InvalidFontFormatException($"The array did not contain a decimal at position {index}: {array}.");
            }

            return item.AsDecimal();
        }

        private static PdfRectangle GetBoundingBox(PdfDictionary dictionary)
        {
            if (!dictionary.TryGetValue(CosName.FONT_BBOX, out var bboxObject))
            {
                throw new InvalidFontFormatException($"Type 3 font was invalid. No Font Bounding Box: {dictionary}.");
            }

            if (bboxObject is COSArray bboxArray)
            {
                return new PdfRectangle(GetDecimal(bboxArray, 0), GetDecimal(bboxArray, 1),
                    GetDecimal(bboxArray, 2), GetDecimal(bboxArray, 3));
            }

            return new PdfRectangle(0, 0, 0, 0);
        }
    }
}
