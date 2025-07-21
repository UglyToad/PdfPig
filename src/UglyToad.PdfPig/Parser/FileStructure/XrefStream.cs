namespace UglyToad.PdfPig.Parser.FileStructure;

using Core;
using Tokens;

internal sealed class XrefStream : IXrefSection
{
    public long Offset { get; }

    /// <summary>
    /// The corresponding byte offset for each keyed object in this document.
    /// </summary>
    public IReadOnlyDictionary<IndirectReference, long> ObjectOffsets { get; }

    public DictionaryToken Dictionary { get; }

    public XrefStream(
        long offset,
        IReadOnlyDictionary<IndirectReference, long> objectOffsets,
        DictionaryToken streamDictionary)
    {
        Offset = offset;
        ObjectOffsets = objectOffsets;
        Dictionary = streamDictionary;
    }

    public long? GetPrevious()
    {
        if (Dictionary.TryGet(NameToken.Prev, out NumericToken prev))
        {
            return prev.Long;
        }

        return null;
    }
}