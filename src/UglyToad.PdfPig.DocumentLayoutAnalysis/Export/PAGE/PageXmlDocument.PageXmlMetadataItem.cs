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
        public class PageXmlMetadataItem
        {

            private PageXmlLabels[] labelsField;

            private PageXmlMetadataItemType typeField;

            private bool typeFieldSpecified;

            private string nameField;

            private string valueField;

            private DateTime dateField;

            private bool dateFieldSpecified;

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
            /// Type of metadata (e.g. author)
            /// </summary>
            [XmlAttribute("type")]
            public PageXmlMetadataItemType Type
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
            /// E.g. imagePhotometricInterpretation
            /// </summary>
            [XmlAttribute("name")]
            public string Name
            {
                get
                {
                    return this.nameField;
                }
                set
                {
                    this.nameField = value;
                }
            }

            /// <summary>
            /// E.g. RGB
            /// </summary>
            [XmlAttribute("value")]
            public string Value
            {
                get
                {
                    return this.valueField;
                }
                set
                {
                    this.valueField = value;
                }
            }

            /// <remarks/>
            [XmlAttribute("date")]
            public DateTime Date
            {
                get
                {
                    return this.dateField;
                }
                set
                {
                    this.dateField = value;
                    this.dateFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool DateSpecified
            {
                get
                {
                    return this.dateFieldSpecified;
                }
                set
                {
                    this.dateFieldSpecified = value;
                }
            }
        }
    }
}
