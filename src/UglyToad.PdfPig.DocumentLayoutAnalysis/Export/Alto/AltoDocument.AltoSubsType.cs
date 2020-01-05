namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Type of the substitution (if any).
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public enum AltoSubsType
        {
            /// <remarks/>
            HypPart1,
            /// <remarks/>
            HypPart2,
            /// <remarks/>
            Abbreviation,
        }
    }
}
