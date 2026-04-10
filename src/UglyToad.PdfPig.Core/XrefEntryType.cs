namespace UglyToad.PdfPig.Core;

/// <summary>
/// Indicates where an object is located in the Xref.
/// </summary>
public enum XrefEntryType : byte
{
    /// <summary>
    /// Free object.
    /// </summary>
    Free = 0,
    /// <summary>
    /// Located as an object in the file.
    /// </summary>
    File = 1,
    /// <summary>
    /// Located in a compressed object stream.
    /// </summary>
    ObjectStream = 2
}