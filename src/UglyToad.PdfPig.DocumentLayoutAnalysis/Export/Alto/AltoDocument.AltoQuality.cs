namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Gives brief information about original page quality
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public enum AltoQuality
        {
            /// <remarks/>
            // ReSharper disable once InconsistentNaming
            OK,

            /// <remarks/>
            Missing,

            /// <remarks/>
            [XmlEnum("Missing in original")]
            MissingInOriginal,

            /// <remarks/>
            Damaged,

            /// <remarks/>
            Retained,

            /// <remarks/>
            Target,

            /// <remarks/>
            [XmlEnum("As in original")]
            AsInOriginal,
        }
    }
}
