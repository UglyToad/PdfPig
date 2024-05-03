namespace UglyToad.PdfPig.Graphics
{
    using Operations;
    using Tokens;

    internal interface IGraphicsStateOperationFactory
    {
        IGraphicsStateOperation? Create(OperatorToken op, IReadOnlyList<IToken> operands);
    }
}