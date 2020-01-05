namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Ocr Processing
        /// <para>Element deprecated. 'AltoProcessing' should be used instead.</para>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoOcrProcessing
        {
            /// <remarks/>
            [XmlElement("preProcessingStep")]
            public AltoProcessingStep[] PreProcessingStep { get; set; }

            /// <remarks/>
            public AltoProcessingStep OcrProcessingStep { get; set; }

            /// <remarks/>
            [XmlElement("postProcessingStep")]
            public AltoProcessingStep[] PostProcessingStep { get; set; }
        }
    }
}
