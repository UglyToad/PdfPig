namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] A hyphenation char. Can appear only at the end of a line.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(AnonymousType = true, Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoTextBlockTextLineHyp : AltoPositionedElement
        {
            /// <summary>
            /// Content.
            /// </summary>
            [XmlAttribute("CONTENT")]
            public string Content { get; set; }
        }
    }
}
