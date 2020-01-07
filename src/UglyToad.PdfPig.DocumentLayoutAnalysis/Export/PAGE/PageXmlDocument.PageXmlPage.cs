namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.PAGE
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
        [GeneratedCode("xsd", "4.6.1055.0")]
        [Serializable()]
        [DebuggerStepThrough()]
        [DesignerCategory("code")]
        [XmlType(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlPage
        {

            private PageXmlAlternativeImage[] alternativeImageField;

            private PageXmlBorder borderField;

            private PageXmlPrintSpace printSpaceField;

            private PageXmlReadingOrder readingOrderField;

            private PageXmlLayers layersField;

            private PageXmlRelations relationsField;

            private PageXmlTextStyle textStyleField;

            private PageXmlUserAttribute[] userDefinedField;

            private PageXmlLabels[] labelsField;

            private PageXmlRegion[] itemsField;

            private string imageFilenameField;

            private int imageWidthField;

            private int imageHeightField;

            private float imageXResolutionField;

            private bool imageXResolutionFieldSpecified;

            private float imageYResolutionField;

            private bool imageYResolutionFieldSpecified;

            private PageXmlPageImageResolutionUnit imageResolutionUnitField;

            private bool imageResolutionUnitFieldSpecified;

            private string customField;

            private float orientationField;

            private bool orientationFieldSpecified;

            private PageXmlPageSimpleType typeField;

            private bool typeFieldSpecified;

            private PageXmlLanguageSimpleType primaryLanguageField;

            private bool primaryLanguageFieldSpecified;

            private PageXmlLanguageSimpleType secondaryLanguageField;

            private bool secondaryLanguageFieldSpecified;

            private PageXmlScriptSimpleType primaryScriptField;

            private bool primaryScriptFieldSpecified;

            private PageXmlScriptSimpleType secondaryScriptField;

            private bool secondaryScriptFieldSpecified;

            private PageXmlReadingDirectionSimpleType readingDirectionField;

            private bool readingDirectionFieldSpecified;

            private PageXmlTextLineOrderSimpleType textLineOrderField;

            private bool textLineOrderFieldSpecified;

            private float confField;

            private bool confFieldSpecified;

            /// <summary>
            /// Alternative document page images (e.g.black-and-white).
            /// </summary>
            [XmlElement("AlternativeImage")]
            public PageXmlAlternativeImage[] AlternativeImage
            {
                get
                {
                    return this.alternativeImageField;
                }
                set
                {
                    this.alternativeImageField = value;
                }
            }

            /// <remarks/>
            public PageXmlBorder Border
            {
                get
                {
                    return this.borderField;
                }
                set
                {
                    this.borderField = value;
                }
            }

            /// <remarks/>
            public PageXmlPrintSpace PrintSpace
            {
                get
                {
                    return this.printSpaceField;
                }
                set
                {
                    this.printSpaceField = value;
                }
            }

            /// <summary>
            /// Order of blocks within the page.
            /// </summary>
            public PageXmlReadingOrder ReadingOrder
            {
                get
                {
                    return this.readingOrderField;
                }
                set
                {
                    this.readingOrderField = value;
                }
            }

            /// <summary>
            /// Unassigned regions are considered to be in the (virtual) default layer which is to be treated as below any other layers.
            /// </summary>
            public PageXmlLayers Layers
            {
                get
                {
                    return this.layersField;
                }
                set
                {
                    this.layersField = value;
                }
            }

            /// <remarks/>
            public PageXmlRelations Relations
            {
                get
                {
                    return this.relationsField;
                }
                set
                {
                    this.relationsField = value;
                }
            }

            /// <summary>
            /// Default text style
            /// </summary>
            public PageXmlTextStyle TextStyle
            {
                get
                {
                    return this.textStyleField;
                }
                set
                {
                    this.textStyleField = value;
                }
            }

            /// <remarks/>
            [XmlArrayItem("UserAttribute", IsNullable = false)]
            public PageXmlUserAttribute[] UserDefined
            {
                get
                {
                    return this.userDefinedField;
                }
                set
                {
                    this.userDefinedField = value;
                }
            }

            /// <summary>
            /// Semantic labels / tags
            /// </summary>
            [XmlElement("Labels")]
            public PageXmlLabels[] Labels
            {
                get
                {
                    return this.labelsField;
                }
                set
                {
                    this.labelsField = value;
                }
            }

            /// <remarks/>
            [XmlElement("AdvertRegion", typeof(PageXmlAdvertRegion))]
            [XmlElement("ChartRegion", typeof(PageXmlChartRegion))]
            [XmlElement("ChemRegion", typeof(PageXmlChemRegion))]
            [XmlElement("CustomRegion", typeof(PageXmlCustomRegion))]
            [XmlElement("GraphicRegion", typeof(PageXmlGraphicRegion))]
            [XmlElement("ImageRegion", typeof(PageXmlImageRegion))]
            [XmlElement("LineDrawingRegion", typeof(PageXmlLineDrawingRegion))]
            [XmlElement("MapRegion", typeof(PageXmlMapRegion))]
            [XmlElement("MathsRegion", typeof(PageXmlMathsRegion))]
            [XmlElement("MusicRegion", typeof(PageXmlMusicRegion))]
            [XmlElement("NoiseRegion", typeof(PageXmlNoiseRegion))]
            [XmlElement("SeparatorRegion", typeof(PageXmlSeparatorRegion))]
            [XmlElement("TableRegion", typeof(PageXmlTableRegion))]
            [XmlElement("TextRegion", typeof(PageXmlTextRegion))]
            [XmlElement("UnknownRegion", typeof(PageXmlUnknownRegion))]
            public PageXmlRegion[] Items
            {
                get
                {
                    return this.itemsField;
                }
                set
                {
                    this.itemsField = value;
                }
            }

            /// <summary>
            /// Contains the image file name including the file extension.
            /// </summary>
            [XmlAttribute("imageFilename")]
            public string ImageFilename
            {
                get
                {
                    return this.imageFilenameField;
                }
                set
                {
                    this.imageFilenameField = value;
                }
            }

            /// <summary>
            /// Specifies the width of the image.
            /// </summary>
            [XmlAttribute("imageWidth")]
            public int ImageWidth
            {
                get
                {
                    return this.imageWidthField;
                }
                set
                {
                    this.imageWidthField = value;
                }
            }

            /// <summary>
            /// Specifies the height of the image.
            /// </summary>
            [XmlAttribute("imageHeight")]
            public int ImageHeight
            {
                get
                {
                    return this.imageHeightField;
                }
                set
                {
                    this.imageHeightField = value;
                }
            }

            /// <summary>
            /// Specifies the image resolution in width.
            /// </summary>
            [XmlAttribute("imageXResolution")]
            public float ImageXResolution
            {
                get
                {
                    return this.imageXResolutionField;
                }
                set
                {
                    this.imageXResolutionField = value;
                    this.imageXResolutionFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool ImageXResolutionSpecified
            {
                get
                {
                    return this.imageXResolutionFieldSpecified;
                }
                set
                {
                    this.imageXResolutionFieldSpecified = value;
                }
            }

            /// <summary>
            /// Specifies the image resolution in height.
            /// </summary>
            [XmlAttribute("imageYResolution")]
            public float ImageYResolution
            {
                get
                {
                    return this.imageYResolutionField;
                }
                set
                {
                    this.imageYResolutionField = value;
                    this.imageYResolutionFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool ImageYResolutionSpecified
            {
                get
                {
                    return this.imageYResolutionFieldSpecified;
                }
                set
                {
                    this.imageYResolutionFieldSpecified = value;
                }
            }

            /// <summary>
            /// Specifies the unit of the resolution information referring to a standardised unit of measurement 
            /// (pixels per inch, pixels per centimeter or other).
            /// </summary>
            [XmlAttribute("imageResolutionUnit")]
            public PageXmlPageImageResolutionUnit ImageResolutionUnit
            {
                get
                {
                    return this.imageResolutionUnitField;
                }
                set
                {
                    this.imageResolutionUnitField = value;
                    this.imageResolutionUnitFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool ImageResolutionUnitSpecified
            {
                get
                {
                    return this.imageResolutionUnitFieldSpecified;
                }
                set
                {
                    this.imageResolutionUnitFieldSpecified = value;
                }
            }

            /// <summary>
            /// For generic use
            /// </summary>
            [XmlAttribute("custom")]
            public string Custom
            {
                get
                {
                    return this.customField;
                }
                set
                {
                    this.customField = value;
                }
            }

            /// <summary>
            /// The angle the rectangle encapsulating the page (or its Border) has to be rotated in clockwise direction
            /// in order to correct the present skew (negative values indicate anti-clockwise rotation).
            /// (The rotated image can be further referenced via “AlternativeImage”.)
            /// <para>Range: -179.999, 180</para>
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
            /// The type of the page within the document
            /// (e.g.cover page).
            /// </summary>
            [XmlAttribute("type")]
            public PageXmlPageSimpleType Type
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
            [XmlIgnore()]
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
            /// The primary language used in the page (lower-level definitions override the page-level definition).
            /// </summary>
            [XmlAttribute("primaryLanguage")]
            public PageXmlLanguageSimpleType PrimaryLanguage
            {
                get
                {
                    return this.primaryLanguageField;
                }
                set
                {
                    this.primaryLanguageField = value;
                    this.primaryLanguageFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool PrimaryLanguageSpecified
            {
                get
                {
                    return this.primaryLanguageFieldSpecified;
                }
                set
                {
                    this.primaryLanguageFieldSpecified = value;
                }
            }

            /// <summary>
            /// The secondary language used in the page (lower-level definitions override the page-level definition).
            /// </summary>
            [XmlAttribute("secondaryLanguage")]
            public PageXmlLanguageSimpleType SecondaryLanguage
            {
                get
                {
                    return this.secondaryLanguageField;
                }
                set
                {
                    this.secondaryLanguageField = value;
                    this.secondaryLanguageFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool SecondaryLanguageSpecified
            {
                get
                {
                    return this.secondaryLanguageFieldSpecified;
                }
                set
                {
                    this.secondaryLanguageFieldSpecified = value;
                }
            }

            /// <summary>
            /// The primary script used in the page (lower-level definitions override the page-level definition).
            /// </summary>
            [XmlAttribute("primaryScript")]
            public PageXmlScriptSimpleType PrimaryScript
            {
                get
                {
                    return this.primaryScriptField;
                }
                set
                {
                    this.primaryScriptField = value;
                    this.primaryScriptFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool PrimaryScriptSpecified
            {
                get
                {
                    return this.primaryScriptFieldSpecified;
                }
                set
                {
                    this.primaryScriptFieldSpecified = value;
                }
            }

            /// <summary>
            /// The secondary script used in the page (lower-level definitions override the page-level definition).
            /// </summary>
            [XmlAttribute("secondaryScript")]
            public PageXmlScriptSimpleType SecondaryScript
            {
                get
                {
                    return this.secondaryScriptField;
                }
                set
                {
                    this.secondaryScriptField = value;
                    this.secondaryScriptFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool SecondaryScriptSpecified
            {
                get
                {
                    return this.secondaryScriptFieldSpecified;
                }
                set
                {
                    this.secondaryScriptFieldSpecified = value;
                }
            }

            /// <summary>
            /// The direction in which text within lines should be read(order of words and characters), 
            /// in addition to “textLineOrder” (lower-level definitions override the page-level definition).    
            /// </summary>
            [XmlAttribute("readingDirection")]
            public PageXmlReadingDirectionSimpleType ReadingDirection
            {
                get
                {
                    return this.readingDirectionField;
                }
                set
                {
                    this.readingDirectionField = value;
                    this.readingDirectionFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool ReadingDirectionSpecified
            {
                get
                {
                    return this.readingDirectionFieldSpecified;
                }
                set
                {
                    this.readingDirectionFieldSpecified = value;
                }
            }

            /// <summary>
            /// The order of text lines within a block, in addition to “readingDirection” 
            /// (lower-level definitions override the page-level definition).
            /// </summary>
            [XmlAttribute("textLineOrder")]
            public PageXmlTextLineOrderSimpleType TextLineOrder
            {
                get
                {
                    return this.textLineOrderField;
                }
                set
                {
                    this.textLineOrderField = value;
                    this.textLineOrderFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool TextLineOrderSpecified
            {
                get
                {
                    return this.textLineOrderFieldSpecified;
                }
                set
                {
                    this.textLineOrderFieldSpecified = value;
                }
            }

            /// <summary>
            /// Confidence value for whole page (between 0 and 1)
            /// </summary>
            [XmlAttribute("conf")]
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
            [XmlIgnore()]
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
