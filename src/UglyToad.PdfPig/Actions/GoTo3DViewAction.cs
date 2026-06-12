namespace UglyToad.PdfPig.Actions
{
    using Tokens;

    /// <summary>
    /// Go-to-3D-view action (PDF reference 8.5.3), setting the current view of a 3D annotation.
    /// </summary>
    public sealed class GoTo3DViewAction : PdfAction
    {
        /// <summary>
        /// The 3D annotation dictionary whose view is to be set (the <c>TA</c> entry), or
        /// <see langword="null"/> if it is not a dictionary.
        /// </summary>
        public DictionaryToken? TargetAnnotation { get; }

        /// <summary>
        /// The view to use, when specified as a name (such as <c>F</c>, <c>L</c>, <c>N</c> or <c>P</c>)
        /// or as a named view string, or <see langword="null"/> when the view is specified some other way.
        /// </summary>
        public string? ViewName { get; }

        /// <summary>
        /// The view to use, when specified as a zero-based index into the target annotation's array of
        /// views, or <see langword="null"/> when the view is not specified as an index.
        /// </summary>
        public int? ViewIndex { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="targetAnnotation">The 3D annotation dictionary whose view is to be set.</param>
        /// <param name="viewName">The view, when specified as a name or named view string.</param>
        /// <param name="viewIndex">The view, when specified as an index into the annotation's views.</param>
        public GoTo3DViewAction(DictionaryToken? targetAnnotation, string? viewName, int? viewIndex)
            : base(ActionType.GoTo3DView)
        {
            TargetAnnotation = targetAnnotation;
            ViewName = viewName;
            ViewIndex = viewIndex;
        }
    }
}
