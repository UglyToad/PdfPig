namespace UglyToad.PdfPig.Writer
{
    using System;

    /// <summary>
    /// Information about a file added with <see cref="PdfDocumentBuilder.AddEmbeddedFile(string, ReadOnlyMemory{byte}, EmbeddedFileOptions)"/>.
    /// </summary>
    public class EmbeddedFileOptions
    {
        /// <summary>
        /// The MIME type of the file, such as text/xml. Defaults to application/octet-stream.
        /// </summary>
        public string MimeType { get; set; } = "application/octet-stream";

        /// <summary>
        /// A description of the file, which readers show with the attachment.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The date and time when the file was last modified. If it is not set, the time the file is added is used.
        /// </summary>
        public DateTimeOffset? ModifiedDate { get; set; }

        /// <summary>
        /// The relationship of the file to the document, which is only written to PDF/A-3 documents since they require it.
        /// Defaults to <see cref="AssociatedFileRelationship.Unspecified"/>.
        /// </summary>
        public AssociatedFileRelationship Relationship { get; set; }
    }
}
