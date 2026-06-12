namespace UglyToad.PdfPig.Actions
{
    /// <summary>
    /// The operation a <see cref="MovieAction"/> performs on its target movie. Corresponds to the
    /// <c>Operation</c> entry of the movie action dictionary.
    /// </summary>
    public enum MovieOperation : byte
    {
        /// <summary>
        /// Start playing the movie, or resume it if paused. This is the default operation.
        /// </summary>
        Play,
        
        /// <summary>
        /// Stop playing the movie.
        /// </summary>
        Stop,
        
        /// <summary>
        /// Pause a playing movie.
        /// </summary>
        Pause,
        
        /// <summary>
        /// Resume a paused movie.
        /// </summary>
        Resume
    }
}
