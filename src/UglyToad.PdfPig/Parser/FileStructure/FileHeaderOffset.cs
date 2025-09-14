namespace UglyToad.PdfPig.Parser.FileStructure;

/// <summary>
/// How many bytes precede the "%PDF-" version header in the file. In some files this 'junk' can
/// offset all following offset bytes.
/// </summary>
internal readonly record struct FileHeaderOffset(int Value) : IEquatable<FileHeaderOffset>
{
    public override string ToString() => Value.ToString();
    
    public bool Equals(FileHeaderOffset other)
    {
        return Value == other.Value;
    }

    public override int GetHashCode() => Value.GetHashCode();
}