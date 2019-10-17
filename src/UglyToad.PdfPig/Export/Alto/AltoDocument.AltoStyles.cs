namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Styles.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoStyles
        {
            /// <summary>
            /// Text Style.
            /// </summary>
            [XmlElement("TextStyle")]
            public AltoTextStyle[] TextStyle { get; set; }

            /// <summary>
            /// Paragraph Style.
            /// </summary>
            [XmlElement("ParagraphStyle")]
            public AltoParagraphStyle[] ParagraphStyle { get; set; }
        }
    }
}
