namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] A circle shape. <see cref="HorizontalPosition"/> and <see cref="VerticalPosition"/> describe the center of the circle.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoCircle
        {
            /// <remarks/>
            [XmlAttribute("HPOS")]
            public float HorizontalPosition { get; set; }

            /// <remarks/>
            [XmlAttribute("VPOS")]
            public float VerticalPosition { get; set; }

            /// <remarks/>
            [XmlAttribute("RADIUS")]
            public float Radius { get; set; }
        }
    }
}
