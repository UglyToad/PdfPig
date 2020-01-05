namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Font type (Serif or Sans-Serif).
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public enum AltoFontType
        {
            /// <summary>
            /// Serif.
            /// </summary>
            [XmlEnum("serif")]
            Serif,
            /// <summary>
            /// Sans-serif.
            /// </summary>
            [XmlEnum("sans-serif")]
            SansSerif,
        }
    }
}
