namespace UglyToad.PdfPig.IO
{
    using System;

    internal interface IInputBytes : IDisposable
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