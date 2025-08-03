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

    public XrefOffsetCorrection CorrectionType { get; }

    public long OffsetCorrection { get; }

    public XrefStream(
        long offset,
        IReadOnlyDictionary<IndirectReference, long> objectOffsets,
        DictionaryToken streamDictionary,
        XrefOffsetCorrection correctionType,
        long offsetCorrection)
    {
        Offset = offset;
        ObjectOffsets = objectOffsets;
        Dictionary = streamDictionary;
        CorrectionType = correctionType;
        OffsetCorrection = offsetCorrection;
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