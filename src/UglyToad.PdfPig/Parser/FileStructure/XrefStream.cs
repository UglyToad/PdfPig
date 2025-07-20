namespace UglyToad.PdfPig.Parser.FileStructure;

using Core;
using Tokens;

internal sealed class XrefStream
{
    public long FileOffset { get; }

    /// <summary>
    /// The corresponding byte offset for each keyed object in this document.
    /// </summary>
    public IReadOnlyDictionary<IndirectReference, long> ObjectOffsets { get; }

    public DictionaryToken StreamDictionary { get; }

    public XrefStream(
        long fileOffset,
        IReadOnlyDictionary<IndirectReference, long> objectOffsets,
        DictionaryToken streamDictionary)
    {
        FileOffset = fileOffset;
        ObjectOffsets = objectOffsets;
        StreamDictionary = streamDictionary;
    }

    public long? GetPrevious()
    {
        if (StreamDictionary.TryGet(NameToken.Prev, out NumericToken prev))
        {
            return prev.Long;
        }

        return null;
    }
}