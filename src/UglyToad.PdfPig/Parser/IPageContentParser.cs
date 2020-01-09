namespace UglyToad.PdfPig.Parser
{
    using System.Collections.Generic;
    using Core;
    using Graphics.Operations;

    internal interface IPageContentParser
    {
        IReadOnlyList<IGraphicsStateOperation> Parse(int pageNumber, IInputBytes inputBytes);
    }
}