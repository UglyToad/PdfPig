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
        /// Base type for graphemes, grapheme groups and non-printing characters.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [XmlIncludeAttribute(typeof(PageXmlGraphemeGroup))]
        [XmlIncludeAttribute(typeof(PageXmlNonPrintingChar))]
        [XmlIncludeAttribute(typeof(PageXmlGrapheme))]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public abstract class PageXmlGraphemeBase
        {

            private PageXmlTextEquiv[] textEquivField;

            private string idField;

            private int indexField;

            private bool ligatureField;

            private bool ligatureFieldSpecified;

            private PageXmlGraphemeBaseCharType charTypeField;

            private bool charTypeFieldSpecified;

            private string customField;

            private string commentsField;

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
            /// Order index of grapheme, group, or non-printing character
            /// within the parent container (graphemes or glyph or grapheme group).
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

            /// <summary>
            /// Type of character represented by the grapheme, group, or non-printing character element.
            /// </summary>
            [XmlAttributeAttribute("charType")]
            public PageXmlGraphemeBaseCharType CharType
            {
                get
                {
                    return this.charTypeField;
                }
                set
                {
                    this.charTypeField = value;
                    this.charTypeFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool CharTypeSpecified
            {
                get
                {
                    return this.charTypeFieldSpecified;
                }
                set
                {
                    this.charTypeFieldSpecified = value;
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

            /// <summary>
            /// For generic use
            /// </summary>
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
