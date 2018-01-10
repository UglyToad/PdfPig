namespace UglyToad.PdfPig.Cos
{
    using System.Collections.Generic;
    using ContentStream;
    using ContentStream.TypedAccessors;

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
    internal class CrossReferenceTablePart
    {
        public IReadOnlyDictionary<CosObjectKey, long> ObjectOffsets { get; }

        public long Offset { get; private set; }

        public long Previous { get; }

        public PdfDictionary Dictionary { get; }

        public CrossReferenceType Type { get; }

        public CrossReferenceTablePart(IReadOnlyDictionary<CosObjectKey, long> objectOffsets, long offset, long previous, PdfDictionary dictionary, CrossReferenceType type)
        {
            ObjectOffsets = objectOffsets;
            Offset = offset;
            Previous = previous;
            Dictionary = dictionary;
            Type = type;
        }

        public void FixOffset(long offset)
        {
            Offset = offset;
            Dictionary.SetLong(CosName.PREV, offset);
        }
    }
}