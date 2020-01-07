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
        public class PageXmlOrderedGroupIndexed
        {

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

            /// <remarks/>
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
            [XmlElement("OrderedGroupIndexed", typeof(PageXmlOrderedGroupIndexed))]
            [XmlElement("RegionRefIndexed", typeof(PageXmlRegionRefIndexed))]
            [XmlElement("UnorderedGroupIndexed", typeof(PageXmlUnorderedGroupIndexed))]
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

            /// <remarks/>
            [XmlAttribute("regionRef", DataType = "IDREF")]
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

            /// <remarks/>
            [XmlAttribute("index")]
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
            [XmlAttribute("caption")]
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
            [XmlAttribute("type")]
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

            /// <remarks/>
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

            /// <remarks/>
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
        }
    }
}
