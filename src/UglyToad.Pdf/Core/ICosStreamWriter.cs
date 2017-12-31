namespace UglyToad.Pdf.Core
{
    using System.IO;

    public interface ICosStreamWriter
    {
        void WriteToPdfStream(BinaryWriter output);
    }
}
