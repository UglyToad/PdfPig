namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.PAGE
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class PageXmlDocument
    {
        /// <summary>
        /// Specifies the unit of the resolution information referring to a standardised unit of measurement (pixels per inch, pixels per centimeter or other).
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCode("xsd", "4.6.1055.0")]
        [Serializable()]
        [XmlType(AnonymousType = true, Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlPageImageResolutionUnit
        {

            /// <remarks/>
            PPI,

            /// <remarks/>
            PPCM,

            /// <remarks/>
            [XmlEnum("other")]
            other,
        }
    }
}
