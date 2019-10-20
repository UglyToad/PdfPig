namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Schema;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Base type for any kind of block on the page.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [XmlInclude(typeof(AltoTextBlock))]
        [XmlInclude(typeof(AltoGraphicalElement))]
        [XmlInclude(typeof(AltoIllustration))]
        [XmlInclude(typeof(AltoComposedBlock))]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoBlock : AltoPositionedElement
        {
            private float rotation;
            private bool correctionStatus;
            private AltoBlockTypeShow show;
            private AltoBlockTypeActuate actuate;
            
            /// <remarks/>
            public AltoShape Shape { get; set; }

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

            /// <summary>
            /// The rotation of e.g. text or illustration within the block. The value is in degree counterclockwise.
            /// </summary>
            [XmlAttribute("ROTATION")]
            public float Rotation
            {
                get => rotation;
                set
                {
                    rotation = value;
                    if (!float.IsNaN(value)) RotationSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool RotationSpecified { get; set; }

            /// <summary>
            /// The next block in reading sequence on the page.
            /// </summary>
            [XmlAttribute("IDNEXT", DataType = "IDREF")]
            public string IdNext { get; set; }

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
            [XmlAttribute("type", Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
            public string Type { get; set; }

            /// <remarks/>
            [XmlAttribute("href", Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink", DataType = "anyURI")]
            public string Href { get; set; }

            /// <remarks/>
            [XmlAttribute("role", Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
            public string Role { get; set; }

            /// <remarks/>
            [XmlAttribute("arcrole", Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
            public string Arcrole { get; set; }

            /// <remarks/>
            [XmlAttribute("title", Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
            public string Title { get; set; }

            /// <remarks/>
            [XmlAttribute("show", Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
            public AltoBlockTypeShow Show
            {
                get => show;
                set
                {
                    show = value;
                    ShowSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool ShowSpecified { get; set; }

            /// <remarks/>
            [XmlAttribute("actuate", Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
            public AltoBlockTypeActuate Actuate
            {
                get => actuate;
                set
                {
                    actuate = value;
                    ActuateSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool ActuateSpecified { get; set; }
        }
    }
}
