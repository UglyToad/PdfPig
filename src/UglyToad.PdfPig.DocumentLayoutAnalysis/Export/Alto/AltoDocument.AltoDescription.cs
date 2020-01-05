namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Description
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoDescription
        {
            /// <remarks/>
            public AltoMeasurementUnit MeasurementUnit { get; set; }

            /// <remarks/>
            [XmlElement("sourceImageInformation")]
            public AltoSourceImageInformation SourceImageInformation { get; set; }

            /// <summary>
            /// Element deprecated. 'Processing' should be used instead.
            /// </summary>
            [XmlElement("OCRProcessing")]
            public AltoDescriptionOcrProcessing[] OcrProcessing { get; set; }

            /// <remarks/>
            [XmlElement("Processing")]
            public AltoDescriptionProcessing[] Processings { get; set; }
        }
    }
}
