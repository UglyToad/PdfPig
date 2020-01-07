namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] A block that consists of other blocks.
        /// <para>WARNING: The CIRCULAR GROUP REFERENCES was removed from the xsd.
        /// NEED TO ADD IT BACK!!!</para>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoComposedBlock : AltoBlock
        {
            // TODO: what is this?
            /*****************************************************************
             * /!\                         WARNING                         /!\
             * The CIRCULAR GROUP REFERENCES below was removed from the xsd
             * NEED TO ADD IT BACK!!!
             * <xsd:sequence minOccurs="0" maxOccurs="unbounded">
             *      <xsd:group ref="BlockGroup"/>
             * </xsd:sequence> 
             *****************************************************************/

            /// <summary>
            /// A user defined string to identify the type of composed block (e.g. table, advertisement, ...)
            /// </summary>
            [XmlAttribute("TYPE")]
            public string TypeComposed { get; set; }

            /// <summary>
            /// An ID to link to an image which contains only the composed block. 
            /// The ID and the file link is defined in the related METS file.
            /// </summary>
            [XmlAttribute("FILEID")]
            public string FileId { get; set; }
        }
    }
}
