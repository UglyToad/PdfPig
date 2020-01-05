namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.PAGE
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class PageXmlDocument
    {
        /// <summary>
        /// Text production type
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCode("xsd", "4.6.1055.0")]
        [Serializable()]
        [XmlType(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlProductionSimpleType
        {

            /// <remarks/>
            [XmlEnum("printed")]
            Printed,

            /// <remarks/>
            [XmlEnum("typewritten")]
            Typewritten,

            /// <remarks/>
            [XmlEnum("handwritten-cursive")]
            HandwrittenCursive,

            /// <remarks/>
            [XmlEnum("handwritten-printscript")]
            HandwrittenPrintscript,

            /// <remarks/>
            [XmlEnum("medieval-manuscript")]
            MedievalManuscript,

            /// <remarks/>
            [XmlEnum("other")]
            Other,
        }
    }
}
