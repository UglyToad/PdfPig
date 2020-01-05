namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Description of the processing step.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoProcessingStep
        {
            private AltoProcessingCategory processingCategory;
            
            /// <summary>
            /// Classification of the category of operation, how the file was created, including 
            /// generation, modification, preprocessing, postprocessing or any other steps.
            /// </summary>
            [XmlAttribute("processingCategory")]
            public AltoProcessingCategory ProcessingCategory
            {
                get => processingCategory;
                set
                {
                    processingCategory = value;
                    ProcessingCategorySpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool ProcessingCategorySpecified { get; set; }

            /// <summary>
            /// Date or DateTime the image was processed.
            /// </summary>
            [XmlAttribute("processingDateTime")]
            public string ProcessingDateTime { get; set; }

            /// <summary>
            /// Identifies the organization level producer(s) of the processed image.
            /// </summary>
            [XmlAttribute("processingAgency")]
            public string ProcessingAgency { get; set; }

            /// <summary>
            /// An ordinal listing of the image processing steps performed. For example, "image despeckling."
            /// </summary>
            [XmlElement("processingStepDescription")]
            public string[] ProcessingStepDescription { get; set; }

            /// <summary>
            /// A description of any setting of the processing application. For example, for a multi-engine
            /// OCR application this might include the engines which were used. Ideally, this description 
            /// should be adequate so that someone else using the same application can produce identical results.
            /// </summary>
            [XmlAttribute("processingStepSettings")]
            public string ProcessingStepSettings { get; set; }

            /// <remarks/>
            [XmlElement("processingSoftware")]
            public AltoProcessingSoftware ProcessingSoftware { get; set; }
        }
    }
}
