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
        public class PageXmlGlyph
        {
            #region private
            private PageXmlAlternativeImage[] alternativeImageField;

            private PageXmlCoords coordsField;

            private PageXmlGraphemeBase[] graphemesField;

            private PageXmlTextEquiv[] textEquivField;

            private PageXmlTextStyle textStyleField;

            private PageXmlUserAttribute[] userDefinedField;

            private PageXmlLabels[] labelsField;

            private string idField;

            private bool ligatureField;

            private bool ligatureFieldSpecified;

            private bool symbolField;

            private bool symbolFieldSpecified;

            private PageXmlScriptSimpleType scriptField;

            private bool scriptFieldSpecified;

            private PageXmlProductionSimpleType productionField;

            private bool productionFieldSpecified;

            private string customField;

            private string commentsField;
            #endregion

            /// <summary>
            /// Alternative glyph images (e.g. black-and-white)
            /// </summary>
            [XmlElementAttribute("AlternativeImage")]
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

            /// <summary>
            /// Container for graphemes, grapheme groups and non-printing characters
            /// </summary>
            [XmlArrayItemAttribute("Grapheme", typeof(PageXmlGrapheme), IsNullable = false)]
            [XmlArrayItemAttribute("GraphemeGroup", typeof(PageXmlGraphemeGroup), IsNullable = false)]
            [XmlArrayItemAttribute("NonPrintingChar", typeof(PageXmlNonPrintingChar), IsNullable = false)]
            public PageXmlGraphemeBase[] Graphemes
            {
                get
                {
                    return this.graphemesField;
                }
                set
                {
                    this.graphemesField = value;
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

            /// <remarks/>
            [XmlArrayItemAttribute("UserAttribute", IsNullable = false)]
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
            [XmlElementAttribute("Labels")]
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
            [XmlAttributeAttribute("id", DataType = "ID")]
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

            /// <remarks/>
            [XmlAttributeAttribute("ligature")]
            public bool Ligature
            {
                get
                {
                    return this.ligatureField;
                }
                set
                {
                    this.ligatureField = value;
                    this.ligatureFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool LigatureSpecified
            {
                get
                {
                    return this.ligatureFieldSpecified;
                }
                set
                {
                    this.ligatureFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("symbol")]
            public bool Symbol
            {
                get
                {
                    return this.symbolField;
                }
                set
                {
                    this.symbolField = value;
                    this.symbolFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool SymbolSpecified
            {
                get
                {
                    return this.symbolFieldSpecified;
                }
                set
                {
                    this.symbolFieldSpecified = value;
                }
            }

            /// <summary>
            /// The script used for the glyph
            /// </summary>
            [XmlAttributeAttribute("script")]
            public PageXmlScriptSimpleType Script
            {
                get
                {
                    return this.scriptField;
                }
                set
                {
                    this.scriptField = value;
                    this.scriptFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ScriptSpecified
            {
                get
                {
                    return this.scriptFieldSpecified;
                }
                set
                {
                    this.scriptFieldSpecified = value;
                }
            }

            /// <summary>
            /// Overrides the production attribute of the parent word / text line / text region.
            /// </summary>
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

            /// <summary>
            /// For generic use
            /// </summary>
            [XmlAttributeAttribute("custom")]
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
            [XmlAttributeAttribute("comments")]
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
        }
    }
}
