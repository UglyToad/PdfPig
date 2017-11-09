namespace UglyToad.Pdf.Text
{
    using System.Collections.Generic;

    public interface ITextComponentApproach
    {
        bool CanRead(byte b, int offset);

        ITextObjectComponent Read(IReadOnlyList<byte> readBytes, IEnumerable<byte> furtherBytes, out int offset);
    }
}