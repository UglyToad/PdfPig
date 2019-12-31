namespace UglyToad.PdfPig.Fonts.TrueType
{
    using System;
    using System.IO;
    using IO;
    using Util;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// A table directory entry from the TrueType font file.
    /// </summary>
    internal struct TrueTypeHeaderTable : IWriteable
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

        public const string Cff = "cff ";
        #endregion

        /// <summary>
        /// The 4 byte tag identifying the table.
        /// </summary>
        [NotNull]
        public string Tag { get; }

        /// <summary>
        /// The checksum for the table.
        /// </summary>
        public uint CheckSum { get; }

        /// <summary>
        /// Offset of the table from the beginning of the file.
        /// </summary>
        public uint Offset { get; }

        /// <summary>
        /// The length of the table.
        /// </summary>
        public uint Length { get; }

        public TrueTypeHeaderTable(string tag, uint checkSum, uint offset, uint length)
        {
            Tag = tag ?? throw new ArgumentNullException(nameof(tag));
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

        public override string ToString()
        {
            return $"{Tag} {Offset} {Length} {CheckSum}";
        }

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
    }
}
