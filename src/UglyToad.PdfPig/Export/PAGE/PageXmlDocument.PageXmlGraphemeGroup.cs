namespace UglyToad.PdfPig.Export.PAGE
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class PageXmlDocument
    {
        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlGraphemeGroup : PageXmlGraphemeBase
        {

            private PageXmlGraphemeBase[] itemsField;

            /// <remarks/>
            [XmlElementAttribute("Grapheme", typeof(PageXmlGrapheme))]
            [XmlElementAttribute("NonPrintingChar", typeof(PageXmlNonPrintingChar))]
            public PageXmlGraphemeBase[] Items
            {
                get
                {
                    return this.itemsField;
                }
                set
                {
                    this.itemsField = value;
                }
            }
        }
    }
}
