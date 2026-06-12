namespace UglyToad.PdfPig.Actions
{
    /// <summary>
    /// Sound action (PDF reference 8.5.3), playing a sound through the viewer application's speakers.
    /// </summary>
    public sealed class SoundAction : PdfAction
    {
        /// <summary>
        /// The volume at which to play the sound, in the range -1.0 to 1.0. Default value: 1.0.
        /// </summary>
        public double Volume { get; }

        /// <summary>
        /// Whether to play the sound synchronously. When <see langword="true"/>, the viewer application
        /// allows no further user interaction (other than cancelling) until the sound has played.
        /// Default value: <see langword="false"/>.
        /// </summary>
        public bool Synchronous { get; }

        /// <summary>
        /// Whether to repeat the sound indefinitely. Default value: <see langword="false"/>.
        /// </summary>
        public bool Repeat { get; }

        /// <summary>
        /// Whether to mix this sound with any other sound already playing. Default value: <see langword="false"/>.
        /// </summary>
        public bool Mix { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public SoundAction(double volume, bool synchronous, bool repeat, bool mix) : base(ActionType.Sound)
        {
            Volume = volume;
            Synchronous = synchronous;
            Repeat = repeat;
            Mix = mix;
        }
    }
}
