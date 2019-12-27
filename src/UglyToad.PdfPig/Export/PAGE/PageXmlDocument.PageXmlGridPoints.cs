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
        /// Points with x,y coordinates.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlGridPoints
        {

            private int indexField;

            private string pointsField;

            /// <summary>
            /// The grid row index
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
            [XmlAttributeAttribute("points")]
            public string Points
            {
                get
                {
                    return this.pointsField;
                }
                set
                {
                    this.pointsField = value;
                }
            }
        }
    }
}
