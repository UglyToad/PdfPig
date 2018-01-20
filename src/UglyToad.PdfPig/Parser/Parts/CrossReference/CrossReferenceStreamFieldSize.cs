namespace UglyToad.PdfPig.Parser.Parts.CrossReference
{
    using System;
    using Exceptions;
    using Tokenization.Tokens;
    using Util;

    /// <summary>
    /// The array representing the size of the fields in a cross reference stream.
    /// </summary>
    internal class CrossReferenceStreamFieldSize
    {
        /// <summary>
        /// The type of the entry.
        /// </summary>
        public int Field1Size { get; }

        /// <summary>
        /// Type 0 and 2 is the object number, Type 1 this is the byte offset from beginning of file.
        /// </summary>
        public int Field2Size { get; }

        /// <summary>
        /// For types 0 and 1 this is the generation number. For type 2 it is the stream index.
        /// </summary>
        public int Field3Size { get; }

        /// <summary>
        /// How many bytes are in a line.
        /// </summary>
        public int LineLength { get; }

        public CrossReferenceStreamFieldSize(DictionaryToken dictionary)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (!dictionary.TryGet(NameToken.W, out var token) || !(token is ArrayToken wArray))
            {
                throw new PdfDocumentFormatException($"The W entry for the stream dictionary was not an array: {token}.");
            }

            if (wArray.Data.Count < 3)
            {
                throw new PdfDocumentFormatException($"There must be at least 3 entries in a W entry for a stream dictionary: {wArray}.");
            }

            Field1Size = wArray.GetNumeric(0).Int;
            Field2Size = wArray.GetNumeric(1).Int;
            Field3Size = wArray.GetNumeric(2).Int;

            LineLength = Field1Size + Field2Size + Field3Size;
        }
    }
}