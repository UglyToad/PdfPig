namespace UglyToad.PdfPig.Artwork
{
    /// <summary>
    /// How keyframe animations contained in 3D artwork are driven (ISO 32000-2, Table 312).
    /// </summary>
    public sealed class Artwork3DAnimationStyle
    {
        /// <summary>
        /// The animation style that applies when a document specifies none: animations are not driven.
        /// </summary>
        public static Artwork3DAnimationStyle None { get; } = new Artwork3DAnimationStyle(Artwork3DAnimationType.None, 0, 1);

        /// <summary>
        /// The animation style (<c>Subtype</c>).
        /// </summary>
        public Artwork3DAnimationType Type { get; }

        /// <summary>
        /// The number of times the animation is played; a negative number means it repeats indefinitely
        /// (<c>PC</c>). Default 0.
        /// </summary>
        public int PlayCount { get; }

        /// <summary>
        /// The time multiplier: a value greater than one speeds the animation up (<c>TM</c>). Default 1.
        /// </summary>
        public double TimeMultiplier { get; }

        internal Artwork3DAnimationStyle(Artwork3DAnimationType type, int playCount, double timeMultiplier)
        {
            Type = type;
            PlayCount = playCount;
            TimeMultiplier = timeMultiplier;
        }
    }
}
