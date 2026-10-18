namespace UglyToad.PdfPig.Artwork
{
    /// <summary>
    /// The state of a run-time instance of 3D artwork (ISO 32000-2, 13.6.2).
    /// </summary>
    public enum Artwork3DInstanceState : byte
    {
        /// <summary>
        /// The artwork has not been read, or its run-time instance has been discarded (<c>U</c>).
        /// </summary>
        Uninstantiated = 0,

        /// <summary>
        /// The artwork has been read and can be rendered; script-driven animation is disabled (<c>I</c>).
        /// </summary>
        Instantiated = 1,

        /// <summary>
        /// The artwork is instantiated and is being modified in real time (<c>L</c>).
        /// </summary>
        Live = 2
    }
}
