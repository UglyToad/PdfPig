namespace UglyToad.Pdf.Content
{
    using System.Collections.Generic;
    using Graphics.Operations;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// This should contain a replayable stack of drawing instructions for page content
    /// from a content stream in addition to lazily evaluated state such as text on the page or images.
    /// </remarks>
    public class PageContent
    {
        internal IReadOnlyList<IGraphicsStateOperation> GraphicsStateOperations { get; set; }

        public IReadOnlyList<string> Text { get; set; }
    }
}
