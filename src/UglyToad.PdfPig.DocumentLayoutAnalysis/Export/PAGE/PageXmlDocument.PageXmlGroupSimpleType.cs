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
        public enum PageXmlGroupSimpleType
        {

            /// <remarks/>
            [XmlEnum("paragraph")]
            Paragraph,

            /// <remarks/>
            [XmlEnum("list")]
            List,

            /// <remarks/>
            [XmlEnum("list-item")]
            ListItem,

            /// <remarks/>
            [XmlEnum("figure")]
            Figure,

            /// <remarks/>
            [XmlEnum("article")]
            Article,

            /// <remarks/>
            [XmlEnum("div")]
            Div,

            /// <remarks/>
            [XmlEnum("other")]
            Other,
        }
    }
}
