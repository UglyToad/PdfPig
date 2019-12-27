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
        public enum PageXmlChartSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("bar")]
            Bar,

            /// <remarks/>
            [XmlEnumAttribute("line")]
            Line,

            /// <remarks/>
            [XmlEnumAttribute("pie")]
            Pie,

            /// <remarks/>
            [XmlEnumAttribute("scatter")]
            Scatter,

            /// <remarks/>
            [XmlEnumAttribute("surface")]
            Surface,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            Other,
        }
    }
}
