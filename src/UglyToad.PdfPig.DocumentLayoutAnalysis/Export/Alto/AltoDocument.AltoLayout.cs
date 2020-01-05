namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Layout.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoLayout
        {
            /// <remarks/>
            [XmlElement("Page")]
            public AltoPage[] Pages { get; set; }

            /// <remarks/>
            [XmlAttribute("STYLEREFS", DataType = "IDREFS")]
            public string StyleRefs { get; set; }
        }
    }
}
