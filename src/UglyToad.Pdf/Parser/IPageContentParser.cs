namespace UglyToad.Pdf.Parser
{
    using Content;
    using Graphics;
    using IO;

    internal interface IPageContentParser
    {
        PageContent Parse(IGraphicsStateOperationFactory operationFactory, IInputBytes inputBytes);
    }
}