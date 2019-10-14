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
        /// [Alto] A block of text.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoTextBlock : AltoBlock
        {
            /// <remarks/>
            [XmlElement("TextLine")]
            public AltoTextBlockTextLine[] TextLines { get; set; }

            /// <summary>
            /// Attribute deprecated. LANG should be used instead.
            /// </summary>
            [XmlAttribute("language", DataType = "language")]
            public string Language { get; set; }

            /// <summary>
            /// Attribute to record language of the textblock.
            /// </summary>
            [XmlAttribute("LANG", DataType = "language")]
            public string Lang { get; set; }

            /// <remarks/>
            public override string ToString()
            {
                return string.Join<AltoTextBlockTextLine>(" ", TextLines);
            }
        }
    }
}
