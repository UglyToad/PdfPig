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
        [XmlType(AnonymousType = true, Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlUserAttributeType
        {

            /// <remarks/>
            [XmlEnum("xsd:string")]
            XsdString,

            /// <remarks/>
            [XmlEnum("xsd:integer")]
            XsdInteger,

            /// <remarks/>
            [XmlEnum("xsd:boolean")]
            XsdBoolean,

            /// <remarks/>
            [XmlEnum("xsd:float")]
            XsdFloat,
        }
    }
}
