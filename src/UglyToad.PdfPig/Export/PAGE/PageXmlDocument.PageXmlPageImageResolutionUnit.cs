namespace UglyToad.PdfPig.Export.PAGE
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
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(AnonymousType = true, Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlPageImageResolutionUnit
        {

            /// <remarks/>
            PPI,

            /// <remarks/>
            PPCM,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            other,
        }
    }
}
