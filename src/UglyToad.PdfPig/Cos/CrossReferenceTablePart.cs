namespace UglyToad.PdfPig.Cos
{
    using System.Collections.Generic;
    using ContentStream;
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
    internal class CrossReferenceTablePart
    {
        public IReadOnlyDictionary<IndirectReference, long> ObjectOffsets { get; }

        public long Offset { get; private set; }

        public long Previous { get; }

        public DictionaryToken Dictionary { get; private set; }

        public CrossReferenceType Type { get; }

        public CrossReferenceTablePart(IReadOnlyDictionary<IndirectReference, long> objectOffsets, long offset, long previous, DictionaryToken dictionary, CrossReferenceType type)
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
            Dictionary = Dictionary.With(NameToken.Prev, new NumericToken(offset));
        }

        public long GetPreviousOffset()
        {
            if (Dictionary.TryGet(NameToken.Prev, out var token) && token is NumericToken numeric)
            {
                return numeric.Long;
            }

            return -1;
        }
    }
}