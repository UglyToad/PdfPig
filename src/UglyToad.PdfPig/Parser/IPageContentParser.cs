namespace UglyToad.PdfPig.Parser
{
    using Core;
    using Graphics.Operations;
    using Logging;
    using System.Collections.Generic;

    /// <summary>
    /// Page content parser interface.
    /// </summary>
    public interface IPageContentParser
    {
        /// <summary>
        /// Parse the <see cref="IInputBytes"/> into <see cref="IGraphicsStateOperation"/>s.
        /// </summary>
        IReadOnlyList<IGraphicsStateOperation> Parse(int pageNumber, IInputBytes inputBytes, ILog log);
    }
}