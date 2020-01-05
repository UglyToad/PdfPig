namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] A white space.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        // ReSharper disable once InconsistentNaming
        public class AltoSP : AltoPositionedElement
        {
            /// <remarks/>
            [XmlAttribute("ID", DataType = "ID")]
            public string Id { get; set; }
        }
    }
}
