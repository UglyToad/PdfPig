namespace UglyToad.Pdf.Parser
{
    using System.Collections.Generic;
    using Graphics;
    using Graphics.Operations;
    using IO;

    internal interface IPageContentParser
    {
        IReadOnlyList<IGraphicsStateOperation> Parse(IGraphicsStateOperationFactory operationFactory, IInputBytes inputBytes);
    }
}