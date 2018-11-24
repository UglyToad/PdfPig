namespace UglyToad.PdfPig
{
    using System;
    using Content;
    using CrossReference;
    using Tokenization.Scanner;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Provides access to explore and retrieve the underlying PDF objects from the document.
    /// </summary>
    public class Structure
    {
        /// <summary>
        /// The root of the document's hierarchy providing access to the page tree as well as other information.
        /// </summary>
        [NotNull]
        public Catalog Catalog { get; }
        
        /// <summary>
        /// The cross-reference table enables direct access to objects by number.
        /// </summary>
        [NotNull]
        public CrossReferenceTable CrossReferenceTable { get; }

        /// <summary>
        /// Provides access to tokenization capabilities for objects by object number.
        /// </summary>
        internal IPdfTokenScanner TokenScanner { get; }

        internal Structure(Catalog catalog, CrossReferenceTable crossReferenceTable,
            IPdfTokenScanner scanner)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            CrossReferenceTable = crossReferenceTable ?? throw new ArgumentNullException(nameof(crossReferenceTable));
            TokenScanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        }

        /// <summary>
        /// Retrieve the tokenized object with the specified object reference number.
        /// </summary>
        /// <param name="reference">The object reference number.</param>
        /// <returns>The tokenized PDF object from the file.</returns>
        public ObjectToken GetObject(IndirectReference reference)
        {
            return TokenScanner.Get(reference);
        }
    }
}
