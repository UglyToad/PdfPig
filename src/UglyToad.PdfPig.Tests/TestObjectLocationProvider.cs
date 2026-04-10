namespace UglyToad.PdfPig.Tests
{
    using PdfPig.Core;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokens;

    internal class TestObjectLocationProvider : IObjectLocationProvider
    {
        public Dictionary<IndirectReference, XrefLocation> Offsets { get; } = new Dictionary<IndirectReference, XrefLocation>();

        public bool TryGetOffset(IndirectReference reference, out XrefLocation offset)
        {
            return Offsets.TryGetValue(reference, out offset);
        }

        public void UpdateOffset(IndirectReference reference, XrefLocation offset)
        {
            Offsets[reference] = offset;
        }

        public bool TryGetCached(IndirectReference reference, out ObjectToken objectToken)
        {
            objectToken = null;
            return false;
        }

        public void Cache(ObjectToken objectToken, bool force = false)
        {
        }
    }
}