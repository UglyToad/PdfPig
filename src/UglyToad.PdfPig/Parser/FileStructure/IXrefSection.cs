namespace UglyToad.PdfPig.Parser.FileStructure;

using Core;
using Tokens;

internal interface IXrefSection
{
    /// <summary>
    /// The byte offset of this xref in the file. For tables this is the position of the
    /// "xref" operator, for stream objects this is the start of the object number marker, e.g. "14 0 obj".
    /// </summary>
    public long Offset { get; }

    /// <summary>
    /// The bytes offsets of the objects in this xref.
    /// </summary>
    public IReadOnlyDictionary<IndirectReference, long> ObjectOffsets { get; }

    /// <summary>
    /// The dictionary for this xref, for the trailer xref this is the trailer dictionary, for streams the stream dictionary.
    /// </summary>
    public DictionaryToken? Dictionary { get; }

    public long? GetPrevious();

    /// <summary>
    /// If we had to apply a correction to locate this xref this is how we found it.
    /// </summary>
    public XrefOffsetCorrection CorrectionType { get; }

    /// <summary>
    /// If we had to apply a correction to locate this xref this is how many bytes from the original location we had to move.
    /// </summary>
    public long OffsetCorrection { get; }
}

internal enum XrefOffsetCorrection
{
    /// <summary>
    /// The xref was found at exactly the specified byte offset in the file.
    /// </summary>
    None = 0,
    /// <summary>
    /// The xref was shifted by the offset of the version header start comment in the file.
    /// </summary>
    FileHeaderOffset = 1,
    /// <summary>
    /// The xref was randomly not at the correct location, but we found it nearby.
    /// </summary>
    Random = 2,
}