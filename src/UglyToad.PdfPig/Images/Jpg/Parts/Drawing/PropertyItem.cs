 namespace UglyToad.PdfPig.Images.Jpg.Parts.Drawing
{
    using UglyToad.PdfPig.Util.JetBrains.Annotations;

    // sdkinc\imaging.h
    /// <summary>
    /// Encapsulates a metadata property to be included in an image file.
    /// </summary>
    internal sealed class PropertyItem
    {
        internal PropertyItem()
        {
        }

        /// <summary>
        /// Represents the ID of the property.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Represents the length of the property.
        /// </summary>
        public int Len { get; set; }

        /// <summary>
        /// Represents the type of the property.
        /// </summary>
        public short Type { get; set; }

        /// <summary>
        /// Contains the property value.
        /// </summary>
        [CanBeNull]
        public byte[] Value { get; set; }
    }
}