namespace UglyToad.PdfPig.IO
{
    internal interface IInputBytes
    {
        long CurrentOffset { get; }

        bool MoveNext();

        byte CurrentByte { get; }

        long Length { get; }

        byte? Peek();
        
        bool IsAtEnd();

        void Seek(long position);
    }
}