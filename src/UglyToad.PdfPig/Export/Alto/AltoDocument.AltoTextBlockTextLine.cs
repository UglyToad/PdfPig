namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] A single line of text.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(AnonymousType = true, Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoTextBlockTextLine : AltoPositionedElement
        {
            private float baseline;
            private bool correctionStatus;

            /// <remarks/>
            public AltoShape Shape { get; set; }

            /// <remarks/>
            [XmlElement("String")]
            public AltoString[] Strings { get; set; }

            /// <remarks/>
            [XmlElement("SP")]
            public AltoSP[] Sp { get; set; }

            /// <summary>
            /// A hyphenation char. Can appear only at the end of a line.
            /// </summary>
            [XmlElement("HYP")]
            public AltoTextBlockTextLineHyp Hyp { get; set; }

            /// <remarks/>
            [XmlAttribute("ID", DataType = "ID")]
            public string Id { get; set; }

            /// <remarks/>
            [XmlAttribute("STYLEREFS", DataType = "IDREFS")]
            public string StyleRefs { get; set; }

            /// <remarks/>
            [XmlAttribute("TAGREFS", DataType = "IDREFS")]
            public string TagRefs { get; set; }

            /// <remarks/>
            [XmlAttribute("PROCESSINGREFS", DataType = "IDREFS")]
            public string ProcessingRefs { get; set; }

            /// <remarks/>
            [XmlAttribute("BASELINE")]
            public float BaseLine
            {
                get => baseline;
                set
                {
                    baseline = value;
                    if (!float.IsNaN(value)) BaseLineSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool BaseLineSpecified { get; set; }

            /// <remarks/>
            [XmlAttribute("LANG", DataType = "language")]
            public string Language { get; set; }

            /// <summary>
            /// Correction Status. Indicates whether manual correction has been done or not. 
            /// The correction status should be recorded at the highest level possible (Block, TextLine, String).
            /// </summary>
            [XmlAttribute("CS")]
            public bool CorrectionStatus
            {
                get => correctionStatus;
                set
                {
                    correctionStatus = value;
                    CorrectionStatusSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool CorrectionStatusSpecified { get; set; }

            /// <remarks/>
            public override string ToString()
            {
                return string.Join<AltoString>(" ", Strings); // take in account order?
            }
        }
    }
}
