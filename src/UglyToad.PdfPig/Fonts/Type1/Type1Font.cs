namespace UglyToad.PdfPig.Fonts.Type1
{
    using System;
    using System.Collections.Generic;
    using Geometry;
    using Tokenization.Tokens;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// The information from the Type 1 font file.
    /// </summary>
    internal class Type1Font
    {
        public string Name { get; }
        
        public IReadOnlyDictionary<int, string> Encoding { get; }

        [CanBeNull]
        public ArrayToken FontMatrix { get; }

        [CanBeNull]
        public PdfRectangle BoundingBox { get; }

        public Type1PrivateDictionary PrivateDictionary { get; }

        public Type1Font(string name, IReadOnlyDictionary<int, string> encoding, ArrayToken fontMatrix, PdfRectangle boundingBox,
            Type1PrivateDictionary privateDictionary)
        {
            Name = name;
            Encoding = encoding;
            FontMatrix = fontMatrix;
            BoundingBox = boundingBox;
            PrivateDictionary = privateDictionary ?? throw new ArgumentNullException(nameof(privateDictionary));
        }
    }
}