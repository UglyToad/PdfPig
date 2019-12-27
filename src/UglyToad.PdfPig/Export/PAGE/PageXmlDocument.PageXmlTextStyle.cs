namespace UglyToad.PdfPig.Export.PAGE
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class PageXmlDocument
    {
        /// <summary>
        /// Monospace (fixed-pitch, non-proportional) or
        /// proportional font.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlTextStyle
        {
            #region private
            private string fontFamilyField;

            private bool serifField;

            private bool serifFieldSpecified;

            private bool monospaceField;

            private bool monospaceFieldSpecified;

            private float fontSizeField;

            private bool fontSizeFieldSpecified;

            private string xHeightField;

            private int kerningField;

            private bool kerningFieldSpecified;

            private PageXmlColourSimpleType textColourField;

            private bool textColourFieldSpecified;

            private string textColourRgbField;

            private PageXmlColourSimpleType bgColourField;

            private bool bgColourFieldSpecified;

            private string bgColourRgbField;

            private bool reverseVideoField;

            private bool reverseVideoFieldSpecified;

            private bool boldField;

            private bool boldFieldSpecified;

            private bool italicField;

            private bool italicFieldSpecified;

            private bool underlinedField;

            private bool underlinedFieldSpecified;

            private PageXmlUnderlineStyleSimpleType underlineStyleField;

            private bool underlineStyleFieldSpecified;

            private bool subscriptField;

            private bool subscriptFieldSpecified;

            private bool superscriptField;

            private bool superscriptFieldSpecified;

            private bool strikethroughField;

            private bool strikethroughFieldSpecified;

            private bool smallCapsField;

            private bool smallCapsFieldSpecified;

            private bool letterSpacedField;

            private bool letterSpacedFieldSpecified;
            #endregion

            /// <summary>
            /// For instance: Arial, Times New Roman.
            /// Add more information if necessary
            /// (e.g.blackletter, antiqua).
            /// </summary>
            [XmlAttributeAttribute("fontFamily")]
            public string FontFamily
            {
                get
                {
                    return this.fontFamilyField;
                }
                set
                {
                    this.fontFamilyField = value;
                }
            }

            /// <summary>
            /// Serif or sans-serif typeface.
            /// </summary>
            [XmlAttributeAttribute("serif")]
            public bool Serif
            {
                get
                {
                    return this.serifField;
                }
                set
                {
                    this.serifField = value;
                    this.serifFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool SerifSpecified
            {
                get
                {
                    return this.serifFieldSpecified;
                }
                set
                {
                    this.serifFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("monospace")]
            public bool Monospace
            {
                get
                {
                    return this.monospaceField;
                }
                set
                {
                    this.monospaceField = value;
                    this.monospaceFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool MonospaceSpecified
            {
                get
                {
                    return this.monospaceFieldSpecified;
                }
                set
                {
                    this.monospaceFieldSpecified = value;
                }
            }

            /// <summary>
            /// The size of the characters in points.
            /// </summary>
            [XmlAttributeAttribute("fontSize")]
            public float FontSize
            {
                get
                {
                    return this.fontSizeField;
                }
                set
                {
                    this.fontSizeField = value;
                    this.fontSizeFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool FontSizeSpecified
            {
                get
                {
                    return this.fontSizeFieldSpecified;
                }
                set
                {
                    this.fontSizeFieldSpecified = value;
                }
            }

            /// <summary>
            /// The x-height or corpus size refers to the distance
            /// between the baseline and the mean line of
            /// lower-case letters in a typeface.
            /// The unit is assumed to be pixels.
            /// </summary>
            [XmlAttributeAttribute("xHeight", DataType = "integer")]
            public string XHeight
            {
                get
                {
                    return this.xHeightField;
                }
                set
                {
                    this.xHeightField = value;
                }
            }

            /// <summary>
            /// The degree of space (in points) between
            /// the characters in a string of text.
            /// </summary>
            [XmlAttributeAttribute("kerning")]
            public int Kerning
            {
                get
                {
                    return this.kerningField;
                }
                set
                {
                    this.kerningField = value;
                    this.kerningFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool KerningSpecified
            {
                get
                {
                    return this.kerningFieldSpecified;
                }
                set
                {
                    this.kerningFieldSpecified = value;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            [XmlAttributeAttribute("textColour")]
            public PageXmlColourSimpleType TextColour
            {
                get
                {
                    return this.textColourField;
                }
                set
                {
                    this.textColourField = value;
                    this.textColourFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool TextColourSpecified
            {
                get
                {
                    return this.textColourFieldSpecified;
                }
                set
                {
                    this.textColourFieldSpecified = value;
                }
            }

            /// <summary>
            /// Text colour in RGB encoded format
            /// <para>(red value) + (256 x green value) + (65536 x blue value).</para> 
            /// </summary>
            [XmlAttributeAttribute("textColourRgb", DataType = "integer")]
            public string TextColourRgb
            {
                get
                {
                    return this.textColourRgbField;
                }
                set
                {
                    this.textColourRgbField = value;
                }
            }

            /// <summary>
            /// Background colour
            /// </summary>
            [XmlAttributeAttribute("bgColour")]
            public PageXmlColourSimpleType BgColour
            {
                get
                {
                    return this.bgColourField;
                }
                set
                {
                    this.bgColourField = value;
                    this.bgColourFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool BgColourSpecified
            {
                get
                {
                    return this.bgColourFieldSpecified;
                }
                set
                {
                    this.bgColourFieldSpecified = value;
                }
            }

            /// <summary>
            /// Background colour in RGB encoded format
            /// <para>(red value) + (256 x green value) + (65536 x blue value).</para>
            /// </summary>
            [XmlAttributeAttribute("bgColourRgb", DataType = "integer")]
            public string BgColourRgb
            {
                get
                {
                    return this.bgColourRgbField;
                }
                set
                {
                    this.bgColourRgbField = value;
                }
            }

            /// <summary>
            /// Specifies whether the colour of the text appears
            /// reversed against a background colour.
            /// </summary>
            [XmlAttributeAttribute("reverseVideo")]
            public bool ReverseVideo
            {
                get
                {
                    return this.reverseVideoField;
                }
                set
                {
                    this.reverseVideoField = value;
                    this.reverseVideoFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ReverseVideoSpecified
            {
                get
                {
                    return this.reverseVideoFieldSpecified;
                }
                set
                {
                    this.reverseVideoFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("bold")]
            public bool Bold
            {
                get
                {
                    return this.boldField;
                }
                set
                {
                    this.boldField = value;
                    this.boldFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool BoldSpecified
            {
                get
                {
                    return this.boldFieldSpecified;
                }
                set
                {
                    this.boldFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("italic")]
            public bool Italic
            {
                get
                {
                    return this.italicField;
                }
                set
                {
                    this.italicField = value;
                    this.italicFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ItalicSpecified
            {
                get
                {
                    return this.italicFieldSpecified;
                }
                set
                {
                    this.italicFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("underlined")]
            public bool Underlined
            {
                get
                {
                    return this.underlinedField;
                }
                set
                {
                    this.underlinedField = value;
                    this.underlinedFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool UnderlinedSpecified
            {
                get
                {
                    return this.underlinedFieldSpecified;
                }
                set
                {
                    this.underlinedFieldSpecified = value;
                }
            }

            /// <summary>
            /// Line style details if "underlined" is TRUE
            /// </summary>
            [XmlAttributeAttribute("underlineStyle")]
            public PageXmlUnderlineStyleSimpleType UnderlineStyle
            {
                get
                {
                    return this.underlineStyleField;
                }
                set
                {
                    this.underlineStyleField = value;
                    this.underlineStyleFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool UnderlineStyleSpecified
            {
                get
                {
                    return this.underlineStyleFieldSpecified;
                }
                set
                {
                    this.underlineStyleFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("subscript")]
            public bool Subscript
            {
                get
                {
                    return this.subscriptField;
                }
                set
                {
                    this.subscriptField = value;
                    this.subscriptFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool SubscriptSpecified
            {
                get
                {
                    return this.subscriptFieldSpecified;
                }
                set
                {
                    this.subscriptFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("superscript")]
            public bool Superscript
            {
                get
                {
                    return this.superscriptField;
                }
                set
                {
                    this.superscriptField = value;
                    this.superscriptFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool SuperscriptSpecified
            {
                get
                {
                    return this.superscriptFieldSpecified;
                }
                set
                {
                    this.superscriptFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("strikethrough")]
            public bool Strikethrough
            {
                get
                {
                    return this.strikethroughField;
                }
                set
                {
                    this.strikethroughField = value;
                    this.strikethroughFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool StrikethroughSpecified
            {
                get
                {
                    return this.strikethroughFieldSpecified;
                }
                set
                {
                    this.strikethroughFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("smallCaps")]
            public bool SmallCaps
            {
                get
                {
                    return this.smallCapsField;
                }
                set
                {
                    this.smallCapsField = value;
                    this.smallCapsFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool SmallCapsSpecified
            {
                get
                {
                    return this.smallCapsFieldSpecified;
                }
                set
                {
                    this.smallCapsFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("letterSpaced")]
            public bool LetterSpaced
            {
                get
                {
                    return this.letterSpacedField;
                }
                set
                {
                    this.letterSpacedField = value;
                    this.letterSpacedFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool LetterSpacedSpecified
            {
                get
                {
                    return this.letterSpacedFieldSpecified;
                }
                set
                {
                    this.letterSpacedFieldSpecified = value;
                }
            }
        }
    }
}
