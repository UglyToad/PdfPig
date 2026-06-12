namespace UglyToad.PdfPig.Actions
{
    /// <summary>
    /// Transition action (PDF reference 8.5.3), updating the display of a document using a transition
    /// dictionary. Typically used during a presentation to control the visual effect between pages.
    /// </summary>
    public sealed class TransAction : PdfAction
    {
        /// <summary>
        /// The transition style to use (the transition dictionary's <c>S</c> entry), such as
        /// <c>Split</c>, <c>Wipe</c>, <c>Dissolve</c> or <c>Fade</c>. Default value: <c>R</c> (replace).
        /// </summary>
        public string Style { get; }

        /// <summary>
        /// The duration of the transition effect, in seconds (the transition dictionary's <c>D</c> entry).
        /// Default value: 1.0.
        /// </summary>
        public double Duration { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="style">The transition style.</param>
        /// <param name="duration">The duration of the transition effect, in seconds.</param>
        public TransAction(string style, double duration) : base(ActionType.Trans)
        {
            Style = style;
            Duration = duration;
        }
    }
}
