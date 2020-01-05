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
        public enum PageXmlColourDepthSimpleType
        {

            /// <remarks/>
            [XmlEnum("bilevel")]
            BiLevel,

            /// <remarks/>
            [XmlEnum("greyscale")]
            GreyScale,

            /// <remarks/>
            [XmlEnum("colour")]
            Colour,

            /// <remarks/>
            [XmlEnum("other")]
            Other,
        }
    }
}
