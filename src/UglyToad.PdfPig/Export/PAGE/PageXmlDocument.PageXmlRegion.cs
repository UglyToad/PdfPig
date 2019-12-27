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
        [XmlIncludeAttribute(typeof(PageXmlMapRegion))]
        [XmlIncludeAttribute(typeof(PageXmlCustomRegion))]
        [XmlIncludeAttribute(typeof(PageXmlUnknownRegion))]
        [XmlIncludeAttribute(typeof(PageXmlNoiseRegion))]
        [XmlIncludeAttribute(typeof(PageXmlAdvertRegion))]
        [XmlIncludeAttribute(typeof(PageXmlMusicRegion))]
        [XmlIncludeAttribute(typeof(PageXmlChemRegion))]
        [XmlIncludeAttribute(typeof(PageXmlMathsRegion))]
        [XmlIncludeAttribute(typeof(PageXmlSeparatorRegion))]
        [XmlIncludeAttribute(typeof(PageXmlChartRegion))]
        [XmlIncludeAttribute(typeof(PageXmlTableRegion))]
        [XmlIncludeAttribute(typeof(PageXmlGraphicRegion))]
        [XmlIncludeAttribute(typeof(PageXmlLineDrawingRegion))]
        [XmlIncludeAttribute(typeof(PageXmlImageRegion))]
        [XmlIncludeAttribute(typeof(PageXmlTextRegion))]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public abstract class PageXmlRegion
        {
            #region private
            private PageXmlAlternativeImage[] alternativeImageField;

            private PageXmlCoords coordsField;

            private PageXmlUserAttribute[] userDefinedField;

            private PageXmlLabels[] labelsField;

            private PageXmlRoles rolesField;

            private PageXmlRegion[] itemsField;

            private string idField;

            private string customField;

            private string commentsField;

            private bool continuationField;

            private bool continuationFieldSpecified;
            #endregion

            /// <summary>
            /// Alternative region images (e.g.black-and-white).
            /// </summary>
            [XmlElementAttribute("AlternativeImage")]
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

            /// <summary>
            /// Roles the region takes (e.g. in context of a parent region).
            /// </summary>
            public PageXmlRoles Roles
            {
                get
                {
                    return this.rolesField;
                }
                set
                {
                    this.rolesField = value;
                }
            }

            /// <remarks/>
            [XmlElementAttribute("AdvertRegion", typeof(PageXmlAdvertRegion))]
            [XmlElementAttribute("ChartRegion", typeof(PageXmlChartRegion))]
            [XmlElementAttribute("ChemRegion", typeof(PageXmlChemRegion))]
            [XmlElementAttribute("CustomRegion", typeof(PageXmlCustomRegion))]
            [XmlElementAttribute("GraphicRegion", typeof(PageXmlGraphicRegion))]
            [XmlElementAttribute("ImageRegion", typeof(PageXmlImageRegion))]
            [XmlElementAttribute("LineDrawingRegion", typeof(PageXmlLineDrawingRegion))]
            [XmlElementAttribute("MathsRegion", typeof(PageXmlMathsRegion))]
            [XmlElementAttribute("MusicRegion", typeof(PageXmlMusicRegion))]
            [XmlElementAttribute("NoiseRegion", typeof(PageXmlNoiseRegion))]
            [XmlElementAttribute("SeparatorRegion", typeof(PageXmlSeparatorRegion))]
            [XmlElementAttribute("TableRegion", typeof(PageXmlTableRegion))]
            [XmlElementAttribute("TextRegion", typeof(PageXmlTextRegion))]
            [XmlElementAttribute("UnknownRegion", typeof(PageXmlUnknownRegion))]
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

            /// <summary>
            /// Is this region a continuation of another region
            /// (in previous column or page, for example)?
            /// </summary>
            [XmlAttributeAttribute("continuation")]
            public bool Continuation
            {
                get
                {
                    return this.continuationField;
                }
                set
                {
                    this.continuationField = value;
                    this.continuationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ContinuationSpecified
            {
                get
                {
                    return this.continuationFieldSpecified;
                }
                set
                {
                    this.continuationFieldSpecified = value;
                }
            }
        }
    }
}
