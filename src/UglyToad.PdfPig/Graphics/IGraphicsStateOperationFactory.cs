namespace UglyToad.PdfPig.Graphics
{
    using System.Collections.Generic;
    using Operations;
    using Tokens;
    using Util.JetBrains.Annotations;
    /// <summary>
    /// interface for Graphics State to create Operator token with operands 
    /// </summary>
    public interface IGraphicsStateOperationFactory
    {
        /// <summary>
        /// Create Operator token with operands 
        /// </summary>
        [CanBeNull]
        IGraphicsStateOperation Create(OperatorToken op, IReadOnlyList<IToken> operands);
    }
}