namespace UglyToad.PdfPig.Parser
{
    using System.Collections.Generic;
    using Graphics.Operations;
    using IO;

    internal interface IPageContentParser
    {
        IReadOnlyList<IGraphicsStateOperation> Parse(IInputBytes inputBytes);
    }
}