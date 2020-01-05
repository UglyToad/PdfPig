namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Classification of the category of operation, how the file was created, including generation, modification, 
        /// preprocessing, postprocessing or any other steps.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Flags]
        [Serializable]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public enum AltoProcessingCategory
        {
            /// <summary>
            /// Content generation.
            /// </summary>
            [XmlEnum("contentGeneration")]
            ContentGeneration = 1,
            /// <summary>
            /// Content modification.
            /// </summary>
            [XmlEnum("contentModification")]
            ContentModification = 2,
            /// <summary>
            /// Pre-operation.
            /// </summary>
            [XmlEnum("preOperation")]
            PreOperation = 4,
            /// <summary>
            /// Post-operation.
            /// </summary>
            [XmlEnum("postOperation")]
            PostOperation = 8,
            /// <summary>
            /// Other.
            /// </summary>
            [XmlEnum("other")]
            Other = 16
        }
    }
}
