namespace UglyToad.PdfPig.Export.PAGE
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class PageXmlDocument
    {
        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlGraphicsSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("logo")]
            Logo,

            /// <remarks/>
            [XmlEnumAttribute("letterhead")]
            Letterhead,

            /// <remarks/>
            [XmlEnumAttribute("decoration")]
            Decoration,

            /// <remarks/>
            [XmlEnumAttribute("frame")]
            Frame,

            /// <remarks/>
            [XmlEnumAttribute("handwritten-annotation")]
            HandwrittenAnnotation,

            /// <remarks/>
            [XmlEnumAttribute("stamp")]
            Stamp,

            /// <remarks/>
            [XmlEnumAttribute("signature")]
            Signature,

            /// <remarks/>
            [XmlEnumAttribute("barcode")]
            Barcode,

            /// <remarks/>
            [XmlEnumAttribute("paper-grow")]
            PaperGrow,

            /// <remarks/>
            [XmlEnumAttribute("punch-hole")]
            PunchHole,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            Other,
        }
    }
}
