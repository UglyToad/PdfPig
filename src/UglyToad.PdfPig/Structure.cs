namespace UglyToad.PdfPig
{
    using System;
    using Content;
    using Core;
    using CrossReference;
    using System.Linq;
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
        /// The xref table of the document. Contains objects from all parsed xref tables.
        /// </summary>
        public CrossReferenceTable CrossReferenceTable { get; }
        
        /// <summary>
        /// The trailer dictionary of the document. Contains most bottom trailer
        /// </summary>
        public TrailerDictionary Trailer { get; }
        
        /// <summary>
        /// The offset of the xref table/object stream
        /// </summary>
        public long XrefOffset { get; }

        /// <summary>
        /// Provides access to tokenization capabilities for objects by object number.
        /// This is the document-level token scanner used to dereference indirect object references.
        /// </summary>
        public IPdfTokenScanner TokenScanner { get; }

        internal Structure(
            Catalog catalog,
            IPdfTokenScanner scanner,
            TrailerDictionary trailer,
            CrossReferenceTable xrefTable)
        {
            Trailer = trailer ?? throw new ArgumentNullException(nameof(trailer));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            TokenScanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
            CrossReferenceTable = xrefTable ?? throw new ArgumentNullException(nameof(xrefTable));
            XrefOffset = CrossReferenceTable.Parts.Count > 0 ? CrossReferenceTable.Parts.Last().Offset : 0;
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
