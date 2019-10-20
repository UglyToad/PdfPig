namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] An ellipse shape. HPOS and VPOS describe the center of the ellipse.
        /// HLENGTH and VLENGTH are the width and height of the described ellipse.
        /// <para>The attribute ROTATION tells the rotation of the e.g. text or 
        /// illustration within the block.The value is in degrees counterclockwise.</para>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoEllipse
        {
            private float rotation;

            /// <remarks/>
            [XmlAttribute("HPOS")]
            public float HorizontalPosition { get; set; }

            /// <remarks/>
            [XmlAttribute("VPOS")]
            public float VerticalPosition { get; set; }

            /// <remarks/>
            [XmlAttribute("HLENGTH")]
            public float HorizontalLength { get; set; }

            /// <remarks/>
            [XmlAttribute("VLENGTH")]
            public float VerticalLength { get; set; }

            /// <remarks/>
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
        }
    }
}
