namespace UglyToad.PdfPig.Core;

/// <summary>
/// Information about where an object is located in the file according to the Xref (or brute force parsing).
/// </summary>
public readonly struct XrefLocation
{
    /// <summary>
    /// Which type of location is indicated.
    /// </summary>
    public readonly XrefEntryType Type;

    /// <summary>
    /// If <see cref="Type"/> is <see cref="XrefEntryType.File"/> then byte offset, otherwise <see cref="XrefEntryType.ObjectStream"/> this is the stream number.
    /// </summary>
    public readonly long Value1;

    /// <summary>
    /// If <see cref="Type"/> is <see cref="XrefEntryType.ObjectStream"/> then the index of the object in the stream.
    /// </summary>
    public readonly int Value2; // only used for ObjectStream

    private XrefLocation(XrefEntryType type, long value1, int value2)
    {
        Type = type;
        Value1 = value1;
        Value2 = value2;
    }

    /// <summary>
    /// Create a location mapped to a byte offset in the file.
    /// </summary>
    public static XrefLocation File(long offset)
        => new XrefLocation(XrefEntryType.File, offset, 0);

    /// <summary>
    /// Create a location mapped to an index inside and object stream.
    /// </summary>
    public static XrefLocation Stream(long objStream, int index)
        => new XrefLocation(XrefEntryType.ObjectStream, objStream, index);

}