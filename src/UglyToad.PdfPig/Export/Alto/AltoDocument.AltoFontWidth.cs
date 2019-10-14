namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Font width (Fixed or proportional).
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public enum AltoFontWidth
        {
            /// <summary>
            /// Proportional.
            /// </summary>
            [XmlEnum("proportional")]
            Proportional,
            /// <summary>
            /// Remarks.
            /// </summary>
            [XmlEnum("fixed")]
            Fixed
        }
    }
}
