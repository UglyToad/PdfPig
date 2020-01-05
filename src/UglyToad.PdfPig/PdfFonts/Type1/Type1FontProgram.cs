namespace UglyToad.PdfPig.PdfFonts.Type1
{
    using System;
    using System.Collections.Generic;
    using CharStrings;
    using Core;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// The information from the Type 1 font file.
    /// </summary>
    internal class Type1FontProgram
    {
        /// <summary>
        /// The name of the font.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// The encoding dictionary defining a name for each character code.
        /// </summary>
        public IReadOnlyDictionary<int, string> Encoding { get; }

        [CanBeNull]
        public ArrayToken FontMatrix { get; }

        /// <summary>
        /// A rectangle in glyph coordinates specifying the font bounding box.
        /// This is the smallest rectangle enclosing the shape that would result if all of the glyphs were overlayed on each other. 
        /// </summary>
        public PdfRectangle BoundingBox { get; }
        
        [NotNull]
        public Type1PrivateDictionary PrivateDictionary { get; }

        [NotNull]
        public Type1CharStrings CharStrings { get; }

        /// <summary>
        /// Create a new <see cref="Type1FontProgram"/> from the information retrieved from the PDF document.
        /// </summary>
        /// <param name="name">The name of the font.</param>
        /// <param name="encoding"></param>
        /// <param name="fontMatrix"></param>
        /// <param name="boundingBox"></param>
        /// <param name="privateDictionary"></param>
        /// <param name="charStrings"></param>
        public Type1FontProgram(string name, IReadOnlyDictionary<int, string> encoding, ArrayToken fontMatrix, PdfRectangle boundingBox,
            Type1PrivateDictionary privateDictionary,
            Type1CharStrings charStrings)
        {
            Name = name;
            Encoding = encoding;
            FontMatrix = fontMatrix;
            BoundingBox = boundingBox;
            PrivateDictionary = privateDictionary ?? throw new ArgumentNullException(nameof(privateDictionary));
            CharStrings = charStrings ?? throw new ArgumentNullException(nameof(charStrings));
        }

        public PdfRectangle? GetCharacterBoundingBox(string characterName)
        {
            if (!CharStrings.TryGenerate(characterName, out var glyph))
            {
                return null;
            }

            var bbox = glyph.GetBoundingRectangle();

            return bbox;
        }

        public bool ContainsNamedCharacter(string name)
        {
            return CharStrings.CharStrings.ContainsKey(name);
        }

        public TransformationMatrix GetFontTransformationMatrix()
        {
            if (FontMatrix == null || FontMatrix.Data.Count != 6)
            {
                return TransformationMatrix.FromValues(0.001, 0, 0, 0.001, 0, 0);
            }

            var a = ((NumericToken) FontMatrix.Data[0]).Double;
            var b = ((NumericToken) FontMatrix.Data[1]).Double;
            var c = ((NumericToken) FontMatrix.Data[2]).Double;
            var d = ((NumericToken) FontMatrix.Data[3]).Double;
            var e = ((NumericToken) FontMatrix.Data[4]).Double;
            var f = ((NumericToken) FontMatrix.Data[5]).Double;

            return TransformationMatrix.FromValues(a, b, c, d, e, f);
        }
    }
}