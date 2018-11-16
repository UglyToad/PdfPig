namespace UglyToad.PdfPig.Graphics
{
    using System.Collections.Generic;
    using Operations;
    using Tokens;
    using Util.JetBrains.Annotations;

    internal interface IGraphicsStateOperationFactory
    {
        [CanBeNull]
        IGraphicsStateOperation Create(OperatorToken op, IReadOnlyList<IToken> operands);
    }
}