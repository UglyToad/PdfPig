namespace UglyToad.Pdf.Fonts
{
    using ContentStream;
    using Cos;

    /// <summary>
    /// The bytes of the stream containing the font program.
    /// </summary>
    /// <remarks>
    /// This can either be a Type 1 font program (FontFile - <see cref="FontFileType.Type1"/>),
    /// a TrueType font program (FontFile2 - <see cref="FontFileType.TrueType"/>) or a font program
    /// whose format is given by the Subtype of the stream dictionary (FontFile3 - <see cref="FontFileType.FromSubtype"/>).
    /// At most only 1 of these entries is present.
    /// </remarks>
    internal class DescriptorFontFile
    {
        public IndirectReference ObjectKey { get; }

        public byte[] FileBytes { get; }

        public FontFileType FileType { get; }

        public DescriptorFontFile(IndirectReference key, FontFileType fileType)
        {
            ObjectKey = key;
            FileBytes = new byte[0];
            FileType = fileType;
        }

        public enum FontFileType
        {
            Type1,
            TrueType,
            FromSubtype
        }
    }
}