namespace UglyToad.PdfPig.Graphics
{
    using System.Collections.Generic;
    using Operations;
    using Tokens;

    internal interface IGraphicsStateOperationFactory
    {
        IGraphicsStateOperation? Create(OperatorToken op, IReadOnlyList<IToken> operands);
    }
}