namespace UglyToad.PdfPig.CrossReference
{
    using System;
    using System.Collections.Generic;
    using Exceptions;
    using Tokens;
    using Util;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Contains information for interpreting the cross-reference table.
    /// </summary>
    public class TrailerDictionary
    {
        /// <summary>
        /// The total number of object entries across both the original cross-reference table
        /// and in any incremental updates. 
        /// </summary>
        /// <remarks>
        /// Any object in a cross-reference section whose number is greater than this value is
        /// ignored and considered missing.
        /// </remarks>
        public int Size { get; }

        /// <summary>
        /// The offset in bytes to the previous cross-reference table or stream
        /// if the document has more than one cross-reference section.
        /// </summary>
        public long? PreviousCrossReferenceOffset { get; }

        /// <summary>
        /// The object reference for the document's catalog dictionary.
        /// </summary>
        public IndirectReference Root { get; }

        /// <summary>
        /// The object reference for the document's information dictionary if it contains one.
        /// </summary>
        public IndirectReference? Info { get; }

        /// <summary>
        /// A list containing two-byte strings which act as file identifiers.
        /// </summary>
        public IReadOnlyList<string> Identifier { get; }

        /// <summary>
        /// The document's encryption dictionary.
        /// </summary>
        [CanBeNull]
        public IToken EncryptionToken { get; }

        /// <summary>
        /// Create a new <see cref="TrailerDictionary"/>.
        /// </summary>
        /// <param name="dictionary">The parsed dictionary from the document.</param>
        internal TrailerDictionary(DictionaryToken dictionary)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            Size = dictionary.GetInt(NameToken.Size);
            PreviousCrossReferenceOffset = dictionary.GetLongOrDefault(NameToken.Prev);

            if (!dictionary.TryGet(NameToken.Root, out IndirectReferenceToken rootReference))
            {
                throw new PdfDocumentFormatException($"No root token was found in the trailer dictionary: {dictionary}.");
            }

            Root = rootReference.Data;

            if (dictionary.TryGet(NameToken.Info, out IndirectReferenceToken reference))
            {
                Info = reference.Data;
            }

            if (dictionary.TryGet(NameToken.Id, out ArrayToken arr))
            {
                var ids = new List<string>(arr.Data.Count);

                foreach (var token in arr.Data)
                {
                    if (token is StringToken str)
                    {
                        ids.Add(str.Data);
                    }
                }

                Identifier = ids;
            }
            else
            {
                Identifier = Array.Empty<string>();
            }

            if (dictionary.TryGet(NameToken.Encrypt, out var encryptionToken))
            {
                EncryptionToken = encryptionToken;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Size: {Size}, Root: {Root}";
        }
    }
}
