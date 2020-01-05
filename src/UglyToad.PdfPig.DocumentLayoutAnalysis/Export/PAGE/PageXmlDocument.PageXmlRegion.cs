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
        [XmlInclude(typeof(PageXmlMapRegion))]
        [XmlInclude(typeof(PageXmlCustomRegion))]
        [XmlInclude(typeof(PageXmlUnknownRegion))]
        [XmlInclude(typeof(PageXmlNoiseRegion))]
        [XmlInclude(typeof(PageXmlAdvertRegion))]
        [XmlInclude(typeof(PageXmlMusicRegion))]
        [XmlInclude(typeof(PageXmlChemRegion))]
        [XmlInclude(typeof(PageXmlMathsRegion))]
        [XmlInclude(typeof(PageXmlSeparatorRegion))]
        [XmlInclude(typeof(PageXmlChartRegion))]
        [XmlInclude(typeof(PageXmlTableRegion))]
        [XmlInclude(typeof(PageXmlGraphicRegion))]
        [XmlInclude(typeof(PageXmlLineDrawingRegion))]
        [XmlInclude(typeof(PageXmlImageRegion))]
        [XmlInclude(typeof(PageXmlTextRegion))]
        [GeneratedCode("xsd", "4.6.1055.0")]
        [Serializable()]
        [DebuggerStepThrough()]
        [DesignerCategory("code")]
        [XmlType(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
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
            [XmlElement("AdvertRegion", typeof(PageXmlAdvertRegion))]
            [XmlElement("ChartRegion", typeof(PageXmlChartRegion))]
            [XmlElement("ChemRegion", typeof(PageXmlChemRegion))]
            [XmlElement("CustomRegion", typeof(PageXmlCustomRegion))]
            [XmlElement("GraphicRegion", typeof(PageXmlGraphicRegion))]
            [XmlElement("ImageRegion", typeof(PageXmlImageRegion))]
            [XmlElement("LineDrawingRegion", typeof(PageXmlLineDrawingRegion))]
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

            /// <summary>
            /// Is this region a continuation of another region
            /// (in previous column or page, for example)?
            /// </summary>
            [XmlAttribute("continuation")]
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
            [XmlIgnore()]
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
