namespace UglyToad.PdfPig.Fonts.TrueType
{
    using System;
    using System.IO;
    using UglyToad.PdfPig.Core;

    /// <inheritdoc cref="IWriteable" />
    /// <summary>
    /// A table directory entry from the TrueType font file. Indicates the position of the corresponding table
    /// data in the TrueType font.
    /// </summary>
    public readonly struct TrueTypeHeaderTable : IWriteable
    {
        #region RequiredTableTags
        /// <summary>
        /// Character to glyph mapping.
        /// </summary>
        /// <remarks>Required</remarks>
        public const string Cmap = "cmap";

        /// <summary>
        /// Glyph data.
        /// </summary>
        /// <remarks>Required</remarks>
        public const string Glyf = "glyf";

        /// <summary>
        /// Font header.
        /// </summary>
        /// <remarks>Required</remarks>
        public const string Head = "head";

        /// <summary>
        /// Horizontal header.
        /// </summary>
        /// <remarks>Required</remarks>
        public const string Hhea = "hhea";

        /// <summary>
        /// Horizontal metrics.
        /// </summary>
        /// <remarks>Required</remarks>
        public const string Hmtx = "hmtx";

        /// <summary>
        /// Index to location.
        /// </summary>
        /// <remarks>Required</remarks>
        public const string Loca = "loca";

        /// <summary>
        /// Maximum profile.
        /// </summary>
        /// <remarks>Required</remarks>
        public const string Maxp = "maxp";

        /// <summary>
        /// Naming table.
        /// </summary>
        /// <remarks>Required</remarks>
        public const string Name = "name";

        /// <summary>
        /// PostScript information.
        /// </summary>
        /// <remarks>Required</remarks>
        public const string Post = "post";

        /// <summary>
        /// OS/2 and Windows specific metrics.
        /// </summary>
        /// <remarks>Required</remarks>
        public const string Os2 = "OS/2";
        #endregion

        #region OptionalTableTags
        /// <summary>
        /// Control Value Table.
        /// </summary>
        /// <remarks>Optional</remarks>
        public const string Cvt = "cvt ";

        /// <summary>
        /// Embedded bitmap data.
        /// </summary>
        /// <remarks>Optional</remarks>
        public const string Ebdt = "EBDT";

        /// <summary>
        /// Embedded bitmap location data.
        /// </summary>
        /// <remarks>Optional</remarks>
        public const string Eblc = "EBLC";

        /// <summary>
        /// Embedded bitmap scaling data.
        /// </summary>
        /// <remarks>Optional</remarks>
        public const string Ebsc = "EBSC";

        /// <summary>
        /// Font program.
        /// </summary>
        /// <remarks>Optional</remarks>
        public const string Fpgm = "fpgm";

        /// <summary>
        /// Grid-fitting and scan conversion procedure (grayscale).
        /// </summary>
        /// <remarks>Optional</remarks>
        public const string Gasp = "gasp";

        /// <summary>
        /// Horizontal device metrics.
        /// </summary>
        /// <remarks>Optional</remarks>
        public const string Hdmx = "hdmx";

        /// <summary>
        /// Kerning.
        /// </summary>
        /// <remarks>Optional</remarks>
        public const string Kern = "kern";

        /// <summary>
        /// Linear threshold title.
        /// </summary>
        /// <remarks>Optional</remarks>
        public const string Ltsh = "LTSH";

        /// <summary>
        /// CVT program.
        /// </summary>
        /// <remarks>Optional</remarks>
        public const string Prep = "prep";

        /// <summary>
        /// PCL5.
        /// </summary>
        /// <remarks>Optional</remarks>
        public const string Pclt = "PCLT";

        /// <summary>
        /// Vertical device metrics.
        /// </summary>
        /// <remarks>Optional</remarks>
        public const string Vdmx = "VDMX";

        /// <summary>
        /// Vertical metrics header.
        /// </summary>
        /// <remarks>Optional</remarks>
        public const string Vhea = "vhea";

        /// <summary>
        /// Vertical metrics.
        /// </summary>
        /// <remarks>Optional</remarks>
        public const string Vmtx = "vmtx";
        #endregion

        #region PostScriptTableTags
        /// <summary>
        /// Compact font format table. The corresponding table contains a Compact Font Format font representation 
        /// (also known as a PostScript Type 1, or CIDFont).
        /// </summary>
        /// <remarks>Optional</remarks>
        public const string Cff = "cff ";
        #endregion

        /// <summary>
        /// The 4 byte tag identifying the table.
        /// </summary>
        public string Tag { get; }

        /// <summary>
        /// The checksum for the table.
        /// </summary>
        public uint CheckSum { get; }

        /// <summary>
        /// Offset of the table data from the beginning of the file in bytes.
        /// </summary>
        public uint Offset { get; }

        /// <summary>
        /// The length of the table data in bytes.
        /// </summary>
        public uint Length { get; }

        /// <summary>
        /// Create a new <see cref="TrueTypeHeaderTable"/>.
        /// </summary>
        public TrueTypeHeaderTable(string tag, uint checkSum, uint offset, uint length)
        {
            if (tag == null)
            {
                throw new ArgumentNullException(nameof(tag));
            }

            if (tag.Length != 4)
            {
                throw new ArgumentException($"A TrueType table tag must be a uint32, 4 bytes long, instead got: {tag}.", nameof(tag));
            }

            Tag = tag;
            CheckSum = checkSum;
            Offset = offset;
            Length = length;
        }

        /// <summary>
        /// Gets an empty header table with the non-tag values set to zero.
        /// </summary>
        public static TrueTypeHeaderTable GetEmptyHeaderTable(string tag)
        {
            return new TrueTypeHeaderTable(tag, 0, 0, 0);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            for (var i = 0; i < Tag.Length; i++)
            {
                stream.WriteByte((byte)Tag[i]);
            }

            stream.WriteUInt(CheckSum);
            stream.WriteUInt(Offset);
            stream.WriteUInt(Length);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Tag} - Offset: {Offset} Length: {Length} Checksum: {CheckSum}";
        }
    }
}
