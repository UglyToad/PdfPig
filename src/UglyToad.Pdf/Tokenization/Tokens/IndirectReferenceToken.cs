namespace UglyToad.Pdf.Tokenization.Tokens
{
    using ContentStream;

    public class IndirectReferenceToken : IDataToken<IndirectReference>
    {
        public IndirectReference Data { get; }

        public IndirectReferenceToken(IndirectReference data)
        {
            Data = data;
        }
    }

}
