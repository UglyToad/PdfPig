namespace UglyToad.PdfPig
{
    using System;
    using Content;
    using Core;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// Provides access to explore and retrieve the underlying PDF objects from the document.
    /// </summary>
    public class Structure
    {
        /// <summary>
        /// The root of the document's hierarchy providing access to the page tree as well as other information.
        /// </summary>
        public Catalog Catalog { get; }

        /// <summary>
        /// Provides access to tokenization capabilities for objects by object number.
        /// </summary>
        internal IPdfTokenScanner TokenScanner { get; }

        internal Structure(
            Catalog catalog,
            IPdfTokenScanner scanner)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            TokenScanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        }

        /// <summary>
        /// Retrieve the tokenized object with the specified object reference number.
        /// </summary>
        /// <param name="reference">The object reference number.</param>
        /// <returns>The tokenized PDF object from the file.</returns>
        public ObjectToken GetObject(IndirectReference reference)
        {
            return TokenScanner.Get(reference) ?? throw new InvalidOperationException($"Could not find the object with reference: {reference}.");
        }
    }
}
