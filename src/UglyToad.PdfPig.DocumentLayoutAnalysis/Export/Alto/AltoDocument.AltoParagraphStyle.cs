namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] A paragraph style defines formatting properties of text blocks.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoParagraphStyle
        {
            private AltoParagraphStyleAlign align;
            private float left;
            private float right;
            private float linespace;
            private float firstLine;

            /// <remarks/>
            [XmlAttribute("ID", DataType = "ID")]
            public string Id { get; set; }

            /// <summary>
            /// Indicates the alignement of the paragraph. Could be left, right, center or justify.
            /// </summary>
            [XmlAttribute("ALIGN")]
            public AltoParagraphStyleAlign Align
            {
                get => align;
                set
                {
                    align = value;
                    AlignSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool AlignSpecified { get; private set; }

            /// <summary>
            /// Left indent of the paragraph in relation to the column.
            /// </summary>
            [XmlAttribute("LEFT")]
            public float Left
            {
                get => left;
                set
                {
                    left = value;
                    if (!float.IsNaN(value)) LeftSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool LeftSpecified { get; private set; }

            /// <summary>
            /// Right indent of the paragraph in relation to the column.
            /// </summary>
            [XmlAttribute("RIGHT")]
            public float Right
            {
                get => right;
                set
                {
                    right = value;
                    if (!float.IsNaN(value)) RightSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool RightSpecified { get; private set; }

            /// <summary>
            /// Line spacing between two lines of the paragraph. Measurement calculated from baseline to baseline.
            /// </summary>
            [XmlAttribute("LINESPACE")]
            public float LineSpace
            {
                get => linespace;
                set
                {
                    linespace = value;
                    if (!float.IsNaN(value)) LineSpaceSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool LineSpaceSpecified { get; private set; }

            /// <summary>
            /// Indent of the first line of the paragraph if this is different from the other lines. A negative 
            /// value indicates an indent to the left, a positive value indicates an indent to the right.
            /// </summary>
            [XmlAttribute("FIRSTLINE")]
            public float FirstLine
            {
                get => firstLine;
                set
                {
                    firstLine = value;
                    if (!float.IsNaN(value)) FirstLineSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool FirstLineSpecified { get; private set; }
        }
    }
}
