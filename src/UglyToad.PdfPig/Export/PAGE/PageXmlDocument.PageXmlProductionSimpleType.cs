namespace UglyToad.PdfPig.Export.PAGE
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
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlProductionSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("printed")]
            Printed,

            /// <remarks/>
            [XmlEnumAttribute("typewritten")]
            Typewritten,

            /// <remarks/>
            [XmlEnumAttribute("handwritten-cursive")]
            HandwrittenCursive,

            /// <remarks/>
            [XmlEnumAttribute("handwritten-printscript")]
            HandwrittenPrintscript,

            /// <remarks/>
            [XmlEnumAttribute("medieval-manuscript")]
            MedievalManuscript,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            Other,
        }
    }
}
