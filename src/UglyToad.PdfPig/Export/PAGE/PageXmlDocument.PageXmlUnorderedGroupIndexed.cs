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
        /// Indexed group containing ordered elements
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlUnorderedGroupIndexed
        {
            #region private
            private PageXmlUserAttribute[] userDefinedField;

            private PageXmlLabels[] labelsField;

            private object[] itemsField;

            private string idField;

            private string regionRefField;

            private int indexField;

            private string captionField;

            private PageXmlGroupSimpleType typeField;

            private bool typeFieldSpecified;

            private bool continuationField;

            private bool continuationFieldSpecified;

            private string customField;

            private string commentsField;
            #endregion

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
            [XmlElementAttribute("OrderedGroup", typeof(PageXmlOrderedGroup))]
            [XmlElementAttribute("RegionRef", typeof(PageXmlRegionRef))]
            [XmlElementAttribute("UnorderedGroup", typeof(PageXmlUnorderedGroup))]
            public object[] Items
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
            /// Optional link to a parent region of nested regions.
            /// The parent region doubles as reading order group.
            /// Only the nested regions should be allowed as group members.
            /// </summary>
            [XmlAttributeAttribute("regionRef", DataType = "IDREF")]
            public string RegionRef
            {
                get
                {
                    return this.regionRefField;
                }
                set
                {
                    this.regionRefField = value;
                }
            }

            /// <summary>
            /// Position (order number) of this item within the current hierarchy level.
            /// </summary>
            [XmlAttributeAttribute("index")]
            public int Index
            {
                get
                {
                    return this.indexField;
                }
                set
                {
                    this.indexField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("caption")]
            public string Caption
            {
                get
                {
                    return this.captionField;
                }
                set
                {
                    this.captionField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("type")]
            public PageXmlGroupSimpleType Type
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
            /// Is this group a continuation of another group (from
            /// previous column or page, for example)?
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
