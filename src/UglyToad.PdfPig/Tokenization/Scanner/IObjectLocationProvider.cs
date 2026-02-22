namespace UglyToad.PdfPig.Tokenization.Scanner
{
    using System.Diagnostics.CodeAnalysis;
    using Core;
    using Tokens;

    internal interface IObjectLocationProvider
    {
        bool TryGetOffset(IndirectReference reference, out XrefLocation offset);

        void UpdateOffset(IndirectReference reference, XrefLocation offset);

        bool TryGetCached(IndirectReference reference, [NotNullWhen(true)] out ObjectToken? objectToken);

        void Cache(ObjectToken objectToken, bool force = false);
    }
}