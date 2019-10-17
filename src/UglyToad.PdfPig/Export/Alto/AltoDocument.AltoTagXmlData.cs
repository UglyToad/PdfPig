namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] The xml data wrapper element XmlData is used to contain XML encoded metadata.
        /// The content of an XmlData element can be in any namespace or in no namespace.
        /// As permitted by the XML Schema Standard, the processContents attribute value for the
        /// metadata in an XmlData is set to "lax". Therefore, if the source schema and its location are
        /// identified by means of an XML schemaLocation attribute, then an XML processor will validate
        /// the elements for which it can find declarations. If a source schema is not identified, or cannot be
        /// found at the specified schemaLocation, then an XML validator will check for well-formedness,
        /// but otherwise skip over the elements appearing in the XmlData element.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(AnonymousType = true, Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoTagXmlData
        {
            /// <remarks/>
            [XmlAnyElement]
            public XmlElement[] Any { get; set; }
        }
    }
}
