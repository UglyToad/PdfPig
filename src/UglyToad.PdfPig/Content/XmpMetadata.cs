namespace UglyToad.PdfPig.Content
{
    using Core;
    using Filters;
    using System;
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
        /// Get the decoded bytes for the metadata stream as <see cref="ReadOnlyMemory{T}"/>. This can be interpreted as a sequence of plain-text bytes.
        /// </summary>
        /// <returns>The bytes for the metadata object with any filters removed.</returns>
        public ReadOnlyMemory<byte> GetXmlMemory()
        {
            return MetadataStreamToken.Decode(filterProvider, pdfTokenScanner);
        }

        /// <summary>
        /// Get the decoded bytes for the metadata stream. This can be interpreted as a sequence of plain-text bytes.
        /// </summary>
        /// <returns>The bytes for the metadata object with any filters removed.</returns>
        public ReadOnlySpan<byte> GetXmlBytes()
        {
            return GetXmlMemory().Span;
        }

        /// <summary>
        /// Gets the metadata stream as an <see cref="XDocument"/>.
        /// <para>
        /// It is recommended to NOT use this method as it parses the XML document without security settings.
        /// This method also converts the bytes into string before processing.
        /// </para>
        /// Instead, create your own XmlReader with the desired without settings (you can create a <see cref="System.IO.MemoryStream"/> based on <see cref="GetXmlMemory"/>),
        /// and then load the XDocument.
        /// </summary>
        /// <returns>The <see cref="XDocument"/> for the XMP XML.</returns>
        public XDocument GetXDocument()
        {
            return XDocument.Parse(OtherEncodings.BytesAsLatin1String(GetXmlBytes()));
        }
    }
}
