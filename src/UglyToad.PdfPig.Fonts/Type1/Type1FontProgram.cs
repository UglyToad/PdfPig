namespace UglyToad.PdfPig.Fonts.Type1
{
    using CharStrings;
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tokens;

    /// <summary>
    /// An Adobe Type 1 font.
    /// </summary>
    public class Type1Font
    {
        /// <summary>
        /// The name of the font.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// The encoding dictionary defining a name for each character code.
        /// </summary>
        public IReadOnlyDictionary<int, string> Encoding { get; }

        /// <summary>
        /// The font matrix.
        /// </summary>
        public TransformationMatrix FontMatrix { get; }

        /// <summary>
        /// A rectangle in glyph coordinates specifying the font bounding box.
        /// This is the smallest rectangle enclosing the shape that would result if all of the glyphs were overlayed on each other. 
        /// </summary>
        public PdfRectangle BoundingBox { get; }
        
        /// <summary>
        /// The private dictionary for this font.
        /// </summary>
        public Type1PrivateDictionary PrivateDictionary { get; }

        /// <summary>
        /// The charstrings in this font.
        /// </summary>
        internal Type1CharStrings CharStrings { get; }

        /// <summary>
        /// Create a new <see cref="Type1Font"/>.
        /// </summary>
        internal Type1Font(string name, IReadOnlyDictionary<int, string> encoding, ArrayToken fontMatrix, PdfRectangle boundingBox,
            Type1PrivateDictionary privateDictionary,
            Type1CharStrings charStrings)
        {
            Name = name;
            Encoding = encoding;
            FontMatrix = GetFontTransformationMatrix(fontMatrix);
            BoundingBox = boundingBox;
            PrivateDictionary = privateDictionary ?? throw new ArgumentNullException(nameof(privateDictionary));
            CharStrings = charStrings ?? throw new ArgumentNullException(nameof(charStrings));
        }

        /// <summary>
        /// Get the bounding box for the character with the given name.
        /// </summary>
        public PdfRectangle? GetCharacterBoundingBox(string characterName)
        {
            if (!CharStrings.TryGenerate(characterName, out var glyph))
            {
                return null;
            }

            var bbox = GetBoundingRectangle(glyph);

            return bbox;
        }

        /// <summary>
        /// Gets a <see cref="PdfRectangle"/> which entirely contains the geometry of the defined path.
        /// </summary>
        /// <returns>For paths which don't define any geometry this returns <see langword="null"/>.</returns>
        private PdfRectangle? GetBoundingRectangle(List<PdfSubpath> path)
        {
            if (path.Count == 0)
            {
                return null;
            }

            var bboxes = path.Select(x => x.GetBoundingRectangle()).Where(x => x.HasValue).Select(x => x.Value).ToList();
            if (bboxes.Count == 0)
            {
                return null;
            }

            var minX = bboxes.Min(x => x.Left);
            var minY = bboxes.Min(x => x.Bottom);
            var maxX = bboxes.Max(x => x.Right);
            var maxY = bboxes.Max(x => x.Top);
            return new PdfRectangle(minX, minY, maxX, maxY);
        }

        /// <summary>
        /// Get the pdfpath for the character with the given name.
        /// </summary>
        public IReadOnlyList<PdfSubpath> GetCharacterPath(string characterName)
        {
            if (!CharStrings.TryGenerate(characterName, out var glyph))
            {
                return null;
            }
            return glyph;
        }

        /// <summary>
        /// Whether the font contains a character with the given name.
        /// </summary>
        public bool ContainsNamedCharacter(string name)
        {
            return CharStrings.CharStrings.ContainsKey(name);
        }

        private static TransformationMatrix GetFontTransformationMatrix(ArrayToken array)
        {
            if (array == null || array.Data.Count != 6)
            {
                return TransformationMatrix.FromValues(0.001, 0, 0, 0.001, 0, 0);
            }

            var a = ((NumericToken)array.Data[0]).Double;
            var b = ((NumericToken)array.Data[1]).Double;
            var c = ((NumericToken)array.Data[2]).Double;
            var d = ((NumericToken)array.Data[3]).Double;
            var e = ((NumericToken)array.Data[4]).Double;
            var f = ((NumericToken)array.Data[5]).Double;

            return TransformationMatrix.FromValues(a, b, c, d, e, f);
        }
    }
}