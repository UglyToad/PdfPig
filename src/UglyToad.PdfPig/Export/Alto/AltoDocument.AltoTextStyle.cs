namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] A text style defines font properties of text.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoTextStyle
        {
            private AltoFontType fontType;
            private AltoFontWidth fontWidth;
            private AltoFontStyles fontStyle;

            /// <remarks/>
            [XmlAttribute("ID", DataType = "ID")]
            public string Id { get; set; }

            /// <summary>
            /// The font name.
            /// </summary>
            [XmlAttribute("FONTFAMILY")]
            public string FontFamily { get; set; }

            /// <remarks/>
            [XmlAttribute("FONTTYPE")]
            public AltoFontType FontType
            {
                get => fontType;
                set
                {
                    fontType = value;
                    FontTypeSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool FontTypeSpecified { get; private set; }

            /// <remarks/>
            [XmlAttribute("FONTWIDTH")]
            public AltoFontWidth FontWidth
            {
                get => fontWidth;
                set
                {
                    fontWidth = value;
                    FontWidthSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool FontWidthSpecified { get; private set; }

            /// <summary>
            /// The font size, in points (1/72 of an inch).
            /// </summary>
            [XmlAttribute("FONTSIZE")]
            public float FontSize { get; set; }

            /// <summary>
            /// The font color as an RGB value.
            /// </summary>
            [XmlAttribute("FONTCOLOR", DataType = "hexBinary")]
            public byte[] FontColor { get; set; }

            /// <summary>
            /// The font style.
            /// </summary>
            [XmlAttribute("FONTSTYLE")]
            public AltoFontStyles FontStyle
            {
                get => fontStyle;
                set
                {
                    fontStyle = value;
                    FontStyleSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool FontStyleSpecified { get; private set; }
        }
    }
}
