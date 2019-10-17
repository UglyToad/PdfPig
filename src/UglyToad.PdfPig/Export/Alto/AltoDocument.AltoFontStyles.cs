namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Font styles.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Flags]
        [Serializable]
        [XmlType(AnonymousType = true, Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public enum AltoFontStyles
        {
            /// <summary>
            /// Bold.
            /// </summary>
            [XmlEnum("bold")]
            Bold = 1,
            /// <summary>
            /// Italics.
            /// </summary>
            [XmlEnum("italics")]
            Italics = 2,
            /// <summary>
            /// Subscript.
            /// </summary>
            [XmlEnum("subscript")]
            Subscript = 4,
            /// <summary>
            /// Superscript.
            /// </summary>
            [XmlEnum("superscript")]
            Superscript = 8,
            /// <summary>
            /// Small caps.
            /// </summary>
            [XmlEnum("smallcaps")]
            SmallCaps = 16,
            /// <summary>
            /// Underline.
            /// </summary>
            [XmlEnum("underline")]
            Underline = 32,
        }
    }
}
