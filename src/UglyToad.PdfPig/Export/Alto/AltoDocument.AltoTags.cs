namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
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
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoTags
        {
            /// <remarks/>
            [XmlElement("LayoutTag", typeof(AltoTag))]
            [XmlElement("NamedEntityTag", typeof(AltoTag))]
            [XmlElement("OtherTag", typeof(AltoTag))]
            [XmlElement("RoleTag", typeof(AltoTag))]
            [XmlElement("StructureTag", typeof(AltoTag))]
            [XmlChoiceIdentifier("ItemsElementName")]
            public AltoTag[] Items { get; set; }

            /// <remarks/>
            [XmlElement("ItemsElementName")]
            [XmlIgnore]
            public AltoItemsChoice[] ItemsElementName { get; set; }
        }
    }
}
