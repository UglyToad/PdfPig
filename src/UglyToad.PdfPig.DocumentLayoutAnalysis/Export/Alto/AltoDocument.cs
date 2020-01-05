namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    /// <summary>
    /// [Alto] Alto Schema root
    /// <para>Version 4.1</para>
    /// See https://github.com/altoxml/schema
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Serializable]
    [DebuggerStepThrough]
    [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
    [XmlRoot("alto", Namespace = "http://www.loc.gov/standards/alto/ns-v4#", IsNullable = false)]
    public partial class AltoDocument
    {
        /// <summary>
        /// Describes general settings of the alto file like measurement units and metadata
        /// </summary>
        public AltoDescription Description { get; set; }

        /// <summary>
        /// Styles define properties of layout elements. A style defined in a parent element
        /// is used as default style for all related children elements.
        /// </summary>
        public AltoStyles Styles { get; set; }

        /// <summary>
        /// Tag define properties of additional characteristic. The tags are referenced from 
        /// related content element on Block or String element by attribute TAGREF via the tag ID.
        /// 
        /// This container element contains the individual elements for LayoutTags, StructureTags,
        /// RoleTags, NamedEntityTags and OtherTags
        /// </summary>
        public AltoTags Tags { get; set; }

        /// <summary>
        /// The root layout element.
        /// </summary>
        public AltoLayout Layout { get; set; }

        /// <summary>
        /// Schema version of the ALTO file.
        /// </summary>
        [XmlAttribute("SCHEMAVERSION")]
        public string SchemaVersion { get; set; }
    }
}
