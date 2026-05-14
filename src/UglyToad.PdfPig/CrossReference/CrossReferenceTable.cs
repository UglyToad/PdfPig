namespace UglyToad.PdfPig.CrossReference
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Parser.FileStructure;
    using System.Linq;

    /// <summary>
    /// The cross-reference table contains information that enables random access to PDF objects within the file by object number
    /// so that specific objects can be located directly without having to scan the whole document. 
    /// A PDF document may contain multiple cross reference tables, this class provides access to the merged result with the latest
    /// offset for each object. The offsets of the original cross reference tables or streams merged into this result are available
    /// in the <see cref="CrossReferenceOffsets"/> list.
    /// </summary>
    public class CrossReferenceTable
    {
        private readonly CrossReferenceTablePart[] parts;
        private readonly Dictionary<IndirectReference, XrefLocation> objectOffsets;

        /// <summary>
        /// The corresponding byte offset for each keyed object in this document.
        /// </summary>
        public IReadOnlyDictionary<IndirectReference, XrefLocation> ObjectOffsets => objectOffsets;
        
        /// <summary>
        /// List of all xref tables parts, added during all incremental updates
        /// </summary>
        public IReadOnlyList<CrossReferenceTablePart> Parts => parts;

        /// <summary>
        /// The type of the first cross-reference table located in this document.
        /// </summary>
        [Obsolete("Useless property. Use each CrossReferenceTablePart.Type instead.")]
        public CrossReferenceType Type { get; }

        /// <summary>
        /// The trailer dictionary.
        /// </summary>
        [Obsolete("Use trailer dictionary from PdfDocument.Structure property instead")]
        public TrailerDictionary Trailer { get; }

        /// <summary>
        /// The byte offsets of each cross-reference table or stream in this document and the previous
        /// table or stream they link to if applicable.
        /// </summary>
        [Obsolete("Useless property. Use each CrossReferenceTablePart.Offset/Prev instead")]
        public IReadOnlyList<CrossReferenceOffset> CrossReferenceOffsets { get; }

        internal CrossReferenceTable(
            IReadOnlyList<IXrefSection> sections,
            IReadOnlyDictionary<IndirectReference, XrefLocation> objectOffsets, 
            TrailerDictionary trailer)
        {
            if (objectOffsets is null)
            {
                throw new ArgumentNullException(nameof(objectOffsets));
            }

            if (sections is null)
            {
                throw new ArgumentNullException(nameof(sections));
            }
            
            parts = sections
                .Select(CrossReferenceTablePart.FromXrefSection)
                .ToArray();

            CrossReferenceOffsets = parts
                .Select(p => new CrossReferenceOffset(p.Offset, p.Previous))
                .ToArray();
            
            Type = parts.FirstOrDefault()?.Type ?? CrossReferenceType.Table;
            Trailer = trailer ?? throw new ArgumentNullException(nameof(trailer));
            this.objectOffsets = objectOffsets.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
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
