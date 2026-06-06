namespace UglyToad.PdfPig.CrossReference
{
    using System.Collections.Generic;
    using Core;
    using Parser.FileStructure;
    using System.Linq;
    using Tokens;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// The format of an in-use entry is
    ///     nnnnnnnnnn ggggg n eol
    /// where
    ///     nnnnnnnnnn is a 10-digit byte offset
    ///     ggggg is a 5-digit generation number
    ///     n is a literal keyword identifying this as an in-use entry
    ///     eol is a 2-character end-of-line sequence
    ///
    ///
    /// The byte offset is a 10-digit number, padded with leading zeros if necessary,
    /// giving the number of bytes from the beginning of the file to the beginning of the
    /// object.
    /// </remarks>
    public class CrossReferenceTablePart
    {
        public IReadOnlyDictionary<IndirectReference, XrefLocation> ObjectOffsets { get; }

        public long Offset { get; }

        public long? Previous { get; }

        public DictionaryToken Dictionary { get; }

        public CrossReferenceType Type { get; }

        /// <summary>
        /// For Xref streams indicated by tables they should be used together when constructing the final table.
        /// </summary>
        public long? TiedToXrefAtOffset { get; }

        public CrossReferenceTablePart(
            IReadOnlyDictionary<IndirectReference, XrefLocation> objectOffsets,
            long offset, 
            long? previous,
            DictionaryToken dictionary,
            CrossReferenceType type,
            long? tiedToXrefAtOffset)
        {
            ObjectOffsets = objectOffsets;
            Offset = offset;
            Previous = previous;
            Dictionary = dictionary;
            Type = type;
            TiedToXrefAtOffset = tiedToXrefAtOffset;
        }

        internal static CrossReferenceTablePart FromXrefSection(IXrefSection xrefSection)
        {
            long? tiedToXrefAtOffset = null;

            if (xrefSection.Dictionary is not null)
            {
                tiedToXrefAtOffset = xrefSection.Dictionary.TryGet<NumericToken>(NameToken.XrefStm, out var xrefStm)
                    ? xrefStm.Long
                    : tiedToXrefAtOffset;
            }

            var partType = xrefSection is XrefStream
                ? CrossReferenceType.Stream
                : CrossReferenceType.Table;
            
            return new CrossReferenceTablePart(
                xrefSection.ObjectOffsets,
                xrefSection.Offset,
                xrefSection.GetPrevious(),
                xrefSection.Dictionary ?? DictionaryToken.Empty,
                partType,
                tiedToXrefAtOffset);
        }
    }
}