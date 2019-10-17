namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <inheritdoc />
        /// <summary>
        /// [Alto] A region on a page.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoPageSpace : AltoPositionedElement
        {
            /// <summary>
            /// Shape.
            /// </summary>
            public AltoShape Shape { get; set; }

            /// <remarks/>
            [XmlElement("TextBlock")]
            public AltoTextBlock[] TextBlock { get; set; }

            /// <remarks/>
            [XmlElement("Illustration")]
            public AltoIllustration[] Illustrations { get; set; }

            /// <remarks/>
            [XmlElement("GraphicalElement")]
            public AltoGraphicalElement[] GraphicalElements { get; set; }

            /// <remarks/>
            [XmlElement("ComposedBlock")]
            public AltoComposedBlock[] ComposedBlocks { get; set; }

            /// <remarks/>
            [XmlAttribute("ID", DataType = "ID")]
            public string Id { get; set; }

            /// <remarks/>
            [XmlAttribute("STYLEREFS", DataType = "IDREFS")]
            public string StyleRefs { get; set; }

            /// <remarks/>
            [XmlAttribute("PROCESSINGREFS", DataType = "IDREFS")]
            public string ProcessingRefs { get; set; }
        }
    }
}
