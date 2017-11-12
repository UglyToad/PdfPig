namespace UglyToad.Pdf.IO
{
    public interface IInputBytes
    {
        int CurrentOffset { get; }

        bool MoveNext();

        byte CurrentByte { get; }

        byte? Peek();
        
        bool IsAtEnd();
    }
}