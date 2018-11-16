namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    /// <summary>
    /// The header table for the binary data of a Compact Font Format file.
    /// </summary>
    internal struct CompactFontFormatHeader
    {
        /// <summary>
        /// The major version of this font format. Starting at 1.
        /// </summary>
        public byte MajorVersion { get; }
        
        /// <summary>
        /// The minor version of this font format. Starting at 0. Indicates extensions to the format which
        /// are undetectable by readers which do not support them.
        /// </summary>
        public byte MinorVersion { get; }

        /// <summary>
        /// Indicates the size of this header in bytes so that future changes to the format may include extra data after the <see cref="OffsetSize"/> field.
        /// </summary>
        public byte SizeInBytes { get; }

        /// <summary>
        /// Specifies the size of all offsets relative to the start of the data in the font.
        /// </summary>
        public byte OffsetSize { get; }

        /// <summary>
        /// Creates a new <see cref="CompactFontFormatHeader"/>.
        /// </summary>
        /// <param name="majorVersion">The major version of this font format.</param>
        /// <param name="minorVersion">The minor version of this font format.</param>
        /// <param name="sizeInBytes">Indicates the size of this header in bytes so that future changes to the format may include extra data after the offsetSize field.</param>
        /// <param name="offsetSize">Specifies the size of all offsets relative to the start of the data in the font.</param>
        public CompactFontFormatHeader(byte majorVersion, byte minorVersion, byte sizeInBytes, byte offsetSize)
        {
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
            SizeInBytes = sizeInBytes;
            OffsetSize = offsetSize;
        }

        public override string ToString()
        {
            return $"Major: {MajorVersion}, Minor: {MinorVersion}, Header Size: {SizeInBytes}, Offset: {OffsetSize}";
        }
    }
}