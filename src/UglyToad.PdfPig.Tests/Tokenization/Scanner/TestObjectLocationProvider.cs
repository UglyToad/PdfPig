namespace UglyToad.PdfPig.Tests.Tokenization.Scanner
{
    using System.Collections.Generic;
    using PdfPig.ContentStream;
    using PdfPig.Tokenization.Scanner;

    internal class TestObjectLocationProvider : IObjectLocationProvider
    {
        public Dictionary<IndirectReference, long> Offsets { get; } = new Dictionary<IndirectReference, long>();

        public bool TryGetOffset(IndirectReference reference, out long offset)
        {
            return Offsets.TryGetValue(reference, out offset);
        }

        public void UpdateOffset(IndirectReference reference, long offset)
        {
            Offsets[reference] = offset;
        }
    }
}