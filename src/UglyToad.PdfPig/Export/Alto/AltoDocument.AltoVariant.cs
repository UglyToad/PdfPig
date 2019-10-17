namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Alternative (combined) character for the glyph, outlined by OCR engine or similar recognition processes.
        /// In case the variant are two (combining) characters, two characters are outlined in one Variant element.
        /// E.g. a Glyph element with CONTENT="m" can have a Variant element with the content "rn".
        /// Details for different use-cases see on the samples on GitHub.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoVariant
        {
            private float vcField;

            /// <summary>
            /// Each Variant represents an option for the glyph that the OCR software detected as possible alternatives.
            /// In case the variant are two(combining) characters, two characters are outlined in one Variant element.
            /// E.g.a Glyph element with CONTENT="m" can have a Variant element with the content "rn".
            /// 
            /// <para>Details for different use-cases see on the samples on GitHub.</para>
            /// </summary>
            [XmlAttribute("CONTENT")]
            public string Content { get; set; }

            /// <summary>
            /// This VC attribute records a float value between 0.0 and 1.0 that expresses the level of confidence 
            /// for the variant where is 1 is certain.
            /// This attribute is optional. If it is not available, the default value for the variant is "0".
            /// The VC attribute semantic is the same as the GC attribute on the Glyph element.
            /// </summary>
            [XmlAttribute("VC")]
            public float Vc
            {
                get => vcField;
                set
                {
                    vcField = value;
                    if (!float.IsNaN(value)) VcSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool VcSpecified { get; private set; }
        }
    }
}
