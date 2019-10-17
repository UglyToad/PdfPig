namespace UglyToad.PdfPig.Fonts
{
    using Geometry;

    /// <summary>
    /// A font which supports a vertical writing mode in addition to the default horizontal writing mode.
    /// </summary>
    internal interface IVerticalWritingSupported
    {
        /// <summary>
        /// In vertical fonts the glyph position is described by a position vector from the origin used for horizontal writing.
        /// The position vector is applied to the horizontal writing origin to give a new vertical writing origin. 
        /// </summary>
        PdfVector GetPositionVector(int characterCode);

        PdfVector GetDisplacementVector(int characterCode);
    }
}
