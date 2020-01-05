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
        public class PageXmlMetadata
        {

            private string creatorField;

            private DateTime createdField;

            private DateTime lastChangeField;

            private string commentsField;

            private PageXmlUserAttribute[] userDefinedField;

            private PageXmlMetadataItem[] metadataItemField;

            private string externalRefField;

            /// <remarks/>
            public string Creator
            {
                get
                {
                    return this.creatorField;
                }
                set
                {
                    this.creatorField = value;
                }
            }

            /// <summary>
            /// The timestamp has to be in UTC (Coordinated Universal Time) and not local time.
            /// </summary>
            public DateTime Created
            {
                get
                {
                    return this.createdField;
                }
                set
                {
                    this.createdField = value;
                }
            }

            /// <summary>
            /// The timestamp has to be in UTC (Coordinated Universal Time) and not local time.
            /// </summary>
            public DateTime LastChange
            {
                get
                {
                    return this.lastChangeField;
                }
                set
                {
                    this.lastChangeField = value;
                }
            }

            /// <remarks/>
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
            /// 
            /// </summary>
            [XmlElement("MetadataItem")]
            public PageXmlMetadataItem[] MetadataItems
            {
                get
                {
                    return this.metadataItemField;
                }
                set
                {
                    this.metadataItemField = value;
                }
            }

            /// <summary>
            /// External reference of any kind
            /// </summary>
            [XmlAttribute("externalRef")]
            public string ExternalRef
            {
                get
                {
                    return this.externalRefField;
                }
                set
                {
                    this.externalRefField = value;
                }
            }
        }
    }
}
