namespace UglyToad.PdfPig.Content
{
    using System.Text;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Metadata for the PDF document.
    /// </summary>
    public class DocumentInformation
    {
        internal static DocumentInformation Default { get; }
            = new DocumentInformation(null, null, null, null, null, null);

        private readonly string representation;

        /// <summary>
        /// The title of this document if applicable.
        /// </summary>
        [CanBeNull]
        public string Title { get; }

        /// <summary>
        /// The name of the person who created this document if applicable.
        /// </summary>
        [CanBeNull]
        public string Author { get; }

        /// <summary>
        /// The subject of this document if applicable.
        /// </summary>
        [CanBeNull]
        public string Subject { get; }

        /// <summary>
        /// Any keywords associated with this document if applicable.
        /// </summary>
        [CanBeNull]
        public string Keywords { get; }

        /// <summary>
        /// The name of the application which created the original document before it was converted to PDF. if applicable.
        /// </summary>
        [CanBeNull]
        public string Creator { get; }

        /// <summary>
        /// The name of the application used to convert the original document to PDF if applicable.
        /// </summary>
        [CanBeNull]
        public string Producer { get; }

        internal DocumentInformation(string title, string author, string subject, string keywords, string creator, string producer)
        {
            Title = title;
            Author = author;
            Subject = subject;
            Keywords = keywords;
            Creator = creator;
            Producer = producer;

            var builder = new StringBuilder();

            AppendPart("Title", title, builder);
            AppendPart("Author", author, builder);
            AppendPart("Subject", subject, builder);
            AppendPart("Keywords", keywords, builder);
            AppendPart("Creator", creator, builder);
            AppendPart("Producer", producer, builder);

            representation = builder.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Gets a string representing this document information. <see langword="null"/> entries are not shown.
        /// </summary>
        public override string ToString()
        {
            return representation;
        }

        private static void AppendPart(string name, string value, StringBuilder builder)
        {
            if (value == null)
            {
                return;
            }

            builder.Append(name).Append(": ").Append(value).Append("; ");
        }
    }
}
