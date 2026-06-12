namespace UglyToad.PdfPig.Actions
{
    using System.Collections.Generic;

    /// <summary>
    /// Set-OCG-state action (PDF reference 8.5.3), setting the state of one or more optional content
    /// groups (layers) to ON, OFF or toggled.
    /// </summary>
    public sealed class SetOcgStateAction : PdfAction
    {
        /// <summary>
        /// The names of the optional content groups to turn on.
        /// </summary>
        public IReadOnlyList<string> On { get; }

        /// <summary>
        /// The names of the optional content groups to turn off.
        /// </summary>
        public IReadOnlyList<string> Off { get; }

        /// <summary>
        /// The names of the optional content groups to toggle (reverse their current state).
        /// </summary>
        public IReadOnlyList<string> Toggle { get; }

        /// <summary>
        /// Whether radio-button relationships between optional content groups should be preserved when
        /// the groups are turned on (the <c>PreserveRB</c> entry). Default value: <see langword="true"/>.
        /// </summary>
        public bool PreserveRadioButtons { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="on">The names of the optional content groups to turn on.</param>
        /// <param name="off">The names of the optional content groups to turn off.</param>
        /// <param name="toggle">The names of the optional content groups to toggle.</param>
        /// <param name="preserveRadioButtons">Whether radio-button relationships should be preserved.</param>
        public SetOcgStateAction(
            IReadOnlyList<string> on,
            IReadOnlyList<string> off,
            IReadOnlyList<string> toggle,
            bool preserveRadioButtons) : base(ActionType.SetOCGState)
        {
            On = on;
            Off = off;
            Toggle = toggle;
            PreserveRadioButtons = preserveRadioButtons;
        }
    }
}
