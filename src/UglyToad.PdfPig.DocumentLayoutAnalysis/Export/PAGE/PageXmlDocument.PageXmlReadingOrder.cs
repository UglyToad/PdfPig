namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.PAGE
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
        [GeneratedCode("xsd", "4.6.1055.0")]
        [Serializable()]
        [DebuggerStepThrough()]
        [DesignerCategory("code")]
        [XmlType(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlReadingOrder
        {

            private object itemField;

            private float confField;

            private bool confFieldSpecified;

            /// <remarks/>
            [XmlElement("OrderedGroup", typeof(PageXmlOrderedGroup))]
            [XmlElement("UnorderedGroup", typeof(PageXmlUnorderedGroup))]
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
            [XmlAttribute("conf")]
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
            [XmlIgnore()]
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
