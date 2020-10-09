namespace UglyToad.PdfPig.PdfFonts.Simple
{
    using Cmap;
    using Composite;
    using Core;
    using Fonts;
    using Fonts.CompactFontFormat;
    using Fonts.Encodings;
    using Fonts.Type1;
    using System.Collections.Generic;
    using Tokens;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Parser.Parts;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using Util.JetBrains.Annotations;
    using static UglyToad.PdfPig.Core.PdfSubpath;

    /// <summary>
    /// A font based on the Adobe Type 1 font format.
    /// </summary>
    internal class Type1FontSimple : IFont
    {
        private static readonly TransformationMatrix DefaultTransformationMatrix = TransformationMatrix.FromValues(0.001, 0, 0, 0.001, 0, 0);

        private readonly Dictionary<int, CharacterBoundingBox> cachedBoundingBoxes = new Dictionary<int, CharacterBoundingBox>();

        private readonly int firstChar;

        private readonly int lastChar;

        private readonly double[] widths;

        private readonly FontDescriptor fontDescriptor;

        private readonly Encoding encoding;

        [CanBeNull]
        private readonly Union<Type1Font, CompactFontFormatFontCollection> fontProgram;

        private readonly ToUnicodeCMap toUnicodeCMap;

        private readonly TransformationMatrix fontMatrix;

        public NameToken Name { get; }

        public bool IsVertical { get; } = false;

        public FontDetails Details { get; }

        public Type1FontSimple(NameToken name, int firstChar, int lastChar, double[] widths, FontDescriptor fontDescriptor, Encoding encoding, 
            CMap toUnicodeCMap,
            Union<Type1Font, CompactFontFormatFontCollection> fontProgram)
        {
            this.firstChar = firstChar;
            this.lastChar = lastChar;
            this.widths = widths;
            this.fontDescriptor = fontDescriptor;
            this.encoding = encoding;
            this.fontProgram = fontProgram;
            this.toUnicodeCMap = new ToUnicodeCMap(toUnicodeCMap);

            var matrix = DefaultTransformationMatrix;

            if (fontProgram != null)
            {
                if (fontProgram.TryGetFirst(out var t1Font))
                {
                    matrix = t1Font.FontMatrix;
                }
                else if (fontProgram.TryGetSecond(out var cffFont))
                {
                    matrix = cffFont.GetFirstTransformationMatrix();
                }
            }

            fontMatrix = matrix;

            Name = name;
            Details = fontDescriptor?.ToDetails(name?.Data) 
                      ?? FontDetails.GetDefault(name?.Data);
        }

        public int ReadCharacterCode(IInputBytes bytes, out int codeLength)
        {
            codeLength = 1;
            return bytes.CurrentByte;
        }

        public bool TryGetUnicode(int characterCode, out string value)
        {
            if (toUnicodeCMap.CanMapToUnicode)
            {
                return toUnicodeCMap.TryGet(characterCode, out value);
            }

            value = null;

            if (encoding == null)
            {
                try
                {
                    value = char.ConvertFromUtf32(characterCode);
                    return true;
                }
                catch
                {
                    if (fontProgram == null)
                    {
                        return false;
                    }

                    var containsEncoding = false;
                    if (fontProgram.TryGetFirst(out var t1Font))
                    {
                        containsEncoding = t1Font.Encoding.TryGetValue(characterCode, out value);
                    }

                    return containsEncoding;
                }
            }

            var name = encoding.GetName(characterCode);
            
            try
            {
                value = GlyphList.AdobeGlyphList.NameToUnicode(name);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public CharacterBoundingBox GetBoundingBox(int characterCode)
        {
            if (cachedBoundingBoxes.TryGetValue(characterCode, out var box))
            {
                return box;
            }
            
            var boundingBox = GetBoundingBoxInGlyphSpace(characterCode);

            var matrix = fontMatrix;

            boundingBox = matrix.Transform(boundingBox);

            var width = GetWidth(characterCode, boundingBox);

            var result = new CharacterBoundingBox(boundingBox, width/1000.0);

            cachedBoundingBoxes[characterCode] = result;

            return result;
        }

        private double GetWidth(int characterCode, PdfRectangle boundingBox)
        {
            var widthIndex = characterCode - firstChar;

            if (widthIndex >= 0 && widthIndex < widths.Length)
            {
                return widths[widthIndex];
            }

            if (fontDescriptor?.MissingWidth != null)
            {
                return (double)fontDescriptor.MissingWidth;
            }

            return boundingBox.Width;
        }
        
        private PdfRectangle GetBoundingBoxInGlyphSpace(int characterCode)
        {
            if (characterCode < firstChar || characterCode > lastChar)
            {
                return new PdfRectangle(0, 0, 250, 0);
            }

            if (fontProgram == null)
            {
                return new PdfRectangle(0, 0, widths[characterCode - firstChar], 0);
            }

            PdfRectangle? rect = null;
            if (fontProgram.TryGetFirst(out var t1Font))
            {
                var name = encoding.GetName(characterCode);
                rect = t1Font.GetCharacterBoundingBox(name);
            }
            else if (fontProgram.TryGetSecond(out var cffFont))
            {
                var first = cffFont.FirstFont;
                string characterName;
                if (encoding != null)
                {
                    characterName = encoding.GetName(characterCode);
                }
                else
                {
                    characterName = cffFont.GetCharacterName(characterCode);
                }

                rect = first.GetCharacterBoundingBox(characterName);
            }


            if (!rect.HasValue)
            {
                return new PdfRectangle(0, 0, widths[characterCode - firstChar], 0);
            }

            // ReSharper disable once PossibleInvalidOperationException
            return rect.Value;
        }

        public TransformationMatrix GetFontMatrix()
        {
            return fontMatrix;
        }

        public bool TryGetPath(int characterCode, out IReadOnlyList<PdfSubpath> path)
        {
            path = null;
            IReadOnlyList<PdfSubpath> tempPath = null;
            if (characterCode < firstChar || characterCode > lastChar)
            {
                return false;
            }

            if (fontProgram == null)
            {
                return false;
            }

            if (fontProgram.TryGetFirst(out var t1Font))
            {
                var name = encoding.GetName(characterCode);
                tempPath = t1Font.GetCharacterPath(name);
            }
            else if (fontProgram.TryGetSecond(out var cffFont))
            {
                var first = cffFont.FirstFont;
                string characterName;
                if (encoding != null)
                {
                    characterName = encoding.GetName(characterCode);
                }
                else
                {
                    characterName = cffFont.GetCharacterName(characterCode);
                }

                tempPath = first.GetCharacterPath(characterName);
            }

            if (tempPath != null && tempPath.Count > 0)
            {
                var pathToReturn = new List<PdfSubpath>(tempPath.Count);
                foreach (var sp in tempPath)
                {
                    var current = new PdfSubpath();
                    foreach (var c in sp.Commands)
                    {
                        if (c is Move move)
                        {
                            var l = fontMatrix.Transform(move.Location);
                            current.MoveTo(l.X, l.Y);
                        }
                        else if (c is Line line)
                        {
                            //var f = fontMatrix.Transform(line.From);
                            var t = fontMatrix.Transform(line.To);

                            current.LineTo(t.X, t.Y);
                        }
                        else if (c is BezierCurve curve)
                        {
                            var first = fontMatrix.Transform(curve.FirstControlPoint);
                            var second = fontMatrix.Transform(curve.SecondControlPoint);
                            var end = fontMatrix.Transform(curve.EndPoint);

                            current.BezierCurveTo(first.X, first.Y, second.X, second.Y, end.X, end.Y);
                        }
                        else if (c is Close)
                        {
                            current.CloseSubpath();
                        }
                    }
                    pathToReturn.Add(current);
                }

                path = pathToReturn;
                return true;
            }
            return false;
        }

        public bool TryGetDecodedFontBytes(IPdfTokenScanner pdfTokenScanner, IFilterProvider filterProvider, out IReadOnlyList<byte> bytes)
        {
            bytes = null;
            if (fontDescriptor?.FontFile?.ObjectKey != null)
            {
                var fontFileStream = DirectObjectFinder.Get<StreamToken>(fontDescriptor.FontFile.ObjectKey, pdfTokenScanner);
                bytes = fontFileStream.Decode(filterProvider);
                return true;
            }

            return false;
        }
    }
}
