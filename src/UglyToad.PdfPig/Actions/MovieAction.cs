namespace UglyToad.PdfPig.Actions
{
    using Tokens;

    /// <summary>
    /// Movie action (PDF reference 8.5.3), playing a movie in a floating window or within the
    /// annotation rectangle of a movie annotation.
    /// </summary>
    public sealed class MovieAction : PdfAction
    {
        /// <summary>
        /// The movie annotation dictionary identifying the movie to be played (the <c>Annotation</c>
        /// entry), or <see langword="null"/> when the movie is identified by <see cref="Title"/> instead.
        /// </summary>
        public DictionaryToken? TargetAnnotation { get; }

        /// <summary>
        /// The title of the movie annotation identifying the movie to be played (the <c>T</c> entry), or
        /// <see langword="null"/> when the movie is identified by <see cref="TargetAnnotation"/> instead.
        /// This is a convenience key for correlating the action with a movie annotation by its title.
        /// </summary>
        public string? Title { get; }

        /// <summary>
        /// The operation to perform on the movie (the <c>Operation</c> entry). Default value:
        /// <see cref="MovieOperation.Play"/>.
        /// </summary>
        public MovieOperation Operation { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="targetAnnotation">The movie annotation identifying the movie to be played.</param>
        /// <param name="title">The title of the movie annotation identifying the movie to be played.</param>
        /// <param name="operation">The operation to perform on the movie.</param>
        public MovieAction(DictionaryToken? targetAnnotation, string? title, MovieOperation operation)
            : base(ActionType.Movie)
        {
            TargetAnnotation = targetAnnotation;
            Title = title;
            Operation = operation;
        }
    }
}
