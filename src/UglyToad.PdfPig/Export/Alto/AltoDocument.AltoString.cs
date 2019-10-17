namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] A sequence of chars. Strings are separated by white spaces or hyphenation chars.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoString : AltoPositionedElement
        {
            private AltoFontStyles style;
            private AltoSubsType subsType;
            private float wc;
            private bool correctionStatus;

            /// <remarks/>
            public AltoShape Shape { get; set; }

            /// <remarks/>
            [XmlElement("ALTERNATIVE")]
            public AltoAlternative[] Alternative { get; set; }

            /// <remarks/>
            [XmlElement("Glyph")]
            public AltoGlyph[] Glyph { get; set; }

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
            [XmlAttribute("CONTENT")]
            public string Content { get; set; }

            /// <remarks/>
            [XmlAttribute("STYLE")]
            public AltoFontStyles Style
            {
                get => style;
                set
                {
                    style = value;
                    StyleSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool StyleSpecified { get; private set; }

            /// <remarks/>
            [XmlAttribute("SUBS_TYPE")]
            public AltoSubsType SubsType
            {
                get => subsType;
                set
                {
                    subsType = value;
                    SubsTypeSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool SubsTypeSpecified { get; private set; }

            /// <summary>
            /// Content of the substitution.
            /// </summary>
            [XmlAttribute("SUBS_CONTENT")]
            public string SubsContent { get; set; }

            /// <remarks/>
            [XmlAttribute("WC")]
            public float Wc
            {
                get => wc;
                set
                {
                    wc = value;
                    if (!float.IsNaN(value)) WcSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool WcSpecified { get; private set; }

            /// <summary>
            /// Confidence level of each character in that string. A list of numbers,
            /// one number between 0 (sure) and 9 (unsure) for each character.
            /// </summary>
            [XmlAttribute("CC")]
            public string Cc { get; set; }

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
            public bool CorrectionStatusSpecified { get; private set; }

            /// <summary>
            /// Attribute to record language of the string. The language should be recorded at the highest level possible.
            /// </summary>
            [XmlAttribute("LANG", DataType = "language")]
            public string Language { get; set; }

            /// <remarks/>
            public override string ToString()
            {
                return Content;
            }
        }
    }
}
