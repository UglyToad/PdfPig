namespace UglyToad.PdfPig.Export.PAGE
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class PageXmlDocument
    {
        /// <summary>
        /// Regions containing simple graphics, such as a company
        /// logo, should be marked as graphic regions.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlGraphicRegion : PageXmlRegion
        {
            #region private
            private float orientationField;

            private bool orientationFieldSpecified;

            private PageXmlGraphicsSimpleType typeField;

            private bool typeFieldSpecified;

            private int numColoursField;

            private bool numColoursFieldSpecified;

            private bool embTextField;

            private bool embTextFieldSpecified;
            #endregion

            /// <summary>
            /// The angle the rectangle encapsulating a region
            /// has to be rotated in clockwise direction
            /// in order to correct the present skew
            /// (negative values indicate anti-clockwise rotation).
            /// Range: -179.999,180
            /// </summary>
            [XmlAttributeAttribute("orientation")]
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
            [XmlIgnoreAttribute()]
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
            /// The type of graphic in the region
            /// </summary>
            [XmlAttributeAttribute("type")]
            public PageXmlGraphicsSimpleType Type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                    this.typeFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool TypeSpecified
            {
                get
                {
                    return this.typeFieldSpecified;
                }
                set
                {
                    this.typeFieldSpecified = value;
                }
            }

            /// <summary>
            /// An approximation of the number of colours used in the region
            /// </summary>
            [XmlAttributeAttribute("numColours")]
            public int NumColours
            {
                get
                {
                    return this.numColoursField;
                }
                set
                {
                    this.numColoursField = value;
                    this.numColoursFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool NumColoursSpecified
            {
                get
                {
                    return this.numColoursFieldSpecified;
                }
                set
                {
                    this.numColoursFieldSpecified = value;
                }
            }

            /// <summary>
            /// Specifies whether the region also contains text.
            /// </summary>
            [XmlAttributeAttribute("embText")]
            public bool EmbText
            {
                get
                {
                    return this.embTextField;
                }
                set
                {
                    this.embTextField = value;
                    this.embTextFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool EmbTextSpecified
            {
                get
                {
                    return this.embTextFieldSpecified;
                }
                set
                {
                    this.embTextFieldSpecified = value;
                }
            }
        }
    }
}
