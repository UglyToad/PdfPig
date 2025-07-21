namespace UglyToad.PdfPig.Parser.FileStructure;

using Core;
using Tokens;

internal sealed class XrefTable : IXrefSection
{
    /// <summary>
    /// The offset in the file of the "xref" operator.
    /// </summary>
    public long Offset { get; }

    /// <summary>
    /// The corresponding byte offset for each keyed object in this document.
    /// </summary>
    public IReadOnlyDictionary<IndirectReference, long> ObjectOffsets { get; }

    public DictionaryToken? Dictionary { get; }

    public XrefTable(
        long offset,
        IReadOnlyDictionary<IndirectReference, long> objectOffsets,
        DictionaryToken? trailer)
    {
        Offset = offset;
        ObjectOffsets = objectOffsets;
        Dictionary = trailer;
    }

    public long? GetPrevious()
    {
        if (Dictionary != null && Dictionary.TryGet(NameToken.Prev, out NumericToken prev))
        {
            return prev.Long;
        }

        return null;
    }
}