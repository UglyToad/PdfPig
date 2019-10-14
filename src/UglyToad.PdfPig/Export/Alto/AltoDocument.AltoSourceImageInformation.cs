namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Information to identify the image file from which the OCR text was created.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoSourceImageInformation
        {
            /// <remarks/>
            [XmlElement("fileName")]
            public string FileName { get; set; }

            /// <remarks/>
            [XmlElement("fileIdentifier")]
            public AltoFileIdentifier[] FileIdentifiers { get; set; }

            /// <remarks/>
            [XmlElement("documentIdentifier")]
            public AltoDocumentIdentifier[] DocumentIdentifiers { get; set; }
        }
    }
}
