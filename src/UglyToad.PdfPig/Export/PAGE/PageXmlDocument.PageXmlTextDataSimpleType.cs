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
        public enum PageXmlTextDataSimpleType
        {

            /// <summary>
            /// Examples: "123.456", "+1234.456", "-1234.456", "-.456", "-456"
            /// </summary>
            [XmlEnumAttribute("xsd:double")]
            Xsddouble,

            /// <summary>
            /// Examples: "123.456", "+1234.456", "-1.2344e56", "-.45E-6", "INF", "-INF", "NaN"
            /// </summary>
            [XmlEnumAttribute("xsd:float")]
            XsdFloat,

            /// <summary>
            /// Examples: "123456", "+00000012", "-1", "-456"
            /// </summary>
            [XmlEnumAttribute("xsd:integer")]
            XsdInteger,

            /// <summary>
            /// Examples: "true", "false", "1", "0"
            /// </summary>
            [XmlEnumAttribute("xsd:boolean")]
            XsdBoolean,

            /// <summary>
            /// Examples: "2001-10-26", "2001-10-26+02:00", "2001-10-26Z", "2001-10-26+00:00", "-2001-10-26", "-20000-04-01"
            /// </summary>
            [XmlEnumAttribute("xsd:date")]
            XsdDate,

            /// <summary>
            /// Examples: "21:32:52", "21:32:52+02:00", "19:32:52Z", "19:32:52+00:00", "21:32:52.12679"
            /// </summary>
            [XmlEnumAttribute("xsd:time")]
            XsdTime,

            /// <summary>
            /// Examples: "2001-10-26T21:32:52", "2001-10-26T21:32:52+02:00", "2001-10-26T19:32:52Z", "2001-10-26T19:32:52+00:00","-2001-10-26T21:32:52", "2001-10-26T21:32:52.12679"
            /// </summary>
            [XmlEnumAttribute("xsd:dateTime")]
            XsdDateTime,

            /// <summary>
            /// Generic text string
            /// </summary>
            [XmlEnumAttribute("xsd:string")]
            XsdString,

            /// <summary>
            /// An XSD type that is not listed or a custom type (use dataTypeDetails attribute).
            /// </summary>
            [XmlEnumAttribute("other")]
            Other,
        }
    }
}
