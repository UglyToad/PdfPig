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
        public class PageXmlLayer
        {

            private PageXmlRegionRef[] regionRefField;

            private string idField;

            private int zIndexField;

            private string captionField;

            /// <remarks/>
            [XmlElementAttribute("RegionRef")]
            public PageXmlRegionRef[] RegionRefs
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

            /// <remarks/>
            [XmlAttributeAttribute("id", DataType = "ID")]
            public string Id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("zIndex")]
            public int ZIndex
            {
                get
                {
                    return this.zIndexField;
                }
                set
                {
                    this.zIndexField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("caption")]
            public string Caption
            {
                get
                {
                    return this.captionField;
                }
                set
                {
                    this.captionField = value;
                }
            }
        }
    }
}
