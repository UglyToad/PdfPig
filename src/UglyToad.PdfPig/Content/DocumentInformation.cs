namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Tokens;
    using Util;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Metadata for the PDF document.
    /// </summary>
    public class DocumentInformation
    {
        internal static DocumentInformation Default { get; }
            = new DocumentInformation(null, null, null, null, null, null, null, null, null);

        private readonly string representation;

        /// <summary>
        /// The underlying document information PDF dictionary from the document.
        /// </summary>
        public DictionaryToken DocumentInformationDictionary { get; }

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
        /// The name of the application which created the original document before it was converted to PDF if applicable.
        /// </summary>
        [CanBeNull]
        public string Creator { get; }

        /// <summary>
        /// The name of the application used to convert the original document to PDF if applicable.
        /// </summary>
        [CanBeNull]
        public string Producer { get; }

        /// <summary>
        /// The date and time the document was created.
        /// </summary>
        [CanBeNull]
        public string CreationDate { get; }

        /// <summary>
        /// The date and time the document was most recently modified.
        /// </summary>
        [CanBeNull]
        public string ModifiedDate { get; }

        internal DocumentInformation(DictionaryToken documentInformationDictionary, string title, string author, string subject, string keywords, string creator, string producer,
            string creationDate,
            string modifiedDate)
        {
            DocumentInformationDictionary = documentInformationDictionary ?? new DictionaryToken(new Dictionary<NameToken, IToken>());
            Title = title;
            Author = author;
            Subject = subject;
            Keywords = keywords;
            Creator = creator;
            Producer = producer;
            CreationDate = creationDate;
            ModifiedDate = modifiedDate;

            var builder = new StringBuilder();

            AppendPart("Title", title, builder);
            AppendPart("Author", author, builder);
            AppendPart("Subject", subject, builder);
            AppendPart("Keywords", keywords, builder);
            AppendPart("Creator", creator, builder);
            AppendPart("Producer", producer, builder);
            AppendPart("CreationDate", creationDate, builder);
            AppendPart("ModifiedDate", modifiedDate, builder);

            representation = builder.ToString();
        }

        /// <summary>
        /// Gets the <see cref="CreationDate"/> as a <see cref="DateTimeOffset"/> if it's possible to convert it, or <see langword="null"/>.
        /// </summary>
        public DateTimeOffset? GetCreatedDateTimeOffset()
        {
            return DateFormatHelper.TryParseDateTimeOffset(CreationDate, out var result) ? result : default(DateTimeOffset?);
        }

        /// <summary>
        /// Gets the <see cref="ModifiedDate"/> as a <see cref="DateTimeOffset"/> if it's possible to convert it, or <see langword="null"/>.
        /// </summary>
        public DateTimeOffset? GetModifiedDateTimeOffset()
        {
            return DateFormatHelper.TryParseDateTimeOffset(ModifiedDate, out var result) ? result : default(DateTimeOffset?);
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
