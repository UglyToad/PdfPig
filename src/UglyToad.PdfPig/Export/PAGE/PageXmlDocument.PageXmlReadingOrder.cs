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
        public class PageXmlReadingOrder
        {

            private object itemField;

            private float confField;

            private bool confFieldSpecified;

            /// <remarks/>
            [XmlElementAttribute("OrderedGroup", typeof(PageXmlOrderedGroup))]
            [XmlElementAttribute("UnorderedGroup", typeof(PageXmlUnorderedGroup))]
            public object Item
            {
                get
                {
                    return this.itemField;
                }
                set
                {
                    this.itemField = value;
                }
            }

            /// <summary>
            /// Confidence value (between 0 and 1)
            /// </summary>
            [XmlAttributeAttribute("conf")]
            public float Conf
            {
                get
                {
                    return this.confField;
                }
                set
                {
                    this.confField = value;
                    this.confFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ConfSpecified
            {
                get
                {
                    return this.confFieldSpecified;
                }
                set
                {
                    this.confFieldSpecified = value;
                }
            }
        }
    }
}
