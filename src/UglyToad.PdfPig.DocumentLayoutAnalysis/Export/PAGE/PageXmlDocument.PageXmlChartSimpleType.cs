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
        public enum PageXmlChartSimpleType
        {

            /// <remarks/>
            [XmlEnum("bar")]
            Bar,

            /// <remarks/>
            [XmlEnum("line")]
            Line,

            /// <remarks/>
            [XmlEnum("pie")]
            Pie,

            /// <remarks/>
            [XmlEnum("scatter")]
            Scatter,

            /// <remarks/>
            [XmlEnum("surface")]
            Surface,

            /// <remarks/>
            [XmlEnum("other")]
            Other,
        }
    }
}
