namespace UglyToad.PdfPig.Artwork
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// Reads the dictionaries of ISO 32000-2, 13.6.4 into the 3D artwork model. Tolerant by design: a missing
    /// or malformed optional entry becomes "absent", never an exception.
    /// </summary>
    internal static class Artwork3DParser
    {
        /// <summary>
        /// The most views read from one view array; further entries are ignored.
        /// </summary>
        internal const int MaxViews = 10_000;

        /// <summary>
        /// The most node dictionaries read from one view; further entries are ignored.
        /// </summary>
        internal const int MaxNodes = 100_000;

        private const int MaxReferenceHops = 32;

        /// <summary>
        /// Follows indirect references. Returns <see langword="null"/> for a missing object, the PDF null
        /// object, a reference chain that does not end, or a scanner failure.
        /// </summary>
        internal static IToken? Resolve(IToken? token, IPdfTokenScanner scanner)
        {
            try
            {
                for (var hop = 0; hop < MaxReferenceHops && token is IndirectReferenceToken reference; hop++)
                {
                    token = scanner.Get(reference.Data)?.Data;
                }
            }
            catch
            {
                return null;
            }

            return token is IndirectReferenceToken || token is NullToken ? null : token;
        }

        internal static IToken? Get(DictionaryToken dictionary, NameToken key, IPdfTokenScanner scanner)
        {
            return dictionary.TryGet(key, out IToken? raw) ? Resolve(raw, scanner) : null;
        }

        internal static DictionaryToken? GetDictionary(DictionaryToken dictionary, NameToken key, IPdfTokenScanner scanner)
        {
            return Get(dictionary, key, scanner) as DictionaryToken;
        }

        internal static ArrayToken? GetArray(DictionaryToken dictionary, NameToken key, IPdfTokenScanner scanner)
        {
            return Get(dictionary, key, scanner) as ArrayToken;
        }

        internal static double? GetNumber(DictionaryToken dictionary, NameToken key, IPdfTokenScanner scanner)
        {
            return Get(dictionary, key, scanner) is NumericToken number ? number.Data : null;
        }

        internal static bool? GetBoolean(DictionaryToken dictionary, NameToken key, IPdfTokenScanner scanner)
        {
            return Get(dictionary, key, scanner) is BooleanToken boolean ? boolean.Data : null;
        }

        internal static string? GetName(DictionaryToken dictionary, NameToken key, IPdfTokenScanner scanner)
        {
            return (Get(dictionary, key, scanner) as NameToken)?.Data;
        }

        internal static string? GetText(DictionaryToken dictionary, NameToken key, IPdfTokenScanner scanner)
        {
            return AsText(Get(dictionary, key, scanner));
        }

        /// <summary>
        /// The value of a text string token. Names are not text strings.
        /// </summary>
        internal static string? AsText(IToken? token)
        {
            return token switch
            {
                StringToken text => text.Data,
                HexToken hex => hex.Data,
                _ => null,
            };
        }

        /// <summary>
        /// Reads exactly <paramref name="count"/> numbers starting at <paramref name="offset"/>; the array must
        /// hold exactly <c>offset + count</c> entries.
        /// </summary>
        internal static bool TryGetNumbers(ArrayToken? array, IPdfTokenScanner scanner, int offset, int count, Span<double> destination)
        {
            if (array is null || array.Length != offset + count)
            {
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                if (Resolve(array.Data[offset + i], scanner) is not NumericToken number)
                {
                    return false;
                }

                destination[i] = number.Data;
            }

            return true;
        }

        internal static Artwork3DMatrix? GetMatrix(DictionaryToken dictionary, NameToken key, IPdfTokenScanner scanner)
        {
            Span<double> m = stackalloc double[12];
            if (!TryGetNumbers(GetArray(dictionary, key, scanner), scanner, 0, 12, m))
            {
                return null;
            }

            return new Artwork3DMatrix(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8], m[9], m[10], m[11]);
        }

        /// <summary>
        /// Reads a colour given either as three numbers, or as a colour space name followed by three numbers.
        /// Only DeviceRGB is valid; any other colour space makes the entry absent, as the specification requires.
        /// </summary>
        internal static Artwork3DColor? GetColor(IToken? token, IPdfTokenScanner scanner, bool leadingColorSpace)
        {
            if (token is not ArrayToken array)
            {
                return null;
            }

            if (leadingColorSpace)
            {
                if (array.Length != 4 || Resolve(array.Data[0], scanner) is not NameToken space || space.Data != NameToken.Devicergb.Data)
                {
                    return null;
                }
            }

            Span<double> rgb = stackalloc double[3];
            if (!TryGetNumbers(array, scanner, leadingColorSpace ? 1 : 0, 3, rgb))
            {
                return null;
            }

            return new Artwork3DColor(rgb[0], rgb[1], rgb[2]);
        }

        internal static bool TryParseView(IToken? token, IPdfTokenScanner scanner, [NotNullWhen(true)] out Artwork3DView? view)
        {
            if (Resolve(token, scanner) is DictionaryToken dictionary)
            {
                view = ParseView(dictionary, scanner);
                return true;
            }

            view = null;
            return false;
        }

        /// <summary>
        /// Reads a view array. Entries that are not view dictionaries are skipped.
        /// </summary>
        internal static Artwork3DView[] ParseViews(ArrayToken? array, IPdfTokenScanner scanner)
        {
            if (array is null || array.Length == 0)
            {
                return [];
            }

            var count = Math.Min(array.Length, MaxViews);
            var views = new Artwork3DView[count];
            var found = 0;
            for (var i = 0; i < count; i++)
            {
                if (TryParseView(array.Data[i], scanner, out var view))
                {
                    views[found++] = view;
                }
            }

            if (found != count)
            {
                Array.Resize(ref views, found);
            }

            return views;
        }

        private static Artwork3DView ParseView(DictionaryToken dictionary, IPdfTokenScanner scanner)
        {
            var externalName = GetText(dictionary, Artwork3DNames.XN, scanner);
            var internalName = GetText(dictionary, Artwork3DNames.IN, scanner) ?? externalName;

            var matrixSource = Artwork3DMatrixSource.None;
            switch (GetName(dictionary, Artwork3DNames.MS, scanner))
            {
                case "M":
                    matrixSource = Artwork3DMatrixSource.Matrix;
                    break;
                case "U3D":
                    matrixSource = Artwork3DMatrixSource.U3DPath;
                    break;
            }

            var projectionDictionary = GetDictionary(dictionary, NameToken.P, scanner);
            var backgroundDictionary = GetDictionary(dictionary, Artwork3DNames.BG, scanner);
            var lightingDictionary = GetDictionary(dictionary, Artwork3DNames.LS, scanner);

            return new Artwork3DView(
                dictionary,
                externalName,
                internalName,
                matrixSource,
                GetMatrix(dictionary, Artwork3DNames.C2W, scanner),
                ParseU3DPath(Get(dictionary, Artwork3DNames.U3DPath, scanner), scanner),
                GetNumber(dictionary, Artwork3DNames.CO, scanner),
                projectionDictionary is null ? null : ParseProjection(projectionDictionary, scanner),
                backgroundDictionary is null ? null : ParseBackground(backgroundDictionary, scanner),
                ParseRenderMode(GetDictionary(dictionary, Artwork3DNames.RM, scanner), scanner),
                lightingDictionary is null ? null : ParseLightingScheme(lightingDictionary, scanner),
                ParseCrossSections(GetArray(dictionary, Artwork3DNames.SA, scanner), scanner),
                ParseNodes(GetArray(dictionary, Artwork3DNames.NA, scanner), scanner),
                GetBoolean(dictionary, Artwork3DNames.NR, scanner) ?? false,
                Get(dictionary, NameToken.O, scanner) as StreamToken,
                ParseDictionaries(GetArray(dictionary, Artwork3DNames.MA, scanner), scanner));
        }

        private static IReadOnlyList<string> ParseU3DPath(IToken? token, IPdfTokenScanner scanner)
        {
            var single = AsText(token);
            if (single is not null)
            {
                return [single];
            }

            if (token is not ArrayToken array || array.Length == 0)
            {
                return [];
            }

            var path = new List<string>(array.Length);
            for (var i = 0; i < array.Length; i++)
            {
                var part = AsText(Resolve(array.Data[i], scanner));
                if (part is not null)
                {
                    path.Add(part);
                }
            }

            return path;
        }

        private static Artwork3DProjection ParseProjection(DictionaryToken dictionary, IPdfTokenScanner scanner)
        {
            var type = GetName(dictionary, NameToken.Subtype, scanner) == "O"
                ? Artwork3DProjectionType.Orthographic
                : Artwork3DProjectionType.Perspective;

            var clippingStyle = GetName(dictionary, Artwork3DNames.CS, scanner) == "XNF"
                ? Artwork3DClippingStyle.ExplicitNearFar
                : Artwork3DClippingStyle.AutomaticNearFar;

            var perspectiveScaling = Artwork3DScaling.Width;
            double? perspectiveScalingValue = null;
            switch (Get(dictionary, Artwork3DNames.PS, scanner))
            {
                case NumericToken number:
                    perspectiveScaling = Artwork3DScaling.Value;
                    perspectiveScalingValue = number.Data;
                    break;
                case NameToken name:
                    perspectiveScaling = ParseScaling(name.Data, Artwork3DScaling.Width, allowAbsolute: false);
                    break;
            }

            return new Artwork3DProjection(
                type,
                clippingStyle,
                GetNumber(dictionary, NameToken.F, scanner),
                GetNumber(dictionary, NameToken.N, scanner),
                GetNumber(dictionary, Artwork3DNames.FOV, scanner),
                perspectiveScaling,
                perspectiveScalingValue,
                GetNumber(dictionary, Artwork3DNames.OS, scanner) ?? 1,
                ParseScaling(GetName(dictionary, Artwork3DNames.OB, scanner), Artwork3DScaling.Absolute, allowAbsolute: true));
        }

        private static Artwork3DScaling ParseScaling(string? name, Artwork3DScaling fallback, bool allowAbsolute)
        {
            return name switch
            {
                "W" => Artwork3DScaling.Width,
                "H" => Artwork3DScaling.Height,
                "Min" => Artwork3DScaling.Min,
                "Max" => Artwork3DScaling.Max,
                "Absolute" when allowAbsolute => Artwork3DScaling.Absolute,
                _ => fallback,
            };
        }

        private static Artwork3DBackground ParseBackground(DictionaryToken dictionary, IPdfTokenScanner scanner)
        {
            dictionary.TryGet(Artwork3DNames.CS, out IToken? colorSpace);

            return new Artwork3DBackground(
                GetColor(Get(dictionary, NameToken.C, scanner), scanner, leadingColorSpace: false) ?? Artwork3DColor.White,
                GetBoolean(dictionary, Artwork3DNames.EA, scanner) ?? false,
                colorSpace);
        }

        internal static Artwork3DRenderMode? ParseRenderMode(DictionaryToken? dictionary, IPdfTokenScanner scanner)
        {
            if (dictionary is null)
            {
                return null;
            }

            var name = GetName(dictionary, NameToken.Subtype, scanner);
            var faceColorToken = Get(dictionary, Artwork3DNames.FC, scanner);

            return new Artwork3DRenderMode(
                ParseRenderModeType(name),
                name,
                GetColor(Get(dictionary, Artwork3DNames.AC, scanner), scanner, leadingColorSpace: true),
                GetColor(faceColorToken, scanner, leadingColorSpace: true),
                faceColorToken is NameToken faceColorName && faceColorName.Data == "BG",
                GetNumber(dictionary, NameToken.O, scanner),
                GetNumber(dictionary, Artwork3DNames.CV, scanner));
        }

        private static Artwork3DRenderModeType ParseRenderModeType(string? name)
        {
            return name switch
            {
                "Solid" => Artwork3DRenderModeType.Solid,
                "SolidWireframe" => Artwork3DRenderModeType.SolidWireframe,
                "Transparent" => Artwork3DRenderModeType.Transparent,
                "TransparentWireframe" => Artwork3DRenderModeType.TransparentWireframe,
                "BoundingBox" => Artwork3DRenderModeType.BoundingBox,
                "TransparentBoundingBox" => Artwork3DRenderModeType.TransparentBoundingBox,
                "TransparentBoundingBoxOutline" => Artwork3DRenderModeType.TransparentBoundingBoxOutline,
                "Wireframe" => Artwork3DRenderModeType.Wireframe,
                "ShadedWireframe" => Artwork3DRenderModeType.ShadedWireframe,
                "HiddenWireframe" => Artwork3DRenderModeType.HiddenWireframe,
                "Vertices" => Artwork3DRenderModeType.Vertices,
                "ShadedVertices" => Artwork3DRenderModeType.ShadedVertices,
                "Illustration" => Artwork3DRenderModeType.Illustration,
                "SolidOutline" => Artwork3DRenderModeType.SolidOutline,
                "ShadedIllustration" => Artwork3DRenderModeType.ShadedIllustration,
                _ => Artwork3DRenderModeType.Unknown,
            };
        }

        private static Artwork3DLightingScheme ParseLightingScheme(DictionaryToken dictionary, IPdfTokenScanner scanner)
        {
            var name = GetName(dictionary, NameToken.Subtype, scanner);
            var style = name switch
            {
                "Artwork" => Artwork3DLightingStyle.Artwork,
                "None" => Artwork3DLightingStyle.None,
                "White" => Artwork3DLightingStyle.White,
                "Day" => Artwork3DLightingStyle.Day,
                "Night" => Artwork3DLightingStyle.Night,
                "Hard" => Artwork3DLightingStyle.Hard,
                "Primary" => Artwork3DLightingStyle.Primary,
                "Blue" => Artwork3DLightingStyle.Blue,
                "Red" => Artwork3DLightingStyle.Red,
                "Cube" => Artwork3DLightingStyle.Cube,
                "CAD" => Artwork3DLightingStyle.CAD,
                "Headlamp" => Artwork3DLightingStyle.Headlamp,
                _ => Artwork3DLightingStyle.Unknown,
            };
            return new Artwork3DLightingScheme(style, name);
        }

        private static IReadOnlyList<Artwork3DCrossSection> ParseCrossSections(ArrayToken? array, IPdfTokenScanner scanner)
        {
            if (array is null || array.Length == 0)
            {
                return Array.Empty<Artwork3DCrossSection>();
            }

            var count = Math.Min(array.Length, MaxNodes);
            var sections = new List<Artwork3DCrossSection>(count);
            for (var i = 0; i < count; i++)
            {
                if (!(Resolve(array.Data[i], scanner) is DictionaryToken dictionary))
                {
                    continue;
                }

                double[]? center = new double[3];
                if (!TryGetNumbers(GetArray(dictionary, NameToken.C, scanner), scanner, 0, 3, center))
                {
                    center = null;
                }

                double?[]? orientation = null;
                var orientationArray = GetArray(dictionary, NameToken.O, scanner);
                if (orientationArray != null && orientationArray.Length == 3)
                {
                    orientation = new double?[3];
                    for (var k = 0; k < 3; k++)
                    {
                        orientation[k] = Resolve(orientationArray.Data[k], scanner) is NumericToken number ? number.Data : null;
                    }
                }

                sections.Add(new Artwork3DCrossSection(
                    dictionary,
                    center,
                    orientation,
                    GetNumber(dictionary, Artwork3DNames.PO, scanner),
                    GetColor(Get(dictionary, Artwork3DNames.PC, scanner), scanner, leadingColorSpace: true),
                    GetBoolean(dictionary, Artwork3DNames.IV, scanner),
                    GetColor(Get(dictionary, Artwork3DNames.IC, scanner), scanner, leadingColorSpace: true),
                    GetBoolean(dictionary, Artwork3DNames.ST, scanner),
                    GetBoolean(dictionary, Artwork3DNames.SC, scanner)));
            }

            return sections;
        }

        private static IReadOnlyList<Artwork3DNode> ParseNodes(ArrayToken? array, IPdfTokenScanner scanner)
        {
            if (array is null || array.Length == 0)
            {
                return [];
            }

            var count = Math.Min(array.Length, MaxNodes);
            var nodes = new Artwork3DNode[count];
            var found = 0;
            for (var i = 0; i < count; i++)
            {
                if (!(Resolve(array.Data[i], scanner) is DictionaryToken dictionary))
                {
                    continue;
                }

                var name = GetText(dictionary, NameToken.N, scanner);
                if (name is null)
                {
                    continue;
                }

                dictionary.TryGet(Artwork3DNames.Instance, out IToken? instance);

                nodes[found++] = new Artwork3DNode(
                    name,
                    GetNumber(dictionary, NameToken.O, scanner),
                    GetBoolean(dictionary, NameToken.V, scanner),
                    GetMatrix(dictionary, NameToken.M, scanner),
                    ParseRenderMode(GetDictionary(dictionary, Artwork3DNames.RM, scanner), scanner),
                    instance,
                    GetText(dictionary, Artwork3DNames.Data, scanner));
            }

            if (found != count)
            {
                Array.Resize(ref nodes, found);
            }

            return nodes;
        }

        private static IReadOnlyList<DictionaryToken> ParseDictionaries(ArrayToken? array, IPdfTokenScanner scanner)
        {
            if (array is null || array.Length == 0)
            {
                return [];
            }

            var count = Math.Min(array.Length, MaxNodes);
            var dictionaries = new List<DictionaryToken>(count);
            for (var i = 0; i < count; i++)
            {
                if (Resolve(array.Data[i], scanner) is DictionaryToken dictionary)
                {
                    dictionaries.Add(dictionary);
                }
            }

            return dictionaries;
        }
    }
}
