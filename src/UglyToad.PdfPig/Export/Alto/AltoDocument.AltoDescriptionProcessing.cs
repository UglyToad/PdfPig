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
        /// [Alto] Description Processing
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(AnonymousType = true, Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoDescriptionProcessing : AltoProcessingStep
        {
            /// <summary>
            /// Id.
            /// </summary>
            [XmlAttribute("ID", DataType = "ID")]
            public string Id { get; set; }
        }
    }
}
