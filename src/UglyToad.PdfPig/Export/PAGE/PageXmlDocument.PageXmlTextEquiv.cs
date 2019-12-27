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
        public class PageXmlTextEquiv
        {
            #region private
            private string plainTextField;

            private string unicodeField;

            private string indexField;

            private float confField;

            private bool confFieldSpecified;

            private PageXmlTextDataSimpleType dataTypeField;

            private bool dataTypeFieldSpecified;

            private string dataTypeDetailsField;

            private string commentsField;
            #endregion

            /// <summary>
            /// Text in a "simple" form (ASCII or extended ASCII
            /// as mostly used for typing). I.e.no use of
            /// special characters for ligatures (should be
            /// stored as two separate characters) etc.
            /// </summary>
            public string PlainText
            {
                get
                {
                    return this.plainTextField;
                }
                set
                {
                    this.plainTextField = value;
                }
            }

            /// <summary>
            /// Correct encoding of the original, always using the corresponding Unicode code point. 
            /// I.e. ligatures have to be represented as one character etc.
            /// </summary>
            public string Unicode
            {
                get
                {
                    return this.unicodeField;
                }
                set
                {
                    this.unicodeField = value;
                }
            }

            /// <summary>
            /// Used for sort order in case multiple TextEquivs are defined. 
            /// The text content with the lowest index should be interpreted as the main text content.
            /// </summary>
            [XmlAttributeAttribute("index", DataType = "integer")]
            public string Index
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
            [XmlAttributeAttribute("conf")]
            public float Conf
            {
                get
                {
                    return this.confField;
                }
                set
                {
                    this.confField = value;
                    this.confFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ConfSpecified
            {
                get
                {
                    return this.confFieldSpecified;
                }
                set
                {
                    this.confFieldSpecified = value;
                }
            }

            /// <summary>
            /// Type of text content (is it free text or a number, for instance). This is only 
            /// a descriptive attribute, the text type is not checked during XML validation.
            /// </summary>
            [XmlAttributeAttribute("dataType")]
            public PageXmlTextDataSimpleType DataType
            {
                get
                {
                    return this.dataTypeField;
                }
                set
                {
                    this.dataTypeField = value;
                    this.dataTypeFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool DataTypeSpecified
            {
                get
                {
                    return this.dataTypeFieldSpecified;
                }
                set
                {
                    this.dataTypeFieldSpecified = value;
                }
            }

            /// <summary>
            /// Refinement for dataType attribute. Can be a regular expression, for instance.
            /// </summary>
            [XmlAttributeAttribute("dataTypeDetails")]
            public string DataTypeDetails
            {
                get
                {
                    return this.dataTypeDetailsField;
                }
                set
                {
                    this.dataTypeDetailsField = value;
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

            /// <remarks/>
            public override string ToString()
            {
                return this.Unicode;
            }
        }
    }
}
