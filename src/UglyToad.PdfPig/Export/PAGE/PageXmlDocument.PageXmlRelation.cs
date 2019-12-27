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
        /// One-to-one relation between to layout object. Use 'link'
        /// for loose relations and 'join' for strong relations
        /// (where something is fragmented for instance).
        /// 
        /// <para>Examples for 'link': caption - image floating -
        /// paragraph paragraph - paragraph (when a paragraph is
        /// split across columns and the last word of the first
        /// paragraph DOES NOT continue in the second paragraph)
        /// drop-cap - paragraph (when the drop-cap is a whole word)</para>
        /// 
        /// Examples for 'join': word - word (separated word at the
        /// end of a line) drop-cap - paragraph (when the drop-cap
        /// is not a whole word) paragraph - paragraph (when a
        /// pragraph is split across columns and the last word of
        /// the first paragraph DOES continue in the second
        /// paragraph)
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlRelation
        {

            private PageXmlLabels[] labelsField;

            private PageXmlRegionRef sourceRegionRefField;

            private PageXmlRegionRef targetRegionRefField;

            private string idField;

            private PageXmlRelationType typeField;

            private bool typeFieldSpecified;

            private string customField;

            private string commentsField;

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
            public PageXmlRegionRef SourceRegionRef
            {
                get
                {
                    return this.sourceRegionRefField;
                }
                set
                {
                    this.sourceRegionRefField = value;
                }
            }

            /// <remarks/>
            public PageXmlRegionRef TargetRegionRef
            {
                get
                {
                    return this.targetRegionRefField;
                }
                set
                {
                    this.targetRegionRefField = value;
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
            [XmlAttributeAttribute("type")]
            public PageXmlRelationType Type
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
