namespace UglyToad.PdfPig.Tokenization.Tokens
{
    using ContentStream;

    internal class IndirectReferenceToken : IDataToken<IndirectReference>
    {
        public IndirectReference Data { get; }

        public IndirectReferenceToken(IndirectReference data)
        {
            Data = data;
        }

        public override string ToString()
        {
            return $"{Data}";
        }
    }
}
