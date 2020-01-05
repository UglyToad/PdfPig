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
        public class PageXmlRegionRef
        {

            private string regionRefField;

            /// <remarks/>
            [XmlAttribute("regionRef", DataType = "IDREF")]
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
