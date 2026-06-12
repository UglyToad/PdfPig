namespace UglyToad.PdfPig.Actions
{
    /// <summary>
    /// Specifies how a destination document should be opened by a <see cref="LaunchAction"/>.
    /// Corresponds to the <c>NewWindow</c> entry of the launch action dictionary.
    /// </summary>
    public enum OpenMode : byte
    {
        /// <summary>
        /// The <c>NewWindow</c> entry is absent: behave in accordance with the current user preference.
        /// </summary>
        UserPreference,
        
        /// <summary>
        /// Open the destination document in the same window (<c>NewWindow</c> is <see langword="false"/>).
        /// </summary>
        SameWindow,
        
        /// <summary>
        /// Open the destination document in a new window (<c>NewWindow</c> is <see langword="true"/>).
        /// </summary>
        NewWindow
    }
}
