namespace UglyToad.PdfPig.Graphics
{
    using System.Collections.Generic;
    using Operations;
    using Tokens;

    /// <summary>
    /// Graphics state operation factory interface.
    /// </summary>
    public interface IGraphicsStateOperationFactory
    {
        /// <summary>
        /// Create a graphics state operation.
        /// </summary>
        /// <param name="op">The operator token to build from.</param>
        /// <param name="operands"></param>
        IGraphicsStateOperation? Create(OperatorToken op, IReadOnlyList<IToken> operands);
    }
}