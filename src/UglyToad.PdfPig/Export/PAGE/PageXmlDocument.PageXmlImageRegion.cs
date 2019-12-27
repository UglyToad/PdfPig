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
        /// An image is considered to be more intricate and complex than a graphic. These can be photos or drawings.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlImageRegion : PageXmlRegion
        {
            #region private
            private float orientationField;

            private bool orientationFieldSpecified;

            private PageXmlColourDepthSimpleType colourDepthField;

            private bool colourDepthFieldSpecified;

            private PageXmlColourSimpleType bgColourField;

            private bool bgColourFieldSpecified;

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
            /// The colour bit depth required for the region
            /// </summary>
            [XmlAttributeAttribute("colourDepth")]
            public PageXmlColourDepthSimpleType ColourDepth
            {
                get
                {
                    return this.colourDepthField;
                }
                set
                {
                    this.colourDepthField = value;
                    this.colourDepthFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ColourDepthSpecified
            {
                get
                {
                    return this.colourDepthFieldSpecified;
                }
                set
                {
                    this.colourDepthFieldSpecified = value;
                }
            }

            /// <summary>
            /// The background colour of the region
            /// </summary>
            [XmlAttributeAttribute("bgColour")]
            public PageXmlColourSimpleType BgColour
            {
                get
                {
                    return this.bgColourField;
                }
                set
                {
                    this.bgColourField = value;
                    this.bgColourFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool BgColourSpecified
            {
                get
                {
                    return this.bgColourFieldSpecified;
                }
                set
                {
                    this.bgColourFieldSpecified = value;
                }
            }

            /// <summary>
            /// Specifies whether the region also contains text
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
