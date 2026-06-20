namespace UglyToad.PdfPig.Graphics.Colors.Icc
{
    using System.Collections.Generic;
    using Tokens;

    /// <summary>
    /// An ICC profile reference dictionary (PDF 2.0, ISO 32000-2 Table 402), referenced from an output
    /// intent's <c>DestOutputProfileRef</c> entry. It identifies an ICC profile that is <b>not</b> embedded
    /// in the document but may be obtained elsewhere (for example an industry-standard registry, or one of
    /// the locations listed in <see cref="Urls"/>).
    /// </summary>
    public sealed class IccProfileReference
    {
        /// <summary>
        /// (Optional) The colour space of the referenced profile, as the ICC data colour space signature
        /// (for example <c>CMYK</c>, <c>RGB </c> or <c>GRAY</c>).
        /// </summary>
        public string? ProfileCS { get; }

        /// <summary>
        /// (Optional) A human-readable name identifying the referenced profile.
        /// </summary>
        public string? ProfileName { get; }

        /// <summary>
        /// (Optional) The ICC version of the referenced profile (the raw bytes of the ICC profile header
        /// version field).
        /// </summary>
        public IReadOnlyList<byte>? ICCVersion { get; }

        /// <summary>
        /// (Optional) A checksum of the referenced profile (the 16-byte ICC profile ID / MD5).
        /// </summary>
        public IReadOnlyList<byte>? CheckSum { get; }

        /// <summary>
        /// (Optional) For an n-colourant (DeviceN) profile, a dictionary naming the colourants.
        /// </summary>
        public DictionaryToken? ColorantTable { get; }

        /// <summary>
        /// (Optional) An array of URL file specifications giving locations from which the referenced profile
        /// may be obtained.
        /// </summary>
        public ArrayToken? Urls { get; }

        internal IccProfileReference(string? profileCS, string? profileName, IReadOnlyList<byte>? iccVersion,
            IReadOnlyList<byte>? checkSum, DictionaryToken? colorantTable, ArrayToken? urls)
        {
            ProfileCS = profileCS;
            ProfileName = profileName;
            ICCVersion = iccVersion;
            CheckSum = checkSum;
            ColorantTable = colorantTable;
            Urls = urls;
        }
    }
}
