namespace UglyToad.PdfPig.Tokenization.Scanner
{
    using ContentStream;

    internal interface IObjectLocationProvider
    {
        bool TryGetOffset(IndirectReference reference, out long offset);

        void UpdateOffset(IndirectReference reference, long offset);
    }
}