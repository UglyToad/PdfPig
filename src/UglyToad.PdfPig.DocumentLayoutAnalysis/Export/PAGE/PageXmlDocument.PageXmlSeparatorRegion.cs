namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.PAGE
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class PageXmlDocument
    {
        /// <summary>
        /// Separators are lines that lie between columns and 
        /// paragraphs and can be used to logically separate
        /// different articles from each other.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCode("xsd", "4.6.1055.0")]
        [Serializable()]
        [DebuggerStepThrough()]
        [DesignerCategory("code")]
        [XmlType(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlSeparatorRegion : PageXmlRegion
        {

            private float orientationField;

            private bool orientationFieldSpecified;

            private PageXmlColourSimpleType colourField;

            private bool colourFieldSpecified;

            /// <summary>
            /// The angle the rectangle encapsulating a region
            /// has to be rotated in clockwise direction
            /// in order to correct the present skew
            /// (negative values indicate anti-clockwise rotation).
            /// Range: -179.999,180
            /// </summary>
            [XmlAttribute("orientation")]
            public float Orientation
            {
                get
                {
                    return this.orientationField;
                }
                set
                {
                    this.orientationField = value;
                    this.orientationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool OrientationSpecified
            {
                get
                {
                    return this.orientationFieldSpecified;
                }
                set
                {
                    this.orientationFieldSpecified = value;
                }
            }

            /// <summary>
            /// The colour of the separator
            /// </summary>
            [XmlAttribute("colour")]
            public PageXmlColourSimpleType Colour
            {
                get
                {
                    return this.colourField;
                }
                set
                {
                    this.colourField = value;
                    this.colourFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool ColourSpecified
            {
                get
                {
                    return this.colourFieldSpecified;
                }
                set
                {
                    this.colourFieldSpecified = value;
                }
            }
        }
    }
}
