namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] A unique identifier for the document.
        /// <para>This identifier must be unique within the local  
        /// To facilitate file sharing or interoperability with other systems, 
        /// documentIdentifierLocation may be added to designate the system or 
        /// application where the identifier is unique.</para>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoDocumentIdentifier
        {
            /// <summary>
            /// A location qualifier, i.e., a namespace.
            /// </summary>
            [XmlAttribute("documentIdentifierLocation")]
            public string DocumentIdentifierLocation { get; set; }

            /// <remarks/>
            [XmlText]
            public string Value { get; set; }
        }
    }
}
