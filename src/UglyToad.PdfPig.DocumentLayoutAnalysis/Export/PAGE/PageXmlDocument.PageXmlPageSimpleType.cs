namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.PAGE
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class PageXmlDocument
    {
        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCode("xsd", "4.6.1055.0")]
        [Serializable()]
        [XmlType(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlPageSimpleType
        {

            /// <remarks/>
            [XmlEnum("front-cover")]
            FrontCover,

            /// <remarks/>
            [XmlEnum("back-cover")]
            BackCover,

            /// <remarks/>
            [XmlEnum("title")]
            Title,

            /// <remarks/>
            [XmlEnum("table-of-contents")]
            TableOfContents,

            /// <remarks/>
            [XmlEnum("index")]
            Index,

            /// <remarks/>
            [XmlEnum("content")]
            Content,

            /// <remarks/>
            [XmlEnum("blank")]
            Blank,

            /// <remarks/>
            [XmlEnum("other")]
            Other,
        }
    }
}
