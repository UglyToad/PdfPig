namespace UglyToad.PdfPig.Parser.FileStructure;

/// <summary>
/// How many bytes precede the "%PDF-" version header in the file. In some files this 'junk' can
/// offset all following offset bytes.
/// </summary>
internal readonly struct FileHeaderOffset(int value)
{
    public int Value => value;

    public override string ToString() => value.ToString();

    public override bool Equals(object? obj) =>
        obj is FileHeaderOffset other && value == other.Value;

    public override int GetHashCode() => value.GetHashCode();
}