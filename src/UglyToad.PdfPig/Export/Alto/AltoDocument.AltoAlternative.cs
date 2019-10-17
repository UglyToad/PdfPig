namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Alternative.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoAlternative
        {
            /// <summary>
            /// Purpose.
            /// </summary>
            [XmlAttribute("PURPOSE")]
            public string Purpose { get; set; }

            /// <summary>
            /// Value.
            /// </summary>
            [XmlText]
            public string Value { get; set; }
        }
    }
}
