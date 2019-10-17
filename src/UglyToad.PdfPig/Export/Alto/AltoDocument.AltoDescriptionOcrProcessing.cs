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
        /// [Alto] Description Ocr Processing
        /// <para>Element deprecated. 'AltoProcessing' should be used instead.</para>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(AnonymousType = true, Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoDescriptionOcrProcessing : AltoOcrProcessing
        {
            /// <remarks/>
            [XmlAttribute(DataType = "ID")]
            public string Id { get; set; }
        }
    }
}
