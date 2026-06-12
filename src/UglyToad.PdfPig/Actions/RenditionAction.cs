namespace UglyToad.PdfPig.Actions
{
    using Tokens;

    /// <summary>
    /// Rendition action (PDF reference 8.5.3), controlling the playing of multimedia content associated
    /// with a screen annotation.
    /// </summary>
    public sealed class RenditionAction : PdfAction
    {
        /// <summary>
        /// The rendition dictionary that shall be associated with the screen annotation (the <c>R</c>
        /// entry), or <see langword="null"/> if it is absent.
        /// </summary>
        public DictionaryToken? Rendition { get; }

        /// <summary>
        /// The screen annotation dictionary the rendition operates on (the <c>AN</c> entry), or
        /// <see langword="null"/> if it is absent.
        /// </summary>
        public DictionaryToken? TargetAnnotation { get; }

        /// <summary>
        /// The operation to perform (the <c>OP</c> entry), or <see langword="null"/> when the action is
        /// driven by <see cref="JavaScript"/> instead.
        /// </summary>
        public RenditionOperation? Operation { get; }

        /// <summary>
        /// A JavaScript script that determines how the rendition is played (the <c>JS</c> entry), or
        /// <see langword="null"/> if it is absent.
        /// </summary>
        public string? JavaScript { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="rendition">The rendition dictionary associated with the screen annotation.</param>
        /// <param name="targetAnnotation">The screen annotation the rendition operates on.</param>
        /// <param name="targetAnnotationName">The name (<c>NM</c>) of the target screen annotation.</param>
        /// <param name="operation">The operation to perform.</param>
        /// <param name="javaScript">A JavaScript script determining how the rendition is played.</param>
        public RenditionAction(
            DictionaryToken? rendition,
            DictionaryToken? targetAnnotation,
            RenditionOperation? operation,
            string? javaScript) : base(ActionType.Rendition)
        {
            Rendition = rendition;
            TargetAnnotation = targetAnnotation;
            Operation = operation;
            JavaScript = javaScript;
        }
    }
}
