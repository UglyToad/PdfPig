namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Tag.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoTag
        {
            /// <summary>
            /// The xml data wrapper element XmlData is used to contain XML encoded metadata.
            /// The content of an XmlData element can be in any namespace or in no namespace.
            /// As permitted by the XML Schema Standard, the processContents attribute value for the
            /// metadata in an XmlData is set to "lax". Therefore, if the source schema and its location are
            /// identified by means of an XML schemaLocation attribute, then an XML processor will validate
            /// the elements for which it can find declarations.If a source schema is not identified, or cannot be
            /// found at the specified schemaLocation, then an XML validator will check for well-formedness,
            /// but otherwise skip over the elements appearing in the XmlData element.
            /// </summary>
            public AltoTagXmlData XmlData { get; set; }

            /// <remarks/>
            [XmlAttribute("ID", DataType = "ID")]
            public string Id { get; set; }

            /// <summary>
            /// Type can be used to classify and group the information within each tag element type.
            /// </summary>
            [XmlAttribute("TYPE")]
            public string Type { get; set; }

            /// <summary>
            /// Content / information value of the tag.
            /// </summary>
            [XmlAttribute("LABEL")]
            public string Label { get; set; }

            /// <summary>
            /// Description text for tag information for clarification.
            /// </summary>
            [XmlAttribute("DESCRIPTION")]
            public string Description { get; set; }

            /// <summary>
            /// Any URI for authority or description relevant information.
            /// </summary>
            [XmlAttribute("URI", DataType = "anyURI")]
            public string Uri { get; set; }
        }
    }
}
