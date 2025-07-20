namespace UglyToad.PdfPig.Parser.FileStructure;

using Core;
using Tokens;

internal sealed class XrefTable
{
    /// <summary>
    /// The offset in the file of the "xref" operator.
    /// </summary>
    public long FileOffset { get; }

    /// <summary>
    /// The corresponding byte offset for each keyed object in this document.
    /// </summary>
    public IReadOnlyDictionary<IndirectReference, long> ObjectOffsets { get; }

    public DictionaryToken? Trailer { get; }

    public XrefTable(
        long fileOffset,
        IReadOnlyDictionary<IndirectReference, long> objectOffsets,
        DictionaryToken? trailer)
    {
        FileOffset = fileOffset;
        ObjectOffsets = objectOffsets;
        Trailer = trailer;
    }

    public long? GetPrevious()
    {
        if (Trailer != null && Trailer.TryGet(NameToken.Prev, out NumericToken prev))
        {
            return prev.Long;
        }

        return null;
    }
}