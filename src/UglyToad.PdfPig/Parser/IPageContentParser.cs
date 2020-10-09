namespace UglyToad.PdfPig.Parser
{
    using System.Collections.Generic;
    using Core;
    using Graphics.Operations;
    using Logging;

    /// <summary>
    /// IPageContentParser
    /// </summary>
    public interface IPageContentParser
    {
        /// <summary>
        /// Parse
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="inputBytes"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        IReadOnlyList<IGraphicsStateOperation> Parse(int pageNumber, IInputBytes inputBytes,
            ILog log);
    }
}