namespace UglyToad.PdfPig.Fonts.Type1
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using CharStrings;
    using Geometry;
    using Tokenization.Tokens;
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

        public PdfRectangle GetCharacterBoundingBox(int characterCode)
        {
            var b = Encoding[characterCode];
            var glyph = CharStrings.Generate(b);
            var bbox = glyph.GetBoundingRectangle();

            if (Debugger.IsAttached)
            {
                if (bbox.Bottom < BoundingBox.Bottom
                    || bbox.Top > BoundingBox.Top
                    || bbox.Left < BoundingBox.Left
                    || bbox.Right > BoundingBox.Right)
                {
                    // Debugger.Break();
                }

                var full = glyph.ToFullSvg();
                Console.WriteLine(full);
            }

            return bbox;
        }
    }
}