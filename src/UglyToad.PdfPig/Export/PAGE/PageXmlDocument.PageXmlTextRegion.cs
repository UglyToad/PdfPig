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
        /// Pure text is represented as a text region. This includes drop capitals, but practically 
        /// ornate text may be considered as a graphic.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlTextRegion : PageXmlRegion
        {
            #region private methods
            private PageXmlTextLine[] textLineField;

            private PageXmlTextEquiv[] textEquivField;

            private PageXmlTextStyle textStyleField;

            private float orientationField;

            private bool orientationFieldSpecified;

            private PageXmlTextSimpleType typeField;

            private bool typeFieldSpecified;

            private int leadingField;

            private bool leadingFieldSpecified;

            private PageXmlReadingDirectionSimpleType readingDirectionField;

            private bool readingDirectionFieldSpecified;

            private PageXmlTextLineOrderSimpleType textLineOrderField;

            private bool textLineOrderFieldSpecified;

            private float readingOrientationField;

            private bool readingOrientationFieldSpecified;

            private bool indentedField;

            private bool indentedFieldSpecified;

            private PageXmlAlignSimpleType alignField;

            private bool alignFieldSpecified;

            private PageXmlLanguageSimpleType primaryLanguageField;

            private bool primaryLanguageFieldSpecified;

            private PageXmlLanguageSimpleType secondaryLanguageField;

            private bool secondaryLanguageFieldSpecified;

            private PageXmlScriptSimpleType primaryScriptField;

            private bool primaryScriptFieldSpecified;

            private PageXmlScriptSimpleType secondaryScriptField;

            private bool secondaryScriptFieldSpecified;

            private PageXmlProductionSimpleType productionField;

            private bool productionFieldSpecified;
            #endregion

            /// <remarks/>
            [XmlElementAttribute("TextLine")]
            public PageXmlTextLine[] TextLines
            {
                get
                {
                    return this.textLineField;
                }
                set
                {
                    this.textLineField = value;
                }
            }

            /// <remarks/>
            [XmlElementAttribute("TextEquiv")]
            public PageXmlTextEquiv[] TextEquivs
            {
                get
                {
                    return this.textEquivField;
                }
                set
                {
                    this.textEquivField = value;
                }
            }

            /// <remarks/>
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

            /// <summary>
            /// The angle the rectangle encapsulating the region
            /// has to be rotated in clockwise direction
            /// in order to correct the present skew
            /// (negative values indicate anti-clockwise rotation).
            /// (The rotated image can be further referenced
            /// via “AlternativeImage”.)
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
            /// The nature of the text in the region
            /// </summary>
            [XmlAttributeAttribute("type")]
            public PageXmlTextSimpleType Type
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
            /// The degree of space in points between the lines of
            /// text(line spacing)
            /// </summary>
            [XmlAttributeAttribute("leading")]
            public int Leading
            {
                get
                {
                    return this.leadingField;
                }
                set
                {
                    this.leadingField = value;
                    this.leadingFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool LeadingSpecified
            {
                get
                {
                    return this.leadingFieldSpecified;
                }
                set
                {
                    this.leadingFieldSpecified = value;
                }
            }

            /// <summary>
            /// The direction in which text within lines
            /// should be read(order of words and characters),
            /// in addition to “textLineOrder”.
            /// </summary>
            [XmlAttributeAttribute("readingDirection")]
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
            [XmlIgnoreAttribute()]
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
            /// The order of text lines within the block,
            /// in addition to “readingDirection”.
            /// </summary>
            [XmlAttributeAttribute("textLineOrder")]
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
            [XmlIgnoreAttribute()]
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
            /// The angle the baseline of text within the region
            /// has to be rotated(relative to the rectangle
            /// encapsulating the region) in clockwise direction
            /// in order to correct the present skew,
            /// in addition to “orientation”
            /// (negative values indicate anti-clockwise rotation).
            /// Range: -179.999,180
            /// </summary>
            [XmlAttributeAttribute("readingOrientation")]
            public float ReadingOrientation
            {
                get
                {
                    return this.readingOrientationField;
                }
                set
                {
                    this.readingOrientationField = value;
                    this.readingOrientationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ReadingOrientationSpecified
            {
                get
                {
                    return this.readingOrientationFieldSpecified;
                }
                set
                {
                    this.readingOrientationFieldSpecified = value;
                }
            }

            /// <summary>
            /// Defines whether a region of text is indented or not
            /// </summary>
            [XmlAttributeAttribute("indented")]
            public bool Indented
            {
                get
                {
                    return this.indentedField;
                }
                set
                {
                    this.indentedField = value;
                    this.indentedFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool IndentedSpecified
            {
                get
                {
                    return this.indentedFieldSpecified;
                }
                set
                {
                    this.indentedFieldSpecified = value;
                }
            }

            /// <summary>
            /// Text align
            /// </summary>
            [XmlAttributeAttribute("align")]
            public PageXmlAlignSimpleType Align
            {
                get
                {
                    return this.alignField;
                }
                set
                {
                    this.alignField = value;
                    this.alignFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool AlignSpecified
            {
                get
                {
                    return this.alignFieldSpecified;
                }
                set
                {
                    this.alignFieldSpecified = value;
                }
            }

            /// <summary>
            /// The primary language used in the region
            /// </summary>
            [XmlAttributeAttribute("primaryLanguage")]
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
            [XmlIgnoreAttribute()]
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
            /// The secondary language used in the region
            /// </summary>
            [XmlAttributeAttribute("secondaryLanguage")]
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
            [XmlIgnoreAttribute()]
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
            /// The primary script used in the region
            /// </summary>
            [XmlAttributeAttribute("primaryScript")]
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
            [XmlIgnoreAttribute()]
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
            /// The secondary script used in the region
            /// </summary>
            [XmlAttributeAttribute("secondaryScript")]
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
            [XmlIgnoreAttribute()]
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

            /// <remarks/>
            [XmlAttributeAttribute("production")]
            public PageXmlProductionSimpleType Production
            {
                get
                {
                    return this.productionField;
                }
                set
                {
                    this.productionField = value;
                    this.productionFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ProductionSpecified
            {
                get
                {
                    return this.productionFieldSpecified;
                }
                set
                {
                    this.productionFieldSpecified = value;
                }
            }
        }
    }
}
