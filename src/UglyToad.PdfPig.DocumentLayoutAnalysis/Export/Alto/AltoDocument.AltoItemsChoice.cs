namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] There are following variation of tag types available:
        /// LayoutTag – criteria about arrangement or graphical appearance; 
        /// StructureTag – criteria about grouping or formation; 
        /// RoleTag – criteria about function or mission; 
        /// NamedEntityTag – criteria about assignment of terms to their relationship / meaning (NER); 
        /// OtherTag – criteria about any other characteristic not listed above, the TYPE attribute is intended to be used for classification within those.; 
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#", IncludeInSchema = false)]
        public enum AltoItemsChoice
        {
            /// <summary>
            /// Criteria about arrangement or graphical appearance.
            /// </summary>
            LayoutTag,
            /// <summary>
            /// Criteria about assignment of terms to their relationship / meaning (NER).
            /// </summary>
            NamedEntityTag,
            /// <summary>
            /// Criteria about any other characteristic not listed above, the TYPE attribute is intended to be used for classification within those.
            /// </summary>
            OtherTag,
            /// <summary>
            /// Criteria about function or mission.
            /// </summary>
            RoleTag,
            /// <summary>
            /// Criteria about grouping or formation.
            /// </summary>
            StructureTag
        }
    }
}
