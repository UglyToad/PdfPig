namespace UglyToad.PdfPig.PdfFonts.Parser.Handlers
{
    using System.Collections.Generic;
    using Cmap;
    using Core;
    using Fonts;
    using Fonts.Encodings;
    using PdfPig.Parser.Parts;
    using Simple;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal class Type3FontHandler : IFontHandler
    {
        private readonly IEncodingReader encodingReader;
        private readonly IPdfTokenScanner scanner;
        private readonly CMapLocalCache cmapLocalCache;

        public Type3FontHandler(IPdfTokenScanner scanner,
            IEncodingReader encodingReader,
            CMapLocalCache cMapLocalCache)
        {
            this.encodingReader = encodingReader;
            this.scanner = scanner;
            this.cmapLocalCache = cMapLocalCache;
        }

        public IFont Generate(DictionaryToken dictionary)
        {
            var boundingBox = GetBoundingBox(dictionary);

            var fontMatrix = GetFontMatrix(dictionary);

            if (boundingBox.Left == 0 && boundingBox.Bottom == 0 && boundingBox.Height == 0 && boundingBox.Width == 0 
                && fontMatrix.A != 0 && fontMatrix.D != 0)
            {
                boundingBox = new PdfRectangle(0, 0, 1 / fontMatrix.A, 1 / fontMatrix.D);
            }

            var firstCharacter = FontDictionaryAccessHelper.GetFirstCharacter(dictionary);
            var lastCharacter = FontDictionaryAccessHelper.GetLastCharacter(dictionary);
            var widths = FontDictionaryAccessHelper.GetWidths(scanner, dictionary);
            
            Encoding? encoding = encodingReader.Read(dictionary);

            CMap? toUnicodeCMap = null;
            if (dictionary.TryGet(NameToken.ToUnicode, out var toUnicodeObj))
            {
                var toUnicode = DirectObjectFinder.Get<StreamToken>(toUnicodeObj, scanner);
                if (toUnicode is not null)
                {
                    cmapLocalCache.TryGet(toUnicode, out toUnicodeCMap);
                }
            }

            var name = GetFontName(dictionary);

            var charProcs = ReadCharProcs(dictionary);
            dictionary.TryGet(NameToken.Resources, scanner, out DictionaryToken? resources);

            return new Type3Font(name, boundingBox, fontMatrix, encoding!,
                firstCharacter,
                lastCharacter, widths, toUnicodeCMap!, charProcs, resources);
        }

        private IReadOnlyDictionary<string, StreamToken>? ReadCharProcs(DictionaryToken dictionary)
        {
            if (!dictionary.TryGet(NameToken.CharProcs, scanner, out DictionaryToken? charProcsDictionary))
            {
                return null;
            }

            var result = new Dictionary<string, StreamToken>(charProcsDictionary.Data.Count);
            foreach (var entry in charProcsDictionary.Data)
            {
                if (DirectObjectFinder.TryGet(entry.Value, scanner, out StreamToken? charProcStream))
                {
                    result[entry.Key] = charProcStream;
                }
            }
            return result;
        }

        private NameToken GetFontName(DictionaryToken dictionary)
        {
            if (dictionary.TryGet(NameToken.Name, scanner, out NameToken? fontName))
            {
                return fontName;
            }

            return NameToken.Type3;
        }

        private TransformationMatrix GetFontMatrix(DictionaryToken dictionary)
        {
            if (!dictionary.TryGet(NameToken.FontMatrix, out var matrixObject))
            {
                throw new InvalidFontFormatException($"No font matrix found: {dictionary}.");
            }

            var matrixArray = DirectObjectFinder.Get<ArrayToken>(matrixObject, scanner);
            if (matrixArray is null)
            {
                throw new InvalidFontFormatException($"Invalid font matrix found: {dictionary} (token: {matrixObject}).");
            }

            return TransformationMatrix.FromValues(matrixArray.GetNumeric(0).Double, matrixArray.GetNumeric(1).Double,
                matrixArray.GetNumeric(2).Double, matrixArray.GetNumeric(3).Double, matrixArray.GetNumeric(4).Double,
                matrixArray.GetNumeric(5).Double);
        }
        
        private static PdfRectangle GetBoundingBox(DictionaryToken dictionary)
        {
            if (!dictionary.TryGet(NameToken.FontBbox, out var bboxObject))
            {
                throw new InvalidFontFormatException($"Type 3 font was invalid. No Font Bounding Box: {dictionary}.");
            }

            if (bboxObject is ArrayToken bboxArray)
            {
                return new PdfRectangle(bboxArray.GetNumeric(0).Double, bboxArray.GetNumeric(1).Double,
                    bboxArray.GetNumeric(2).Double, bboxArray.GetNumeric(3).Double);
            }

            return new PdfRectangle(0, 0, 0, 0);
        }
    }
}
