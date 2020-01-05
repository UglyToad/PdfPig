namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.PAGE
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
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
        public class PageXmlWord
        {
            #region private
            private PageXmlAlternativeImage[] alternativeImageField;

            private PageXmlCoords coordsField;

            private PageXmlGlyph[] glyphField;

            private PageXmlTextEquiv[] textEquivField;

            private PageXmlTextStyle textStyleField;

            private PageXmlUserAttribute[] userDefinedField;

            private PageXmlLabels[] labelsField;

            private string idField;

            private PageXmlLanguageSimpleType languageField;

            private bool languageFieldSpecified;

            private PageXmlScriptSimpleType primaryScriptField;

            private bool primaryScriptFieldSpecified;

            private PageXmlScriptSimpleType secondaryScriptField;

            private bool secondaryScriptFieldSpecified;

            private PageXmlReadingDirectionSimpleType readingDirectionField;

            private bool readingDirectionFieldSpecified;

            private PageXmlProductionSimpleType productionField;

            private bool productionFieldSpecified;

            private string customField;

            private string commentsField;
            #endregion

            /// <summary>
            /// Alternative word images (e.g. black-and-white)
            /// </summary>
            [XmlElement("AlternativeImage")]
            public PageXmlAlternativeImage[] AlternativeImages
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
            public PageXmlCoords Coords
            {
                get
                {
                    return this.coordsField;
                }
                set
                {
                    this.coordsField = value;
                }
            }

            /// <remarks/>
            [XmlElement("Glyph")]
            public PageXmlGlyph[] Glyphs
            {
                get
                {
                    return this.glyphField;
                }
                set
                {
                    this.glyphField = value;
                }
            }

            /// <remarks/>
            [XmlElement("TextEquiv")]
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
            [XmlAttribute("id", DataType = "ID")]
            public string Id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }

            /// <summary>
            /// Overrides primaryLanguage attribute of parent line and/or text region
            /// </summary>
            [XmlAttribute("language")]
            public PageXmlLanguageSimpleType Language
            {
                get
                {
                    return this.languageField;
                }
                set
                {
                    this.languageField = value;
                    this.languageFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool LanguageSpecified
            {
                get
                {
                    return this.languageFieldSpecified;
                }
                set
                {
                    this.languageFieldSpecified = value;
                }
            }

            /// <summary>
            /// The primary script used in the word
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
            /// The secondary script used in the word 
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
            /// The direction in which text within the word should be read(order of characters).
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
            /// Overrides the production attribute of the parent text line and/or text region.
            /// </summary>
            [XmlAttribute("production")]
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
            [XmlIgnore()]
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

            /// <remarks/>
            [XmlAttribute("comments")]
            public string Comments
            {
                get
                {
                    return this.commentsField;
                }
                set
                {
                    this.commentsField = value;
                }
            }

            /// <remarks/>
            public override string ToString()
            {
                return string.Join("\n", this.TextEquivs.Select(t => t.Unicode));
            }
        }
    }
}
