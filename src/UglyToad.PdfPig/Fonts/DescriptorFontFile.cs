namespace UglyToad.PdfPig.Fonts
{
    using Tokens;

    /// <summary>
    /// Holds the location and type of the stream containing the corresponding font program.
    /// </summary>
    /// <remarks>
    /// This can either be a Type 1 font program (FontFile - <see cref="FontFileType.Type1"/>),
    /// a TrueType font program (FontFile2 - <see cref="FontFileType.TrueType"/>) or a font program
    /// whose format is given by the Subtype of the stream dictionary (FontFile3 - <see cref="FontFileType.FromSubtype"/>).
    /// At most only 1 of these entries is present.
    /// </remarks>
    public class DescriptorFontFile
    {
        /// <summary>
        /// The object containing the stream for this font program.
        /// </summary>
        public IndirectReferenceToken ObjectKey { get; }
        
        /// <summary>
        /// The type of the font program represented by this descriptor.
        /// </summary>
        public FontFileType FileType { get; }

        /// <summary>
        /// Create a new <see cref="DescriptorFontFile"/>.
        /// </summary>
        public DescriptorFontFile(IndirectReferenceToken key, FontFileType fileType)
        {
            ObjectKey = key;
            FileType = fileType;
        }

        /// <summary>
        /// The type of font program represented by the stream used by this font descriptor.
        /// </summary>
        public enum FontFileType
        {
            /// <summary>
            /// A Type 1 font program.
            /// </summary>
            Type1,
            /// <summary>
            /// A TrueType font program.
            /// </summary>
            TrueType,
            /// <summary>
            /// A type defined by the stream dictionary's Subtype entry.
            /// </summary>
            FromSubtype
        }
    }
}