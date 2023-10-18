namespace UglyToad.PdfPig.CrossReference
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// The cross-reference table contains information that enables random access to PDF objects within the file by object number
    /// so that specific objects can be located directly without having to scan the whole document. 
    /// A PDF document may contain multiple cross reference tables, this class provides access to the merged result with the latest
    /// offset for each object. The offsets of the original cross reference tables or streams merged into this result are available
    /// in the <see cref="CrossReferenceOffsets"/> list.
    /// </summary>
    public class CrossReferenceTable
    {
        private readonly Dictionary<IndirectReference, long> objectOffsets;

        /// <summary>
        /// The corresponding byte offset for each keyed object in this document.
        /// </summary>
        [NotNull]
        public IReadOnlyDictionary<IndirectReference, long> ObjectOffsets => objectOffsets;

        /// <summary>
        /// The type of the first cross-reference table located in this document.
        /// </summary>
        public CrossReferenceType Type { get; }

        /// <summary>
        /// The trailer dictionary.
        /// </summary>
        [NotNull]
        public TrailerDictionary Trailer { get; }

        /// <summary>
        /// The byte offsets of each cross-reference table or stream in this document and the previous
        /// table or stream they link to if applicable.
        /// </summary>
        [NotNull]
        public IReadOnlyList<CrossReferenceOffset> CrossReferenceOffsets { get; }

        internal CrossReferenceTable(CrossReferenceType type, IReadOnlyDictionary<IndirectReference, long> objectOffsets, 
            TrailerDictionary trailer,
            IReadOnlyList<CrossReferenceOffset> crossReferenceOffsets)
        {
            if (objectOffsets == null)
            {
                throw new ArgumentNullException(nameof(objectOffsets));
            }

            Type = type;
            Trailer = trailer ?? throw new ArgumentNullException(nameof(trailer));
            CrossReferenceOffsets = crossReferenceOffsets ?? throw new ArgumentNullException(nameof(crossReferenceOffsets));

            var result = new Dictionary<IndirectReference, long>();
            foreach (var objectOffset in objectOffsets)
            {
                result[objectOffset.Key] = objectOffset.Value;
            }

            this.objectOffsets = result;
        }

        /// <summary>
        /// The offset of a cross-reference table or stream in the document.
        /// </summary>
        public readonly struct CrossReferenceOffset
        {
            /// <summary>
            /// The offset in bytes from the start of the document where the stream or table starts.
            /// </summary>
            public long Current { get; }

            /// <summary>
            /// The offset in bytes from the start of the document where the previous stream or table in a document
            /// containing incremental updates can be found, if applicable.
            /// </summary>
            public long? Previous { get; }

            /// <summary>
            /// Create a new <see cref="CrossReferenceOffset"/>.
            /// </summary>
            /// <param name="current">The offset in bytes from the start of the document where the stream or table starts.</param>
            /// <param name="previous">The offset in bytes from the start of the document where the previous stream or table in a document starts.</param>
            public CrossReferenceOffset(long current, long? previous)
            {
                Current = current;
                Previous = previous;
            }

            /// <inheritdoc />
            public override string ToString()
            {
                var prev = Previous.HasValue ? $" {Previous}" : string.Empty;
                return $"{Current}{prev}";
            }
        }
    }
}
