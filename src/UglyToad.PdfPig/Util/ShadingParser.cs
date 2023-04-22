namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Linq;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Functions;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;

    internal static class ShadingParser
    {
        public static Shading Create(IToken shading, IPdfTokenScanner scanner, IResourceStore resourceStore, ILookupFilterProvider filterProvider)
        {
            DictionaryToken shadingDictionary = null;
            StreamToken shadingStream = null;

            if (shading is StreamToken fs)
            {
                shadingDictionary = fs.StreamDictionary;
                shadingStream = new StreamToken(fs.StreamDictionary, fs.Decode(filterProvider, scanner));
            }
            else if (shading is DictionaryToken fd)
            {
                shadingDictionary = fd;
            }

            ShadingType shadingType;
            if (shadingDictionary.TryGet<NumericToken>(NameToken.ShadingType, scanner, out var shadingTypeToken))
            {
                // Shading types 4 to 7 shall be defined by a stream containing descriptive data characterizing
                // the shading's gradient fill.
                if (shadingTypeToken.Int >= 4 && shadingStream == null)
                {
                    throw new ArgumentNullException(nameof(shadingStream), $"Shading type '{(ShadingType)shadingTypeToken.Int}' is not properly defined. Shading types 4 to 7 shall be defined by a stream.");
                }

                shadingType = (ShadingType)shadingTypeToken.Int;
            }
            else
            {
                throw new ArgumentNullException($"'{NameToken.ShadingType}' is required for shading.");
            }

            ColorSpaceDetails colorSpace = null;
            if (shadingDictionary.TryGet<NameToken>(NameToken.ColorSpace, scanner, out var colorSpaceToken))
            {
                colorSpace = resourceStore.GetColorSpaceDetails(colorSpaceToken, shadingDictionary);
            }
            else if (shadingDictionary.TryGet<ArrayToken>(NameToken.ColorSpace, scanner, out var colorSpaceSToken))
            {
                var first = colorSpaceSToken.Data[0];
                if (first is NameToken firstColorSpaceName)
                {
                    colorSpace = resourceStore.GetColorSpaceDetails(firstColorSpaceName, shadingDictionary);
                }
                else
                {
                    throw new ArgumentNullException("Invalid color space found in shading.");
                }
            }
            else
            {
                throw new ArgumentNullException($"'{NameToken.ColorSpace}' is required for shading.");
            }

            double[] background = null;
            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Background, scanner, out var backgroundToken))
            {
                // Optional
                background = backgroundToken.Data.OfType<NumericToken>().Select(v => v.Double).ToArray();
            }

            PdfRectangle? bbox = null;
            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Bbox, scanner, out var bboxToken))
            {
                // Optional
                bbox = bboxToken.ToRectangle(scanner);
            }

            // Optional
            bool antiAlias = shadingDictionary.TryGet<BooleanToken>(NameToken.AntiAlias, scanner, out var antiAliasToken) && antiAliasToken.Data; // Default value: false.

            switch (shadingType)
            {
                case ShadingType.FunctionBased:
                    return CreateFunctionBasedShading(shadingDictionary, colorSpace, background, bbox, antiAlias, scanner, filterProvider);

                case ShadingType.Axial:
                    return CreateAxialShading(shadingDictionary, colorSpace, background, bbox, antiAlias, scanner, filterProvider);

                case ShadingType.Radial:
                    return CreateRadialShading(shadingDictionary, colorSpace, background, bbox, antiAlias, scanner, filterProvider);

                case ShadingType.FreeFormGouraud:
                    return CreateFreeFormGouraudShadedTriangleMeshesShading(shadingStream, colorSpace, background, bbox, antiAlias, scanner, filterProvider);

                case ShadingType.LatticeFormGouraud:
                    return CreateLatticeFormGouraudShadedTriangleMeshesShading(shadingStream, colorSpace, background, bbox, antiAlias, scanner, filterProvider);

                case ShadingType.CoonsPatch:
                    return CreateCoonsPatchMeshesShading(shadingStream, colorSpace, background, bbox, antiAlias, scanner, filterProvider);

                case ShadingType.TensorProductPatch:
                    return CreateTensorProductPatchMeshesShading(shadingStream, colorSpace, background, bbox, antiAlias, scanner, filterProvider);

                default:
                    throw new PdfDocumentFormatException($"Invalid Shading type encountered in page resource dictionary: '{shadingType}'.");
            }
        }

        private static FunctionBasedShading CreateFunctionBasedShading(DictionaryToken shadingDictionary, ColorSpaceDetails colorSpace,
            double[] background, PdfRectangle? bbox, bool antiAlias, IPdfTokenScanner scanner, ILookupFilterProvider filterProvider)
        {
            double[] domain = null;
            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Domain, scanner, out var domainToken))
            {
                domain = domainToken.Data.OfType<NumericToken>().Select(v => v.Double).ToArray();
            }
            else
            {
                // Optional - Default value: [0.0 1.0 0.0 1.0].
                domain = new double[] { 0.0, 1.0, 0.0, 1.0 };
            }

            TransformationMatrix matrix;
            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Coords, scanner, out var matrixToken))
            {
                matrix = TransformationMatrix.FromArray(matrixToken.Data.OfType<NumericToken>().Select(n => n.Data).ToArray());
            }
            else
            {
                // Optional - Default value: the identity matrix [1 0 0 1 0 0]
                matrix = TransformationMatrix.FromArray(new decimal[] { 1, 0, 0, 1, 0, 0 });
            }

            if (!shadingDictionary.ContainsKey(NameToken.Function))
            {
                throw new ArgumentNullException($"'{NameToken.Function}' is required for shading type '{ShadingType.FunctionBased}'.");
            }

            PdfFunction function = PdfFunctionParser.Create(shadingDictionary.Data[NameToken.Function], scanner, filterProvider);

            return new FunctionBasedShading(antiAlias, shadingDictionary, colorSpace, bbox, background, domain, matrix, function);
        }

        private static AxialShading CreateAxialShading(DictionaryToken shadingDictionary, ColorSpaceDetails colorSpace,
            double[] background, PdfRectangle? bbox, bool antiAlias, IPdfTokenScanner scanner, ILookupFilterProvider filterProvider)
        {
            double[] coords = null;
            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Coords, scanner, out var coordsToken))
            {
                coords = coordsToken.Data.OfType<NumericToken>().Select(v => v.Double).ToArray();
            }
            else
            {
                throw new ArgumentNullException($"{NameToken.Coords} is required for shading type '{ShadingType.Axial}'.");
            }

            double[] domain = null;
            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Domain, scanner, out var domainToken))
            {
                domain = domainToken.Data.OfType<NumericToken>().Select(v => v.Double).ToArray();
            }
            else
            {
                // set default values
                domain = new double[] { 0, 1 };
            }

            if (!shadingDictionary.ContainsKey(NameToken.Function))
            {
                throw new ArgumentNullException($"{NameToken.Function} is required for shading type '{ShadingType.Axial}'.");
            }

            PdfFunction function = PdfFunctionParser.Create(shadingDictionary.Data[NameToken.Function], scanner, filterProvider);

            bool[] extend = new bool[] { false, false }; // Default values
            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Extend, scanner, out var extendToken))
            {
                extend = extendToken.Data.OfType<BooleanToken>().Select(v => v.Data).ToArray();
            }

            return new AxialShading(antiAlias, shadingDictionary, colorSpace, bbox, background, coords, domain, function, extend);
        }

        private static RadialShading CreateRadialShading(DictionaryToken shadingDictionary, ColorSpaceDetails colorSpace,
            double[] background, PdfRectangle? bbox, bool antiAlias, IPdfTokenScanner scanner, ILookupFilterProvider filterProvider)
        {
            double[] coords = null;
            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Coords, scanner, out var coordsToken))
            {
                coords = coordsToken.Data.OfType<NumericToken>().Select(v => v.Double).ToArray();
            }
            else
            {
                throw new ArgumentNullException($"{NameToken.Coords} is required for shading type '{ShadingType.Radial}'.");
            }

            double[] domain = null;
            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Domain, scanner, out var domainToken))
            {
                domain = domainToken.Data.OfType<NumericToken>().Select(v => v.Double).ToArray();
            }
            else
            {
                // set default values
                domain = new double[] { 0, 1 };
            }

            if (!shadingDictionary.ContainsKey(NameToken.Function))
            {
                throw new ArgumentNullException($"{NameToken.Function} is required for shading type '{ShadingType.Radial}'.");
            }

            PdfFunction function = PdfFunctionParser.Create(shadingDictionary.Data[NameToken.Function], scanner, filterProvider);

            bool[] extend = new bool[] { false, false }; // Default values
            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Extend, scanner, out var extendToken))
            {
                extend = extendToken.Data.OfType<BooleanToken>().Select(v => v.Data).ToArray();
            }

            return new RadialShading(antiAlias, shadingDictionary, colorSpace, bbox, background, coords, domain, function, extend);
        }

        private static FreeFormGouraudShading CreateFreeFormGouraudShadedTriangleMeshesShading(StreamToken shadingStream,
            ColorSpaceDetails colorSpace, double[] background, PdfRectangle? bbox, bool antiAlias, IPdfTokenScanner scanner, ILookupFilterProvider filterProvider)
        {
            int bitsPerCoordinate;
            if (shadingStream.StreamDictionary.TryGet<NumericToken>(NameToken.BitsPerCoordinate, scanner, out var bitsPerCoordinateToken))
            {
                bitsPerCoordinate = bitsPerCoordinateToken.Int;
            }
            else
            {
                throw new ArgumentNullException($"{NameToken.BitsPerCoordinate} is required for shading type '{ShadingType.FreeFormGouraud}'.");
            }

            int bitsPerComponent;
            if (shadingStream.StreamDictionary.TryGet<NumericToken>(NameToken.BitsPerComponent, scanner, out var bitsPerComponentToken))
            {
                bitsPerComponent = bitsPerComponentToken.Int;
            }
            else
            {
                throw new ArgumentNullException($"{NameToken.BitsPerComponent} is required for shading type '{ShadingType.FreeFormGouraud}'.");
            }

            int bitsPerFlag;
            if (shadingStream.StreamDictionary.TryGet<NumericToken>(NameToken.BitsPerFlag, scanner, out var bitsPerFlagToken))
            {
                bitsPerFlag = bitsPerFlagToken.Int;
            }
            else
            {
                throw new ArgumentNullException($"{NameToken.BitsPerFlag} is required for shading type '{ShadingType.FreeFormGouraud}'.");
            }

            double[] decode;
            if (shadingStream.StreamDictionary.TryGet<ArrayToken>(NameToken.Decode, scanner, out var decodeToken))
            {
                decode = decodeToken.Data.OfType<NumericToken>().Select(v => v.Double).ToArray();
            }
            else
            {
                throw new ArgumentNullException($"{NameToken.Decode} is required for shading type '{ShadingType.FreeFormGouraud}'.");
            }

            PdfFunction function = null; // Optional
            if (shadingStream.StreamDictionary.ContainsKey(NameToken.Function))
            {
                function = PdfFunctionParser.Create(shadingStream.StreamDictionary.Data[NameToken.Function], scanner, filterProvider);
            }

            return new FreeFormGouraudShading(antiAlias, shadingStream, colorSpace, bbox, background,
                bitsPerCoordinate, bitsPerComponent, bitsPerFlag, decode, function);
        }

        private static LatticeFormGouraudShading CreateLatticeFormGouraudShadedTriangleMeshesShading(StreamToken shadingStream,
            ColorSpaceDetails colorSpace, double[] background, PdfRectangle? bbox, bool antiAlias, IPdfTokenScanner scanner, ILookupFilterProvider filterProvider)
        {
            int bitsPerCoordinate;
            if (shadingStream.StreamDictionary.TryGet<NumericToken>(NameToken.BitsPerCoordinate, scanner, out var bitsPerCoordinateToken))
            {
                bitsPerCoordinate = bitsPerCoordinateToken.Int;
            }
            else
            {
                throw new ArgumentNullException($"{NameToken.BitsPerCoordinate} is required for shading type '{ShadingType.LatticeFormGouraud}'.");
            }

            int bitsPerComponent;
            if (shadingStream.StreamDictionary.TryGet<NumericToken>(NameToken.BitsPerComponent, scanner, out var bitsPerComponentToken))
            {
                bitsPerComponent = bitsPerComponentToken.Int;
            }
            else
            {
                throw new ArgumentNullException($"{NameToken.BitsPerComponent} is required for shading type '{ShadingType.LatticeFormGouraud}'.");
            }

            int verticesPerRow;
            if (shadingStream.StreamDictionary.TryGet<NumericToken>(NameToken.VerticesPerRow, scanner, out var verticesPerRowToken))
            {
                verticesPerRow = verticesPerRowToken.Int;
            }
            else
            {
                throw new ArgumentNullException($"{NameToken.VerticesPerRow} is required for shading type '{ShadingType.LatticeFormGouraud}'.");
            }

            double[] decode;
            if (shadingStream.StreamDictionary.TryGet<ArrayToken>(NameToken.Decode, scanner, out var decodeToken))
            {
                decode = decodeToken.Data.OfType<NumericToken>().Select(v => v.Double).ToArray();
            }
            else
            {
                throw new ArgumentNullException($"{NameToken.Decode} is required for shading type '{ShadingType.LatticeFormGouraud}'.");
            }

            PdfFunction function = null; // Optional
            if (shadingStream.StreamDictionary.ContainsKey(NameToken.Function))
            {
                function = PdfFunctionParser.Create(shadingStream.StreamDictionary.Data[NameToken.Function], scanner, filterProvider);
            }

            return new LatticeFormGouraudShading(antiAlias, shadingStream, colorSpace, bbox, background,
                bitsPerCoordinate, bitsPerComponent, verticesPerRow, decode, function);
        }

        private static CoonsPatchMeshesShading CreateCoonsPatchMeshesShading(StreamToken shadingStream,
            ColorSpaceDetails colorSpace, double[] background, PdfRectangle? bbox, bool antiAlias, IPdfTokenScanner scanner, ILookupFilterProvider filterProvider)
        {
            int bitsPerCoordinate;
            if (shadingStream.StreamDictionary.TryGet<NumericToken>(NameToken.BitsPerCoordinate, scanner, out var bitsPerCoordinateToken))
            {
                bitsPerCoordinate = bitsPerCoordinateToken.Int;
            }
            else
            {
                throw new ArgumentNullException($"{NameToken.BitsPerCoordinate} is required for shading type '{ShadingType.CoonsPatch}'.");
            }

            int bitsPerComponent;
            if (shadingStream.StreamDictionary.TryGet<NumericToken>(NameToken.BitsPerComponent, scanner, out var bitsPerComponentToken))
            {
                bitsPerComponent = bitsPerComponentToken.Int;
            }
            else
            {
                throw new ArgumentNullException($"{NameToken.BitsPerComponent} is required for shading type '{ShadingType.CoonsPatch}'.");
            }

            int bitsPerFlag;
            if (shadingStream.StreamDictionary.TryGet<NumericToken>(NameToken.BitsPerFlag, scanner, out var bitsPerFlagToken))
            {
                bitsPerFlag = bitsPerFlagToken.Int;
            }
            else
            {
                throw new ArgumentNullException($"{NameToken.BitsPerFlag} is required for shading type '{ShadingType.CoonsPatch}'.");
            }

            double[] decode;
            if (shadingStream.StreamDictionary.TryGet<ArrayToken>(NameToken.Decode, scanner, out var decodeToken))
            {
                decode = decodeToken.Data.OfType<NumericToken>().Select(v => v.Double).ToArray();
            }
            else
            {
                throw new ArgumentNullException($"{NameToken.Decode} is required for shading type '{ShadingType.CoonsPatch}'.");
            }

            PdfFunction function = null; // Optional
            if (shadingStream.StreamDictionary.ContainsKey(NameToken.Function))
            {
                function = PdfFunctionParser.Create(shadingStream.StreamDictionary.Data[NameToken.Function], scanner, filterProvider);
            }

            return new CoonsPatchMeshesShading(antiAlias, shadingStream, colorSpace, bbox, background,
                bitsPerCoordinate, bitsPerComponent, bitsPerFlag, decode, function);
        }

        private static TensorProductPatchMeshesShading CreateTensorProductPatchMeshesShading(StreamToken shadingStream,
            ColorSpaceDetails colorSpace, double[] background, PdfRectangle? bbox, bool antiAlias, IPdfTokenScanner scanner, ILookupFilterProvider filterProvider)
        {
            int bitsPerCoordinate;
            if (shadingStream.StreamDictionary.TryGet<NumericToken>(NameToken.BitsPerCoordinate, scanner, out var bitsPerCoordinateToken))
            {
                bitsPerCoordinate = bitsPerCoordinateToken.Int;
            }
            else
            {
                throw new ArgumentNullException($"{NameToken.BitsPerCoordinate} is required for shading type '{ShadingType.TensorProductPatch}'.");
            }

            int bitsPerComponent;
            if (shadingStream.StreamDictionary.TryGet<NumericToken>(NameToken.BitsPerComponent, scanner, out var bitsPerComponentToken))
            {
                bitsPerComponent = bitsPerComponentToken.Int;
            }
            else
            {
                throw new ArgumentNullException($"{NameToken.BitsPerComponent} is required for shading type '{ShadingType.TensorProductPatch}'.");
            }

            int bitsPerFlag;
            if (shadingStream.StreamDictionary.TryGet<NumericToken>(NameToken.BitsPerFlag, scanner, out var bitsPerFlagToken))
            {
                bitsPerFlag = bitsPerFlagToken.Int;
            }
            else
            {
                throw new ArgumentNullException($"{NameToken.BitsPerFlag} is required for shading type '{ShadingType.TensorProductPatch}'.");
            }

            double[] decode;
            if (shadingStream.StreamDictionary.TryGet<ArrayToken>(NameToken.Decode, scanner, out var decodeToken))
            {
                decode = decodeToken.Data.OfType<NumericToken>().Select(v => v.Double).ToArray();
            }
            else
            {
                throw new ArgumentNullException($"{NameToken.Decode} is required for shading type '{ShadingType.TensorProductPatch}'.");
            }

            PdfFunction function = null; // Optional
            if (shadingStream.StreamDictionary.ContainsKey(NameToken.Function))
            {
                function = PdfFunctionParser.Create(shadingStream.StreamDictionary.Data[NameToken.Function], scanner, filterProvider);
            }

            return new TensorProductPatchMeshesShading(antiAlias, shadingStream, colorSpace, bbox, background,
                bitsPerCoordinate, bitsPerComponent, bitsPerFlag, decode, function);
        }
    }
}
