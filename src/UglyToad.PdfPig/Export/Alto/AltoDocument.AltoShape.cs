namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Describes the bounding shape of a block, if it is not rectangular.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoShape
        {
            /// <remarks/>
            [XmlElement("Circle", typeof(AltoCircle))]
            [XmlElement("Ellipse", typeof(AltoEllipse))]
            [XmlElement("Polygon", typeof(AltoPolygon))]
            public object Item { get; set; }
        }
    }
}
