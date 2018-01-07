namespace UglyToad.Pdf.IO
{
    internal interface IInputBytes
    {
        int CurrentOffset { get; }

        bool MoveNext();

        byte CurrentByte { get; }

        long Length { get; }

        byte? Peek();
        
        bool IsAtEnd();

        void Seek(long position);
    }
}