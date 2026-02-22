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
    public IReadOnlyDictionary<IndirectReference, XrefLocation> ObjectOffsets { get; }

    public DictionaryToken? Dictionary { get; }

    public XrefOffsetCorrection CorrectionType { get; }

    public long OffsetCorrection { get; }

    public XrefTable(
        long offset,
        IReadOnlyDictionary<IndirectReference, XrefLocation> objectOffsets,
        DictionaryToken? trailer,
        XrefOffsetCorrection correctionType,
        long offsetCorrection)
    {
        Offset = offset;
        ObjectOffsets = objectOffsets;
        Dictionary = trailer;
        CorrectionType = correctionType;
        OffsetCorrection = offsetCorrection;
    }

    public long? GetPrevious()
    {
        if (Dictionary != null && Dictionary.TryGet(NameToken.Prev, out NumericToken prev))
        {
            return prev.Long;
        }

        return null;
    }

    public long? GetXRefStm()
    {
        if (Dictionary != null && Dictionary.TryGet(NameToken.XrefStm, out NumericToken xRefStm))
        {
            return xRefStm.Long;
        }

        return null;
    }
}