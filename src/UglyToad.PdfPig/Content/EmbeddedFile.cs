namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using Tokens;

    /// <summary>
    /// A file embedded in a PDF document for document references.
    /// </summary>
    public class EmbeddedFile
    {
        /// <summary>
        /// The name given to this embedded file in the document's name tree.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The specification of the path to the file.
        /// </summary>
        public string FileSpecification { get; }

        /// <summary>
        /// The decrypted memory of the file.
        /// </summary>
        public ReadOnlyMemory<byte> Memory { get; }

        /// <summary>
        /// The decrypted bytes of the file.
        /// </summary>
        public ReadOnlySpan<byte> Bytes => Memory.Span;

        /// <summary>
        /// The underlying embedded file stream.
        /// </summary>
        public StreamToken Stream { get; }
        
        internal EmbeddedFile(string name, string fileSpecification, ReadOnlyMemory<byte> bytes, StreamToken stream)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            FileSpecification = fileSpecification;
            Memory = bytes;
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Name}: {Stream.StreamDictionary}.";
        }
    }
}
