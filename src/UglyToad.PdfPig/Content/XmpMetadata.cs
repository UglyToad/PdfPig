namespace UglyToad.PdfPig.Content
{
    using Core;
    using Filters;
    using System.Xml.Linq;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// Wraps an XML based Extensible Metadata Platform (XMP) document. These XML documents are embedded in PDFs to provide metadata
    /// about objects (the entire document, images, etc). They can be present as plain text or encoded/encrypted streams.
    /// </summary>
    public class XmpMetadata
    {
        private readonly ILookupFilterProvider filterProvider;
        private readonly IPdfTokenScanner pdfTokenScanner;

        /// <summary>
        /// The underlying <see cref="StreamToken"/> for this metadata.
        /// </summary>
        public StreamToken MetadataStreamToken { get; }

        internal XmpMetadata(StreamToken stream, ILookupFilterProvider filterProvider, IPdfTokenScanner pdfTokenScanner)
        {
            this.filterProvider = filterProvider ?? throw new ArgumentNullException(nameof(filterProvider));
            this.pdfTokenScanner = pdfTokenScanner;
            MetadataStreamToken = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        /// <summary>
        /// Get the decoded bytes for the metadata stream. This can be interpreted as a sequence of plain-text bytes.
        /// </summary>
        /// <returns>The bytes for the metadata object with any filters removed.</returns>
        public ReadOnlySpan<byte> GetXmlBytes()
        {
            return MetadataStreamToken.Decode(filterProvider, pdfTokenScanner).Span;
        }

        /// <summary>
        /// Gets the metadata stream as an <see cref="XDocument"/>.
        /// </summary>
        /// <returns>The <see cref="XDocument"/> for the XMP XML.</returns>
        public XDocument GetXDocument()
        {
            return XDocument.Parse(OtherEncodings.BytesAsLatin1String(GetXmlBytes()));
        }
    }
}
