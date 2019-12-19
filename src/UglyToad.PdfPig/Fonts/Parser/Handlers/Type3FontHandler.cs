namespace UglyToad.PdfPig.Fonts.Parser.Handlers
{
    using Cmap;
    using Core;
    using Encodings;
    using Exceptions;
    using Filters;
    using Geometry;
    using IO;
    using PdfPig.Parser.Parts;
    using Simple;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal class Type3FontHandler : IFontHandler
    {
        private readonly IFilterProvider filterProvider;
        private readonly IEncodingReader encodingReader;
        private readonly IPdfTokenScanner scanner;

        public Type3FontHandler(IPdfTokenScanner scanner, IFilterProvider filterProvider,
            IEncodingReader encodingReader)
        {
            this.filterProvider = filterProvider;
            this.encodingReader = encodingReader;
            this.scanner = scanner;
        }

        public IFont Generate(DictionaryToken dictionary, bool isLenientParsing)
        {
            var boundingBox = GetBoundingBox(dictionary);

            var fontMatrix = GetFontMatrix(dictionary);

            var firstCharacter = FontDictionaryAccessHelper.GetFirstCharacter(dictionary);
            var lastCharacter = FontDictionaryAccessHelper.GetLastCharacter(dictionary);
            var widths = FontDictionaryAccessHelper.GetWidths(scanner, dictionary, isLenientParsing);
            
            Encoding encoding = encodingReader.Read(dictionary, isLenientParsing);

            CMap toUnicodeCMap = null;
            if (dictionary.TryGet(NameToken.ToUnicode, out var toUnicodeObj))
            {
                var toUnicode = DirectObjectFinder.Get<StreamToken>(toUnicodeObj, scanner);

                var decodedUnicodeCMap = toUnicode?.Decode(filterProvider);

                if (decodedUnicodeCMap != null)
                {
                    toUnicodeCMap = CMapCache.Parse(new ByteArrayInputBytes(decodedUnicodeCMap), isLenientParsing);
                }
            }
            
            return new Type3Font(NameToken.Type3, boundingBox, fontMatrix, encoding, firstCharacter,
                lastCharacter, widths, toUnicodeCMap);
        }

        private TransformationMatrix GetFontMatrix(DictionaryToken dictionary)
        {
            if (!dictionary.TryGet(NameToken.FontMatrix, out var matrixObject))
            {
                throw new InvalidFontFormatException($"No font matrix found: {dictionary}.");
            }

            var matrixArray = DirectObjectFinder.Get<ArrayToken>(matrixObject, scanner);
            
            return TransformationMatrix.FromValues(matrixArray.GetNumeric(0).Data, matrixArray.GetNumeric(1).Data,
                matrixArray.GetNumeric(2).Data, matrixArray.GetNumeric(3).Data, matrixArray.GetNumeric(4).Data,
                matrixArray.GetNumeric(5).Data);
        }
        
        private static PdfRectangle GetBoundingBox(DictionaryToken dictionary)
        {
            if (!dictionary.TryGet(NameToken.FontBbox, out var bboxObject))
            {
                throw new InvalidFontFormatException($"Type 3 font was invalid. No Font Bounding Box: {dictionary}.");
            }

            if (bboxObject is ArrayToken bboxArray)
            {
                return new PdfRectangle(bboxArray.GetNumeric(0).Data, bboxArray.GetNumeric(1).Data,
                    bboxArray.GetNumeric(2).Data, bboxArray.GetNumeric(3).Data);
            }

            return new PdfRectangle(0, 0, 0, 0);
        }
    }
}
