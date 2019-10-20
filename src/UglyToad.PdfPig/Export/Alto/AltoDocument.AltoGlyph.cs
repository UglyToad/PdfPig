namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Modern OCR software stores information on glyph level. A glyph is essentially a character or ligature.
        /// Accordingly the value for the glyph element will be defined as follows:
        /// Pre-composed representation = base + combining character(s) (decomposed representation)
        /// See http://www.fileformat.info/info/unicode/char/0101/index.htm
        /// "U+0101" = (U+0061) + (U+0304)
        /// "combining characters" ("base characters" in combination with non-spacing marks or characters which are combined to one) are represented as one "glyph", e.g.áàâ.
        /// 
        /// <para>Each glyph has its own coordinate information and must be separately addressable as a distinct object.
        /// Correction and verification processes can be carried out for individual characters.</para>
        /// 
        /// <para>Post-OCR analysis of the text as well as adaptive OCR algorithm must be able to record information on glyph level.
        /// In order to reproduce the decision of the OCR software, optional characters must be recorded.These are called variants.
        /// The OCR software evaluates each variant and picks the one with the highest confidence score as the glyph.
        /// The confidence score expresses how confident the OCR software is that a single glyph had been recognized correctly.</para>
        /// 
        /// <para>The glyph elements are in order of the word. Each glyph need to be recorded to built up the whole word sequence.</para>
        /// 
        /// <para>The glyph’s CONTENT attribute is no replacement for the string’s CONTENT attribute.
        /// Due to post-processing steps such as correction the values of both attributes may be inconsistent.</para>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoGlyph : AltoPositionedElement
        {
            private float gc;

            /// <remarks/>
            public AltoShape Shape { get; set; }

            /// <summary>
            /// Alternative (combined) character for the glyph, outlined by OCR engine or similar recognition processes.
            /// In case the variant are two (combining) characters, two characters are outlined in one Variant element.
            /// E.g. a Glyph element with CONTENT="m" can have a Variant element with the content "rn".
            /// <para>Details for different use-cases see on the samples on GitHub.</para>
            /// </summary>
            [XmlElement("Variant")]
            public AltoVariant[] Variant { get; set; }

            /// <remarks/>
            [XmlAttribute("ID", DataType = "ID")]
            public string Id { get; set; }

            /// <summary>
            /// CONTENT contains the precomposed representation (combining character) of the character from the parent String element.
            /// The sequence position of the Gylph element matches the position of the character in the String.
            /// </summary>
            [XmlAttribute("CONTENT")]
            public string Content { get; set; }

            /// <summary>
            /// This GC attribute records a float value between 0.0 and 1.0 that expresses the level of confidence for the variant where is 1 is certain.
            /// This attribute is optional. If it is not available, the default value for the variant is "0".
            /// 
            /// <para>The GC attribute semantic is the same as the WC attribute on the String element and VC on Variant element.</para>
            /// </summary>
            [XmlAttribute("GC")]
            public float Gc
            {
                get => gc;
                set
                {
                    gc = value;
                    if (!float.IsNaN(value)) GcSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool GcSpecified { get; set; }

            /// <remarks/>
            public override string ToString()
            {
                return Content;
            }
        }
    }
}
