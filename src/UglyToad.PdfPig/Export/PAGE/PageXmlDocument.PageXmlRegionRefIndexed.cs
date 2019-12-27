namespace UglyToad.PdfPig.Export.PAGE
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class PageXmlDocument
    {
        /// <summary>
        /// Numbered region
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlRegionRefIndexed
        {

            private int indexField;

            private string regionRefField;

            /// <summary>
            /// Position (order number) of this item within the current hierarchy level.
            /// </summary>
            [XmlAttributeAttribute("index")]
            public int Index
            {
                get
                {
                    return this.indexField;
                }
                set
                {
                    this.indexField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("regionRef", DataType = "IDREF")]
            public string RegionRef
            {
                get
                {
                    return this.regionRefField;
                }
                set
                {
                    this.regionRefField = value;
                }
            }
        }
    }
}
