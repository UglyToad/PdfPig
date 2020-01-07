namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.PAGE
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class PageXmlDocument
    {
        /// <summary>
        /// Can be used to express the z-index of overlapping
        /// regions.An element with a greater z-index is always in
        /// front of another element with lower z-index.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCode("xsd", "4.6.1055.0")]
        [Serializable()]
        [DebuggerStepThrough()]
        [DesignerCategory("code")]
        [XmlType(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlLayers
        {

            private PageXmlLayer[] layerField;

            /// <remarks/>
            [XmlElement("Layer")]
            public PageXmlLayer[] Layers
            {
                get
                {
                    return this.layerField;
                }
                set
                {
                    this.layerField = value;
                }
            }
        }
    }
}
