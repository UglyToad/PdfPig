namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Linq;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Parser.Parts;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;

    internal static class PatternParser
    {
        public static PatternColor Create(IToken pattern, IPdfTokenScanner scanner, IResourceStore resourceStore, ILookupFilterProvider filterProvider)
        {
            DictionaryToken patternDictionary;
            StreamToken? patternStream = null;

            if (DirectObjectFinder.TryGet(pattern, scanner, out StreamToken? fs))
            {
                patternDictionary = fs.StreamDictionary;
                patternStream = new StreamToken(fs.StreamDictionary, fs.Decode(filterProvider, scanner).ToArray());
            }
            else if (DirectObjectFinder.TryGet(pattern, scanner, out DictionaryToken? fd))
            {
                patternDictionary = fd;
            }
            else
            {
                throw new PdfDocumentFormatException($"Invalid Pattern token encountered in page resource dictionary: {pattern}.");
            }

            if (!patternDictionary.Data.ContainsKey(NameToken.PatternType))
            {
                throw new Exception("TODO");
            }

            int patternType = ((NumericToken)patternDictionary.Data[NameToken.PatternType]).Int;

            TransformationMatrix matrix;
            if ((patternDictionary.Data.ContainsKey(NameToken.Matrix) &&
                DirectObjectFinder.TryGet(patternDictionary.Data[NameToken.Matrix], scanner, out ArrayToken? patternMatrix)))
            {
                matrix = TransformationMatrix.FromArray(patternMatrix.Data.OfType<NumericToken>().Select(n => n.Data).ToArray());
            }
            else
            {
                // optional - Default value: the identity matrix [1 0 0 1 0 0]
                matrix = TransformationMatrix.FromArray([1, 0, 0, 1, 0, 0]);
            }

            DictionaryToken? patternExtGState = null;
            if (!(patternDictionary.Data.ContainsKey(NameToken.ExtGState) &&
                DirectObjectFinder.TryGet(patternDictionary.Data[NameToken.ExtGState], scanner, out patternExtGState)))
            {
                // optional
            }

            return patternType switch {
                // Tiling
                1 => CreateTilingPattern(patternStream!, patternExtGState!, matrix, scanner),
                // Shading
                2 => CreateShadingPattern(patternDictionary, patternExtGState, matrix, scanner, resourceStore, filterProvider),
                _ => throw new PdfDocumentFormatException($"Invalid Pattern type encountered in page resource dictionary: {patternType}.")
            };
        }

        private static PatternColor CreateTilingPattern(StreamToken patternStream, DictionaryToken patternExtGState,
            TransformationMatrix matrix, IPdfTokenScanner scanner)
        {
            if (!patternStream.StreamDictionary.TryGet<NumericToken>(NameToken.PaintType, scanner, out var paintTypeToken))
            {
                // 1 - Coloured tiling pattern
                // 2 - Uncoloured tiling pattern
                throw new PdfDocumentFormatException($"Invalid Pattern token encountered.");
            }

            // Coloured Tiling Patterns - This type of pattern is identified by a pattern type of 1 and a paint type of 1 in the pattern dictionary.
            // Uncoloured Tiling Patterns - This type of pattern shall be identified by a pattern type of 1 and a paint type of 2 in the pattern dictionary.

            if (!patternStream.StreamDictionary.TryGet<NumericToken>(NameToken.TilingType, scanner, out var tilingTypeToken))
            {
                // 1 - Constant spacing
                // 2 - No distortion
                // 3 - Constant spacing and faster tiling
                throw new PdfDocumentFormatException($"Invalid Pattern token encountered.");
            }

            if (!patternStream.StreamDictionary.TryGet<ArrayToken>(NameToken.Bbox, scanner, out var bboxToken))
            {
                throw new PdfDocumentFormatException($"Invalid Pattern token encountered.");
            }

            if (!patternStream.StreamDictionary.TryGet<NumericToken>(NameToken.XStep, scanner, out var xStepToken))
            {
                throw new PdfDocumentFormatException($"Invalid Pattern token encountered.");
            }

            if (!patternStream.StreamDictionary.TryGet<NumericToken>(NameToken.YStep, scanner, out var yStepToken))
            {
                throw new PdfDocumentFormatException($"Invalid Pattern token encountered.");
            }

            if (!patternStream.StreamDictionary.TryGet<DictionaryToken>(NameToken.Resources, scanner, out var resourcesToken))
            {
                throw new PdfDocumentFormatException($"Invalid Pattern token encountered.");
            }

            return new TilingPatternColor(matrix, patternExtGState, patternStream, (PatternPaintType)paintTypeToken.Int,
                (PatternTilingType)tilingTypeToken.Int, bboxToken.ToRectangle(scanner), xStepToken.Double, yStepToken.Double, resourcesToken,
                patternStream.Data);
        }

        private static PatternColor CreateShadingPattern(DictionaryToken patternDictionary, DictionaryToken? patternExtGState,
            TransformationMatrix matrix, IPdfTokenScanner scanner, IResourceStore resourceStore,
            ILookupFilterProvider filterProvider)
        {
            IToken shadingToken = patternDictionary.Data[NameToken.Shading];
            Shading patternShading;
            if (DirectObjectFinder.TryGet(shadingToken, scanner, out DictionaryToken? patternShadingDictionary))
            {
                patternShading = ShadingParser.Create(patternShadingDictionary, scanner, resourceStore, filterProvider);
            }
            else if (DirectObjectFinder.TryGet(shadingToken, scanner, out StreamToken? patternShadingStream))
            {
                patternShading = ShadingParser.Create(patternShadingStream, scanner, resourceStore, filterProvider);
            }
            else
            {
                throw new ArgumentException("TODO");
            }
            return new ShadingPatternColor(matrix, patternExtGState!, patternDictionary, patternShading);
        }
    }
}
