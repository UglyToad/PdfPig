namespace UglyToad.PdfPig.Tokenization.Scanner
{
    using Core;
    using Tokens;

    internal interface IObjectLocationProvider
    {
        bool TryGetOffset(IndirectReference reference, out long offset);

        void UpdateOffset(IndirectReference reference, long offset);

        bool TryGetCached(IndirectReference reference, out ObjectToken objectToken);

        void Cache(ObjectToken objectToken, bool force = false);
    }
}