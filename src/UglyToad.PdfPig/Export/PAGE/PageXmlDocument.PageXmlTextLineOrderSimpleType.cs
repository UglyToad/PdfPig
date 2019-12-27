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
        public enum PageXmlTextLineOrderSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("top-to-bottom")]
            TopToBottom,

            /// <remarks/>
            [XmlEnumAttribute("bottom-to-top")]
            BottomToTop,

            /// <remarks/>
            [XmlEnumAttribute("left-to-right")]
            LeftToRight,

            /// <remarks/>
            [XmlEnumAttribute("right-to-left")]
            RightToLeft,
        }
    }
}
