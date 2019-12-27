namespace UglyToad.PdfPig.Export.PAGE
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class PageXmlDocument
    {
        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlCoords
        {

            private string pointsField;

            private float confField;

            private bool confFieldSpecified;

            /// <summary>
            /// Polygon outline of the element as a path of points.
            /// No points may lie outside the outline of its parent,
            /// which in the case of Border is the bounding rectangle
            /// of the root image. Paths are closed by convention,
            /// i.e.the last point logically connects with the first
            /// (and at least 3 points are required to span an area).
            /// Paths must be planar (i.e.must not self-intersect).
            /// </summary>
            [XmlAttributeAttribute("points")]
            public string Points
            {
                get
                {
                    return this.pointsField;
                }
                set
                {
                    this.pointsField = value;
                }
            }

            /// <summary>
            /// Confidence value (between 0 and 1)
            /// </summary>
            [XmlAttributeAttribute("conf")]
            public float Conf
            {
                get
                {
                    return this.confField;
                }
                set
                {
                    this.confField = value;
                    this.confFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ConfSpecified
            {
                get
                {
                    return this.confFieldSpecified;
                }
                set
                {
                    this.confFieldSpecified = value;
                }
            }
        }
    }
}
