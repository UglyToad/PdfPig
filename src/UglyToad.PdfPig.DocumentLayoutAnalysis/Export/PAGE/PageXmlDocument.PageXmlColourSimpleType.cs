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
        public enum PageXmlColourSimpleType
        {

            /// <remarks/>
            [XmlEnum("black")]
            Black,

            /// <remarks/>
            [XmlEnum("blue")]
            Blue,

            /// <remarks/>
            [XmlEnum("brown")]
            Brown,

            /// <remarks/>
            [XmlEnum("cyan")]
            Cyan,

            /// <remarks/>
            [XmlEnum("green")]
            Green,

            /// <remarks/>
            [XmlEnum("grey")]
            Grey,

            /// <remarks/>
            [XmlEnum("indigo")]
            Indigo,

            /// <remarks/>
            [XmlEnum("magenta")]
            Magenta,

            /// <remarks/>
            [XmlEnum("orange")]
            Orange,

            /// <remarks/>
            [XmlEnum("pink")]
            Pink,

            /// <remarks/>
            [XmlEnum("red")]
            Red,

            /// <remarks/>
            [XmlEnum("turquoise")]
            Turquoise,

            /// <remarks/>
            [XmlEnum("violet")]
            Violet,

            /// <remarks/>
            [XmlEnum("white")]
            White,

            /// <remarks/>
            [XmlEnum("yellow")]
            Yellow,

            /// <remarks/>
            [XmlEnum("other")]
            Other,
        }
    }
}
