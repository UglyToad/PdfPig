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
        public enum PageXmlPageSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("front-cover")]
            FrontCover,

            /// <remarks/>
            [XmlEnumAttribute("back-cover")]
            BackCover,

            /// <remarks/>
            [XmlEnumAttribute("title")]
            Title,

            /// <remarks/>
            [XmlEnumAttribute("table-of-contents")]
            TableOfContents,

            /// <remarks/>
            [XmlEnumAttribute("index")]
            Index,

            /// <remarks/>
            [XmlEnumAttribute("content")]
            Content,

            /// <remarks/>
            [XmlEnumAttribute("blank")]
            Blank,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            Other,
        }
    }
}
