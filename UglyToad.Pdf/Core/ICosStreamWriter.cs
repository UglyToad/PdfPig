namespace UglyToad.Pdf.Core
{
    using System.IO;

    public interface ICosStreamWriter
    {
        void WriteToPdfStream(StreamWriter output);
    }
}
