namespace UglyToad.PdfPig.Core
{
    using System.IO;

    internal interface ICosStreamWriter
    {
        void WriteToPdfStream(BinaryWriter output);
    }
}
