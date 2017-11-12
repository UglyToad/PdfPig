namespace UglyToad.Pdf.Tokenization.Tokens
{
    public class IndirectReferenceToken : IDataToken<IndirectReference>
    {
        public IndirectReference Data { get; }

        public IndirectReferenceToken(IndirectReference data)
        {
            Data = data;
        }
    }

    public struct IndirectReference
    {
        public long ObjectNumber { get; }

        public long Generation { get; }

        public IndirectReference(long objectNumber, long generation)
        {
            ObjectNumber = objectNumber;
            Generation = generation;
        }
    }
}
