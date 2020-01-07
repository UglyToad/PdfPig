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
        public class PageXmlLayer
        {

            private PageXmlRegionRef[] regionRefField;

            private string idField;

            private int zIndexField;

            private string captionField;

            /// <remarks/>
            [XmlElement("RegionRef")]
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
            [XmlAttribute("id", DataType = "ID")]
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
            [XmlAttribute("zIndex")]
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
            [XmlAttribute("caption")]
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
