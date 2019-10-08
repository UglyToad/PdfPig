using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Xml.Serialization;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.Geometry;
using UglyToad.PdfPig.Graphics.Colors;
using UglyToad.PdfPig.Util;

namespace UglyToad.PdfPig.Export
{
    /// <summary>
    /// PAGE-XML 2019-07-15 (XML) text exporter.
    /// <para>See https://github.com/PRImA-Research-Lab/PAGE-XML </para>
    /// </summary>
    internal class PageXmlTextExporter : ITextExporter
    {
        private IPageSegmenter pageSegmenter;
        private IWordExtractor wordExtractor;

        private decimal scale;
        private string indentChar;

        int lineCount = 0;
        int wordCount = 0;
        int glyphCount = 0;
        int regionCount = 0;

        /// <summary>
        /// PAGE-XML 2019-07-15 (XML) text exporter.
        /// <para>See https://github.com/PRImA-Research-Lab/PAGE-XML </para>
        /// </summary>
        /// <param name="wordExtractor"></param>
        /// <param name="pageSegmenter"></param>
        /// <param name="scale"></param>
        /// <param name="indent">Indent character.</param>
        public PageXmlTextExporter(IWordExtractor wordExtractor, IPageSegmenter pageSegmenter, double scale = 1.0, string indent = "\t")
        {
            this.wordExtractor = wordExtractor;
            this.pageSegmenter = pageSegmenter;
            this.scale = (decimal)scale;
            this.indentChar = indent;
        }

        /// <summary>
        /// Get the PAGE-XML (XML) string of the pages layout.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="includePaths">Draw <see cref="PdfPath"/>s present in the page.</param>
        public string Get(PdfDocument document, bool includePaths = false)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the PAGE-XML (XML) string of the pages layout. Excludes <see cref="PdfPath"/>s.
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public string Get(Page page)
        {
            return Get(page, false);
        }

        /// <summary>
        /// Get the PAGE-XML (XML) string of the pages layout.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="includePaths">Draw <see cref="PdfPath"/>s present in the page.</param>
        public string Get(Page page, bool includePaths)
        {
            PageXmlDocument pageXmlDocument = new PageXmlDocument()
            {
                Metadata = new PageXmlDocument.PageXmlMetadata()
                {
                    Created = DateTime.UtcNow,
                    LastChange = DateTime.UtcNow,
                    Creator = "PdfPig",
                    Comments = pageSegmenter.GetType().Name + "|" + wordExtractor.GetType().Name,
                },
                PcGtsId = "pc-" + page.GetHashCode()
            };

            pageXmlDocument.Page = ToPageXmlPage(page, includePaths);

            return Serialize(pageXmlDocument);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private string PointToString(PdfPoint point, decimal height)
        {
            decimal x = Math.Round(point.X * scale);
            decimal y = Math.Round((height - point.Y) * scale);
            return (x > 0 ? x : 0).ToString("0") + "," + (y > 0 ? y : 0).ToString("0");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private string ToPoints(IEnumerable<PdfPoint> points, decimal height)
        {
            return string.Join(" ", points.Select(p => PointToString(p, height)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pdfRectangle"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private string ToPoints(PdfRectangle pdfRectangle, decimal height)
        {
            return ToPoints(new[] { pdfRectangle.BottomLeft, pdfRectangle.TopLeft, pdfRectangle.TopRight, pdfRectangle.BottomRight }, height);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pdfRectangle"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private PageXmlDocument.PageXmlCoords ToCoords(PdfRectangle pdfRectangle, decimal height)
        {
            return new PageXmlDocument.PageXmlCoords()
            {
                //Conf = 1,
                Points = ToPoints(pdfRectangle, height)
            };
        }

        /// <summary>
        /// PageXml Text colour in RGB encoded format
        /// <para>(red value) + (256 x green value) + (65536 x blue value).</para> 
        /// </summary>
        private string ToRgbEncoded(IColor color)
        {
            var rgb = color.ToRGBValues();
            int red = (int)Math.Round(255f * (float)rgb.r);
            int green = 256 * (int)Math.Round(255f * (float)rgb.g);
            int blue = 65536 * (int)Math.Round(255f * (float)rgb.b);
            int sum = red + green + blue;

            // as per below, red and blue order might be inverted...
            //var colorWin = System.Drawing.Color.FromArgb(sum);

            return sum.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="includePaths">Draw <see cref="PdfPath"/>s present in the page.</param>
        private PageXmlDocument.PageXmlPage ToPageXmlPage(Page page, bool includePaths)
        {
            var pageXmlPage = new PageXmlDocument.PageXmlPage()
            {
                //Border = new PageXmlBorder()
                //{
                //    Coords = new PageXmlCoords()
                //    {
                //        Points = page.
                //    }
                //},
                ImageFilename = "unknown",
                ImageHeight = (int)Math.Round(page.Height * scale),
                ImageWidth = (int)Math.Round(page.Width * scale),
                //PrintSpace = new PageXmlPrintSpace()
                //{
                //    Coords = new PageXmlCoords()
                //    {

                //    }
                //}
            };

            var words = page.GetWords(wordExtractor);
            var regions = new List<PageXmlDocument.PageXmlRegion>();

            if (words.Count() > 0)
            {
                var blocks = pageSegmenter.GetBlocks(words);
                regions.AddRange(blocks.Select(b => ToPageXmlTextRegion(b, page.Height)));
            }

            if (includePaths)
            {
                var graphicalElements = page.ExperimentalAccess.Paths.Select(p => ToPageXmlLineDrawingRegion(p, page.Height));
                if (graphicalElements.Where(g => g != null).Count() > 0)
                {
                    regions.AddRange(graphicalElements.Where(g => g != null));
                }
            }

            pageXmlPage.Items = regions.ToArray();
            return pageXmlPage;
        }

        private PageXmlDocument.PageXmlLineDrawingRegion ToPageXmlLineDrawingRegion(PdfPath pdfPath, decimal height)
        {
            var bbox = pdfPath.GetBoundingRectangle();
            if (bbox.HasValue)
            {
                regionCount++;
                return new PageXmlDocument.PageXmlLineDrawingRegion()
                {
                    Coords = ToCoords(bbox.Value, height),
                    Id = "r" + regionCount
                };
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="textBlock"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private PageXmlDocument.PageXmlTextRegion ToPageXmlTextRegion(TextBlock textBlock, decimal height)
        {
            regionCount++;
            return new PageXmlDocument.PageXmlTextRegion()
            {
                Coords = ToCoords(textBlock.BoundingBox, height),
                TextLines = textBlock.TextLines.Select(l => ToPageXmlTextLine(l, height)).ToArray(),
                TextEquivs = new[] { new PageXmlDocument.PageXmlTextEquiv() { Unicode = textBlock.Text } },
                Id = "r" + regionCount
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="textLine"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private PageXmlDocument.PageXmlTextLine ToPageXmlTextLine(TextLine textLine, decimal height)
        {
            lineCount++;
            return new PageXmlDocument.PageXmlTextLine()
            {
                Coords = ToCoords(textLine.BoundingBox, height),
                //Baseline = new PageXmlBaseline() { },
                Production = PageXmlDocument.PageXmlProductionSimpleType.Printed,
                //ReadingDirection = PageXmlReadingDirectionSimpleType.LeftToRight,
                Words = textLine.Words.Select(w => ToPageXmlWord(w, height)).ToArray(),
                TextEquivs = new[] { new PageXmlDocument.PageXmlTextEquiv() { Unicode = textLine.Text } },
                Id = "l" + lineCount
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="word"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private PageXmlDocument.PageXmlWord ToPageXmlWord(Word word, decimal height)
        {
            wordCount++;
            return new PageXmlDocument.PageXmlWord()
            {
                Coords = ToCoords(word.BoundingBox, height),
                Glyphs = word.Letters.Select(l => ToPageXmlGlyph(l, height)).ToArray(),
                TextEquivs = new[] { new PageXmlDocument.PageXmlTextEquiv() { Unicode = word.Text } },
                Id = "w" + wordCount
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="letter"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private PageXmlDocument.PageXmlGlyph ToPageXmlGlyph(Letter letter, decimal height)
        {
            glyphCount++;
            return new PageXmlDocument.PageXmlGlyph()
            {
                Coords = ToCoords(letter.GlyphRectangle, height),
                Ligature = false,
                Production = PageXmlDocument.PageXmlProductionSimpleType.Printed,
                TextStyle = new PageXmlDocument.PageXmlTextStyle()
                {
                    FontSize = (float)letter.FontSize,
                    FontFamily = letter.FontName,
                    TextColourRgb = ToRgbEncoded(letter.Color),
                },
                TextEquivs = new[] { new PageXmlDocument.PageXmlTextEquiv() { Unicode = letter.Value } },
                Id = "c" + glyphCount
            };
        }

        private static PageXmlDocument Deserialize(string xmlPath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PageXmlDocument));

            using (var reader = System.Xml.XmlReader.Create(xmlPath))
            {
                return (PageXmlDocument)serializer.Deserialize(reader);
            }
        }

        private string Serialize(PageXmlDocument pageXmlDocument)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PageXmlDocument));
            var settings = new System.Xml.XmlWriterSettings()
            {
                //Encoding = new System.Text.UTF8Encoding(true),
                Indent = true,
                IndentChars = indentChar,
                OmitXmlDeclaration = true // hack to manually handle utf-8
            };

            using (var stringWriter = new System.IO.StringWriter())
            using (var xmlWriter = System.Xml.XmlWriter.Create(stringWriter, settings))
            {
                stringWriter.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>"); // hack to manually handle utf-8
                serializer.Serialize(xmlWriter, pageXmlDocument);
                return stringWriter.ToString();
            }
        }
    }

    #region PageXml Schema
    /******************************************************************************
     * PAGE pagecontent version 2019-07-15 https://www.primaresearch.org/schema/PAGE/gts/pagecontent/2019-07-15/pagecontent.xsd
     * Auto-generated by xsd and improved by BobLD
     ******************************************************************************/

    /// <summary>
    /// PAGE (Page Analysis and Ground-Truth Elements) root
    /// <para>Version 2019-07-15</para>
    /// </summary>
    [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [SerializableAttribute()]
    [DebuggerStepThroughAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
    [XmlRootAttribute("PcGts", Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15", IsNullable = false)]
    public class PageXmlDocument
    {
        private PageXmlMetadata metadataField;

        private PageXmlPage pageField;

        private string pcGtsIdField;

        /// <remarks/>
        public PageXmlMetadata Metadata
        {
            get
            {
                return this.metadataField;
            }
            set
            {
                this.metadataField = value;
            }
        }

        /// <remarks/>
        public PageXmlPage Page
        {
            get
            {
                return this.pageField;
            }
            set
            {
                this.pageField = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute("pcGtsId", DataType = "ID")]
        public string PcGtsId
        {
            get
            {
                return this.pcGtsIdField;
            }
            set
            {
                this.pcGtsIdField = value;
            }
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlMetadata
        {

            private string creatorField;

            private DateTime createdField;

            private DateTime lastChangeField;

            private string commentsField;

            private PageXmlUserAttribute[] userDefinedField;

            private PageXmlMetadataItem[] metadataItemField;

            private string externalRefField;

            /// <remarks/>
            public string Creator
            {
                get
                {
                    return this.creatorField;
                }
                set
                {
                    this.creatorField = value;
                }
            }

            /// <summary>
            /// The timestamp has to be in UTC (Coordinated Universal Time) and not local time.
            /// </summary>
            public DateTime Created
            {
                get
                {
                    return this.createdField;
                }
                set
                {
                    this.createdField = value;
                }
            }

            /// <summary>
            /// The timestamp has to be in UTC (Coordinated Universal Time) and not local time.
            /// </summary>
            public DateTime LastChange
            {
                get
                {
                    return this.lastChangeField;
                }
                set
                {
                    this.lastChangeField = value;
                }
            }

            /// <remarks/>
            public string Comments
            {
                get
                {
                    return this.commentsField;
                }
                set
                {
                    this.commentsField = value;
                }
            }

            /// <remarks/>
            [XmlArrayItemAttribute("UserAttribute", IsNullable = false)]
            public PageXmlUserAttribute[] UserDefined
            {
                get
                {
                    return this.userDefinedField;
                }
                set
                {
                    this.userDefinedField = value;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            [XmlElementAttribute("MetadataItem")]
            public PageXmlMetadataItem[] MetadataItems
            {
                get
                {
                    return this.metadataItemField;
                }
                set
                {
                    this.metadataItemField = value;
                }
            }

            /// <summary>
            /// External reference of any kind
            /// </summary>
            [XmlAttributeAttribute("externalRef")]
            public string ExternalRef
            {
                get
                {
                    return this.externalRefField;
                }
                set
                {
                    this.externalRefField = value;
                }
            }
        }

        /// <summary>
        /// Structured custom data defined by name, type and value.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlUserAttribute
        {

            private string nameField;

            private string descriptionField;

            private PageXmlUserAttributeType typeField;

            private bool typeFieldSpecified;

            private string valueField;

            /// <remarks/>
            [XmlAttributeAttribute("name")]
            public string Name
            {
                get
                {
                    return this.nameField;
                }
                set
                {
                    this.nameField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("description")]
            public string Description
            {
                get
                {
                    return this.descriptionField;
                }
                set
                {
                    this.descriptionField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("type")]
            public PageXmlUserAttributeType Type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                    this.typeFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool TypeSpecified
            {
                get
                {
                    return this.typeFieldSpecified;
                }
                set
                {
                    this.typeFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("value")]
            public string Value
            {
                get
                {
                    return this.valueField;
                }
                set
                {
                    this.valueField = value;
                }
            }
        }

        /// <summary>
        /// Points with x,y coordinates.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlGridPoints
        {

            private int indexField;

            private string pointsField;

            /// <summary>
            /// The grid row index
            /// </summary>
            [XmlAttributeAttribute("index")]
            public int Index
            {
                get
                {
                    return this.indexField;
                }
                set
                {
                    this.indexField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("points")]
            public string Points
            {
                get
                {
                    return this.pointsField;
                }
                set
                {
                    this.pointsField = value;
                }
            }
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlTextEquiv
        {
            #region private
            private string plainTextField;

            private string unicodeField;

            private string indexField;

            private float confField;

            private bool confFieldSpecified;

            private PageXmlTextDataSimpleType dataTypeField;

            private bool dataTypeFieldSpecified;

            private string dataTypeDetailsField;

            private string commentsField;
            #endregion

            /// <summary>
            /// Text in a "simple" form (ASCII or extended ASCII
            /// as mostly used for typing). I.e.no use of
            /// special characters for ligatures (should be
            /// stored as two separate characters) etc.
            /// </summary>
            public string PlainText
            {
                get
                {
                    return this.plainTextField;
                }
                set
                {
                    this.plainTextField = value;
                }
            }

            /// <summary>
            /// Correct encoding of the original, always using the corresponding Unicode code point. 
            /// I.e. ligatures have to be represented as one character etc.
            /// </summary>
            public string Unicode
            {
                get
                {
                    return this.unicodeField;
                }
                set
                {
                    this.unicodeField = value;
                }
            }

            /// <summary>
            /// Used for sort order in case multiple TextEquivs are defined. 
            /// The text content with the lowest index should be interpreted as the main text content.
            /// </summary>
            [XmlAttributeAttribute("index", DataType = "integer")]
            public string Index
            {
                get
                {
                    return this.indexField;
                }
                set
                {
                    this.indexField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("conf")]
            public float Conf
            {
                get
                {
                    return this.confField;
                }
                set
                {
                    this.confField = value;
                    this.confFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ConfSpecified
            {
                get
                {
                    return this.confFieldSpecified;
                }
                set
                {
                    this.confFieldSpecified = value;
                }
            }

            /// <summary>
            /// Type of text content (is it free text or a number, for instance). This is only 
            /// a descriptive attribute, the text type is not checked during XML validation.
            /// </summary>
            [XmlAttributeAttribute("dataType")]
            public PageXmlTextDataSimpleType DataType
            {
                get
                {
                    return this.dataTypeField;
                }
                set
                {
                    this.dataTypeField = value;
                    this.dataTypeFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool DataTypeSpecified
            {
                get
                {
                    return this.dataTypeFieldSpecified;
                }
                set
                {
                    this.dataTypeFieldSpecified = value;
                }
            }

            /// <summary>
            /// Refinement for dataType attribute. Can be a regular expression, for instance.
            /// </summary>
            [XmlAttributeAttribute("dataTypeDetails")]
            public string DataTypeDetails
            {
                get
                {
                    return this.dataTypeDetailsField;
                }
                set
                {
                    this.dataTypeDetailsField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("comments")]
            public string Comments
            {
                get
                {
                    return this.commentsField;
                }
                set
                {
                    this.commentsField = value;
                }
            }

            /// <remarks/>
            public override string ToString()
            {
                return this.Unicode;
            }
        }

        /// <summary>
        /// Base type for graphemes, grapheme groups and non-printing characters.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [XmlIncludeAttribute(typeof(PageXmlGraphemeGroup))]
        [XmlIncludeAttribute(typeof(PageXmlNonPrintingChar))]
        [XmlIncludeAttribute(typeof(PageXmlGrapheme))]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public abstract class PageXmlGraphemeBase
        {

            private PageXmlTextEquiv[] textEquivField;

            private string idField;

            private int indexField;

            private bool ligatureField;

            private bool ligatureFieldSpecified;

            private PageXmlGraphemeBaseCharType charTypeField;

            private bool charTypeFieldSpecified;

            private string customField;

            private string commentsField;

            /// <remarks/>
            [XmlElementAttribute("TextEquiv")]
            public PageXmlTextEquiv[] TextEquivs
            {
                get
                {
                    return this.textEquivField;
                }
                set
                {
                    this.textEquivField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("id", DataType = "ID")]
            public string Id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }

            /// <summary>
            /// Order index of grapheme, group, or non-printing character
            /// within the parent container (graphemes or glyph or grapheme group).
            /// </summary>
            [XmlAttributeAttribute("index")]
            public int Index
            {
                get
                {
                    return this.indexField;
                }
                set
                {
                    this.indexField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("ligature")]
            public bool Ligature
            {
                get
                {
                    return this.ligatureField;
                }
                set
                {
                    this.ligatureField = value;
                    this.ligatureFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool LigatureSpecified
            {
                get
                {
                    return this.ligatureFieldSpecified;
                }
                set
                {
                    this.ligatureFieldSpecified = value;
                }
            }

            /// <summary>
            /// Type of character represented by the grapheme, group, or non-printing character element.
            /// </summary>
            [XmlAttributeAttribute("charType")]
            public PageXmlGraphemeBaseCharType CharType
            {
                get
                {
                    return this.charTypeField;
                }
                set
                {
                    this.charTypeField = value;
                    this.charTypeFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool CharTypeSpecified
            {
                get
                {
                    return this.charTypeFieldSpecified;
                }
                set
                {
                    this.charTypeFieldSpecified = value;
                }
            }

            /// <summary>
            /// For generic use
            /// </summary>
            [XmlAttributeAttribute("custom")]
            public string Custom
            {
                get
                {
                    return this.customField;
                }
                set
                {
                    this.customField = value;
                }
            }

            /// <summary>
            /// For generic use
            /// </summary>
            [XmlAttributeAttribute("comments")]
            public string Comments
            {
                get
                {
                    return this.commentsField;
                }
                set
                {
                    this.commentsField = value;
                }
            }
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlGraphemeGroup : PageXmlGraphemeBase
        {

            private PageXmlGraphemeBase[] itemsField;

            /// <remarks/>
            [XmlElementAttribute("Grapheme", typeof(PageXmlGrapheme))]
            [XmlElementAttribute("NonPrintingChar", typeof(PageXmlNonPrintingChar))]
            public PageXmlGraphemeBase[] Items
            {
                get
                {
                    return this.itemsField;
                }
                set
                {
                    this.itemsField = value;
                }
            }
        }

        /// <summary>
        /// Represents a sub-element of a glyph. Smallest graphical unit that can be assigned a Unicode code point.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlGrapheme : PageXmlGraphemeBase
        {

            private PageXmlCoords coordsField;

            /// <remarks/>
            public PageXmlCoords Coords
            {
                get
                {
                    return this.coordsField;
                }
                set
                {
                    this.coordsField = value;
                }
            }
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlCoords
        {

            private string pointsField;

            private float confField;

            private bool confFieldSpecified;

            /// <summary>
            /// Polygon outline of the element as a path of points.
            /// No points may lie outside the outline of its parent,
            /// which in the case of Border is the bounding rectangle
            /// of the root image. Paths are closed by convention,
            /// i.e.the last point logically connects with the first
            /// (and at least 3 points are required to span an area).
            /// Paths must be planar (i.e.must not self-intersect).
            /// </summary>
            [XmlAttributeAttribute("points")]
            public string Points
            {
                get
                {
                    return this.pointsField;
                }
                set
                {
                    this.pointsField = value;
                }
            }

            /// <summary>
            /// Confidence value (between 0 and 1)
            /// </summary>
            [XmlAttributeAttribute("conf")]
            public float Conf
            {
                get
                {
                    return this.confField;
                }
                set
                {
                    this.confField = value;
                    this.confFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ConfSpecified
            {
                get
                {
                    return this.confFieldSpecified;
                }
                set
                {
                    this.confFieldSpecified = value;
                }
            }
        }

        /// <summary>
        /// A glyph component without visual representation 
        /// but with Unicode code point.
        /// Non-visual / non-printing / control character.
        /// Part of grapheme container (of glyph) or grapheme sub group.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlNonPrintingChar : PageXmlGraphemeBase
        {
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlGlyph
        {
            #region private
            private PageXmlAlternativeImage[] alternativeImageField;

            private PageXmlCoords coordsField;

            private PageXmlGraphemeBase[] graphemesField;

            private PageXmlTextEquiv[] textEquivField;

            private PageXmlTextStyle textStyleField;

            private PageXmlUserAttribute[] userDefinedField;

            private PageXmlLabels[] labelsField;

            private string idField;

            private bool ligatureField;

            private bool ligatureFieldSpecified;

            private bool symbolField;

            private bool symbolFieldSpecified;

            private PageXmlScriptSimpleType scriptField;

            private bool scriptFieldSpecified;

            private PageXmlProductionSimpleType productionField;

            private bool productionFieldSpecified;

            private string customField;

            private string commentsField;
            #endregion

            /// <summary>
            /// Alternative glyph images (e.g. black-and-white)
            /// </summary>
            [XmlElementAttribute("AlternativeImage")]
            public PageXmlAlternativeImage[] AlternativeImages
            {
                get
                {
                    return this.alternativeImageField;
                }
                set
                {
                    this.alternativeImageField = value;
                }
            }

            /// <remarks/>
            public PageXmlCoords Coords
            {
                get
                {
                    return this.coordsField;
                }
                set
                {
                    this.coordsField = value;
                }
            }

            /// <summary>
            /// Container for graphemes, grapheme groups and non-printing characters
            /// </summary>
            [XmlArrayItemAttribute("Grapheme", typeof(PageXmlGrapheme), IsNullable = false)]
            [XmlArrayItemAttribute("GraphemeGroup", typeof(PageXmlGraphemeGroup), IsNullable = false)]
            [XmlArrayItemAttribute("NonPrintingChar", typeof(PageXmlNonPrintingChar), IsNullable = false)]
            public PageXmlGraphemeBase[] Graphemes
            {
                get
                {
                    return this.graphemesField;
                }
                set
                {
                    this.graphemesField = value;
                }
            }

            /// <remarks/>
            [XmlElementAttribute("TextEquiv")]
            public PageXmlTextEquiv[] TextEquivs
            {
                get
                {
                    return this.textEquivField;
                }
                set
                {
                    this.textEquivField = value;
                }
            }

            /// <remarks/>
            public PageXmlTextStyle TextStyle
            {
                get
                {
                    return this.textStyleField;
                }
                set
                {
                    this.textStyleField = value;
                }
            }

            /// <remarks/>
            [XmlArrayItemAttribute("UserAttribute", IsNullable = false)]
            public PageXmlUserAttribute[] UserDefined
            {
                get
                {
                    return this.userDefinedField;
                }
                set
                {
                    this.userDefinedField = value;
                }
            }

            /// <summary>
            /// Semantic labels / tags
            /// </summary>
            [XmlElementAttribute("Labels")]
            public PageXmlLabels[] Labels
            {
                get
                {
                    return this.labelsField;
                }
                set
                {
                    this.labelsField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("id", DataType = "ID")]
            public string Id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("ligature")]
            public bool Ligature
            {
                get
                {
                    return this.ligatureField;
                }
                set
                {
                    this.ligatureField = value;
                    this.ligatureFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool LigatureSpecified
            {
                get
                {
                    return this.ligatureFieldSpecified;
                }
                set
                {
                    this.ligatureFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("symbol")]
            public bool Symbol
            {
                get
                {
                    return this.symbolField;
                }
                set
                {
                    this.symbolField = value;
                    this.symbolFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool SymbolSpecified
            {
                get
                {
                    return this.symbolFieldSpecified;
                }
                set
                {
                    this.symbolFieldSpecified = value;
                }
            }

            /// <summary>
            /// The script used for the glyph
            /// </summary>
            [XmlAttributeAttribute("script")]
            public PageXmlScriptSimpleType Script
            {
                get
                {
                    return this.scriptField;
                }
                set
                {
                    this.scriptField = value;
                    this.scriptFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ScriptSpecified
            {
                get
                {
                    return this.scriptFieldSpecified;
                }
                set
                {
                    this.scriptFieldSpecified = value;
                }
            }

            /// <summary>
            /// Overrides the production attribute of the parent word / text line / text region.
            /// </summary>
            [XmlAttributeAttribute("production")]
            public PageXmlProductionSimpleType Production
            {
                get
                {
                    return this.productionField;
                }
                set
                {
                    this.productionField = value;
                    this.productionFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ProductionSpecified
            {
                get
                {
                    return this.productionFieldSpecified;
                }
                set
                {
                    this.productionFieldSpecified = value;
                }
            }

            /// <summary>
            /// For generic use
            /// </summary>
            [XmlAttributeAttribute("custom")]
            public string Custom
            {
                get
                {
                    return this.customField;
                }
                set
                {
                    this.customField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("comments")]
            public string Comments
            {
                get
                {
                    return this.commentsField;
                }
                set
                {
                    this.commentsField = value;
                }
            }
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlAlternativeImage
        {

            private string filenameField;

            private string commentsField;

            private float confField;

            private bool confFieldSpecified;

            /// <remarks/>
            [XmlAttributeAttribute("filename")]
            public string FileName
            {
                get
                {
                    return this.filenameField;
                }
                set
                {
                    this.filenameField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("comments")]
            public string Comments
            {
                get
                {
                    return this.commentsField;
                }
                set
                {
                    this.commentsField = value;
                }
            }

            /// <summary>
            /// Confidence value (between 0 and 1)
            /// </summary>
            [XmlAttributeAttribute("conf")]
            public float Conf
            {
                get
                {
                    return this.confField;
                }
                set
                {
                    this.confField = value;
                    this.confFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ConfSpecified
            {
                get
                {
                    return this.confFieldSpecified;
                }
                set
                {
                    this.confFieldSpecified = value;
                }
            }
        }

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

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlLabels
        {

            private PageXmlLabel[] labelField;

            private string externalModelField;

            private string externalIdField;

            private string prefixField;

            private string commentsField;

            /// <summary>
            /// A semantic label / tag
            /// </summary>
            [XmlElementAttribute("Label")]
            public PageXmlLabel[] Labels
            {
                get
                {
                    return this.labelField;
                }
                set
                {
                    this.labelField = value;
                }
            }

            /// <summary>
            /// Reference to external model / ontology / schema
            /// </summary>
            [XmlAttributeAttribute("externalModel")]
            public string ExternalModel
            {
                get
                {
                    return this.externalModelField;
                }
                set
                {
                    this.externalModelField = value;
                }
            }

            /// <summary>
            /// E.g. an RDF resource identifier (to be used as subject or object of an RDF triple)
            /// </summary>
            [XmlAttributeAttribute("externalId")]
            public string ExternalId
            {
                get
                {
                    return this.externalIdField;
                }
                set
                {
                    this.externalIdField = value;
                }
            }

            /// <summary>
            /// Prefix for all labels (e.g. first part of an URI)
            /// </summary>
            [XmlAttributeAttribute("prefix")]
            public string Prefix
            {
                get
                {
                    return this.prefixField;
                }
                set
                {
                    this.prefixField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("comments")]
            public string Comments
            {
                get
                {
                    return this.commentsField;
                }
                set
                {
                    this.commentsField = value;
                }
            }
        }

        /// <summary>
        /// Semantic label
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlLabel
        {

            private string valueField;

            private string typeField;

            private string commentsField;

            /// <summary>
            /// The label / tag (e.g. 'person'). Can be an RDF resource identifier (e.g. object of an RDF triple).
            /// </summary>
            [XmlAttributeAttribute("value")]
            public string Value
            {
                get
                {
                    return this.valueField;
                }
                set
                {
                    this.valueField = value;
                }
            }

            /// <summary>
            /// Additional information on the label (e.g. 'YYYY-mm-dd' for a date label). Can be used as predicate of an RDF triple.
            /// </summary>
            [XmlAttributeAttribute("type")]
            public string Type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("comments")]
            public string Comments
            {
                get
                {
                    return this.commentsField;
                }
                set
                {
                    this.commentsField = value;
                }
            }
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlWord
        {
            #region private
            private PageXmlAlternativeImage[] alternativeImageField;

            private PageXmlCoords coordsField;

            private PageXmlGlyph[] glyphField;

            private PageXmlTextEquiv[] textEquivField;

            private PageXmlTextStyle textStyleField;

            private PageXmlUserAttribute[] userDefinedField;

            private PageXmlLabels[] labelsField;

            private string idField;

            private PageXmlLanguageSimpleType languageField;

            private bool languageFieldSpecified;

            private PageXmlScriptSimpleType primaryScriptField;

            private bool primaryScriptFieldSpecified;

            private PageXmlScriptSimpleType secondaryScriptField;

            private bool secondaryScriptFieldSpecified;

            private PageXmlReadingDirectionSimpleType readingDirectionField;

            private bool readingDirectionFieldSpecified;

            private PageXmlProductionSimpleType productionField;

            private bool productionFieldSpecified;

            private string customField;

            private string commentsField;
            #endregion

            /// <summary>
            /// Alternative word images (e.g. black-and-white)
            /// </summary>
            [XmlElementAttribute("AlternativeImage")]
            public PageXmlAlternativeImage[] AlternativeImages
            {
                get
                {
                    return this.alternativeImageField;
                }
                set
                {
                    this.alternativeImageField = value;
                }
            }

            /// <remarks/>
            public PageXmlCoords Coords
            {
                get
                {
                    return this.coordsField;
                }
                set
                {
                    this.coordsField = value;
                }
            }

            /// <remarks/>
            [XmlElementAttribute("Glyph")]
            public PageXmlGlyph[] Glyphs
            {
                get
                {
                    return this.glyphField;
                }
                set
                {
                    this.glyphField = value;
                }
            }

            /// <remarks/>
            [XmlElementAttribute("TextEquiv")]
            public PageXmlTextEquiv[] TextEquivs
            {
                get
                {
                    return this.textEquivField;
                }
                set
                {
                    this.textEquivField = value;
                }
            }

            /// <remarks/>
            public PageXmlTextStyle TextStyle
            {
                get
                {
                    return this.textStyleField;
                }
                set
                {
                    this.textStyleField = value;
                }
            }

            /// <remarks/>
            [XmlArrayItemAttribute("UserAttribute", IsNullable = false)]
            public PageXmlUserAttribute[] UserDefined
            {
                get
                {
                    return this.userDefinedField;
                }
                set
                {
                    this.userDefinedField = value;
                }
            }

            /// <summary>
            /// Semantic labels / tags
            /// </summary>
            [XmlElementAttribute("Labels")]
            public PageXmlLabels[] Labels
            {
                get
                {
                    return this.labelsField;
                }
                set
                {
                    this.labelsField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("id", DataType = "ID")]
            public string Id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }

            /// <summary>
            /// Overrides primaryLanguage attribute of parent line and/or text region
            /// </summary>
            [XmlAttributeAttribute("language")]
            public PageXmlLanguageSimpleType Language
            {
                get
                {
                    return this.languageField;
                }
                set
                {
                    this.languageField = value;
                    this.languageFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool LanguageSpecified
            {
                get
                {
                    return this.languageFieldSpecified;
                }
                set
                {
                    this.languageFieldSpecified = value;
                }
            }

            /// <summary>
            /// The primary script used in the word
            /// </summary>
            [XmlAttributeAttribute("primaryScript")]
            public PageXmlScriptSimpleType PrimaryScript
            {
                get
                {
                    return this.primaryScriptField;
                }
                set
                {
                    this.primaryScriptField = value;
                    this.primaryScriptFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool PrimaryScriptSpecified
            {
                get
                {
                    return this.primaryScriptFieldSpecified;
                }
                set
                {
                    this.primaryScriptFieldSpecified = value;
                }
            }

            /// <summary>
            /// The secondary script used in the word 
            /// </summary>
            [XmlAttributeAttribute("secondaryScript")]
            public PageXmlScriptSimpleType SecondaryScript
            {
                get
                {
                    return this.secondaryScriptField;
                }
                set
                {
                    this.secondaryScriptField = value;
                    this.secondaryScriptFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool SecondaryScriptSpecified
            {
                get
                {
                    return this.secondaryScriptFieldSpecified;
                }
                set
                {
                    this.secondaryScriptFieldSpecified = value;
                }
            }

            /// <summary>
            /// The direction in which text within the word should be read(order of characters).
            /// </summary>
            [XmlAttributeAttribute("readingDirection")]
            public PageXmlReadingDirectionSimpleType ReadingDirection
            {
                get
                {
                    return this.readingDirectionField;
                }
                set
                {
                    this.readingDirectionField = value;
                    this.readingDirectionFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ReadingDirectionSpecified
            {
                get
                {
                    return this.readingDirectionFieldSpecified;
                }
                set
                {
                    this.readingDirectionFieldSpecified = value;
                }
            }

            /// <summary>
            /// Overrides the production attribute of the parent text line and/or text region.
            /// </summary>
            [XmlAttributeAttribute("production")]
            public PageXmlProductionSimpleType Production
            {
                get
                {
                    return this.productionField;
                }
                set
                {
                    this.productionField = value;
                    this.productionFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ProductionSpecified
            {
                get
                {
                    return this.productionFieldSpecified;
                }
                set
                {
                    this.productionFieldSpecified = value;
                }
            }

            /// <summary>
            /// For generic use
            /// </summary>
            [XmlAttributeAttribute("custom")]
            public string Custom
            {
                get
                {
                    return this.customField;
                }
                set
                {
                    this.customField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("comments")]
            public string Comments
            {
                get
                {
                    return this.commentsField;
                }
                set
                {
                    this.commentsField = value;
                }
            }

            /// <remarks/>
            public override string ToString()
            {
                return string.Join("\n", this.TextEquivs.Select(t => t.Unicode));
            }
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlBaseline
        {

            private string pointsField;

            private float confField;

            private bool confFieldSpecified;

            /// <remarks/>
            [XmlAttributeAttribute("points")]
            public string Points
            {
                get
                {
                    return this.pointsField;
                }
                set
                {
                    this.pointsField = value;
                }
            }

            /// <summary>
            /// Confidence value (between 0 and 1)
            /// </summary>
            [XmlAttributeAttribute("conf")]
            public float Conf
            {
                get
                {
                    return this.confField;
                }
                set
                {
                    this.confField = value;
                    this.confFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ConfSpecified
            {
                get
                {
                    return this.confFieldSpecified;
                }
                set
                {
                    this.confFieldSpecified = value;
                }
            }
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlTextLine
        {
            #region private
            private PageXmlAlternativeImage[] alternativeImageField;

            private PageXmlCoords coordsField;

            private PageXmlBaseline baselineField;

            private PageXmlWord[] wordField;

            private PageXmlTextEquiv[] textEquivField;

            private PageXmlTextStyle textStyleField;

            private PageXmlUserAttribute[] userDefinedField;

            private PageXmlLabels[] labelsField;

            private string idField;

            private PageXmlLanguageSimpleType primaryLanguageField;

            private bool primaryLanguageFieldSpecified;

            private PageXmlScriptSimpleType primaryScriptField;

            private bool primaryScriptFieldSpecified;

            private PageXmlScriptSimpleType secondaryScriptField;

            private bool secondaryScriptFieldSpecified;

            private PageXmlReadingDirectionSimpleType readingDirectionField;

            private bool readingDirectionFieldSpecified;

            private PageXmlProductionSimpleType productionField;

            private bool productionFieldSpecified;

            private string customField;

            private string commentsField;

            private int indexField;

            private bool indexFieldSpecified;
            #endregion

            /// <summary>
            /// Alternative text line images (e.g. black-and-white)
            /// </summary>
            [XmlElementAttribute("AlternativeImage")]
            public PageXmlAlternativeImage[] AlternativeImages
            {
                get
                {
                    return this.alternativeImageField;
                }
                set
                {
                    this.alternativeImageField = value;
                }
            }

            /// <remarks/>
            public PageXmlCoords Coords
            {
                get
                {
                    return this.coordsField;
                }
                set
                {
                    this.coordsField = value;
                }
            }

            /// <summary>
            /// Multiple connected points that mark the baseline of the glyphs
            /// </summary>
            public PageXmlBaseline Baseline
            {
                get
                {
                    return this.baselineField;
                }
                set
                {
                    this.baselineField = value;
                }
            }

            /// <remarks/>
            [XmlElementAttribute("Word")]
            public PageXmlWord[] Words
            {
                get
                {
                    return this.wordField;
                }
                set
                {
                    this.wordField = value;
                }
            }

            /// <remarks/>
            [XmlElementAttribute("TextEquiv")]
            public PageXmlTextEquiv[] TextEquivs
            {
                get
                {
                    return this.textEquivField;
                }
                set
                {
                    this.textEquivField = value;
                }
            }

            /// <remarks/>
            public PageXmlTextStyle TextStyle
            {
                get
                {
                    return this.textStyleField;
                }
                set
                {
                    this.textStyleField = value;
                }
            }

            /// <remarks/>
            [XmlArrayItemAttribute("UserAttribute", IsNullable = false)]
            public PageXmlUserAttribute[] UserDefined
            {
                get
                {
                    return this.userDefinedField;
                }
                set
                {
                    this.userDefinedField = value;
                }
            }

            /// <summary>
            /// Semantic labels / tags
            /// </summary>
            [XmlElementAttribute("Labels")]
            public PageXmlLabels[] Labels
            {
                get
                {
                    return this.labelsField;
                }
                set
                {
                    this.labelsField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("id", DataType = "ID")]
            public string Id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("primaryLanguage")]
            public PageXmlLanguageSimpleType PrimaryLanguage
            {
                get
                {
                    return this.primaryLanguageField;
                }
                set
                {
                    this.primaryLanguageField = value;
                    this.primaryLanguageFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool PrimaryLanguageSpecified
            {
                get
                {
                    return this.primaryLanguageFieldSpecified;
                }
                set
                {
                    this.primaryLanguageFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("primaryScript")]
            public PageXmlScriptSimpleType PrimaryScript
            {
                get
                {
                    return this.primaryScriptField;
                }
                set
                {
                    this.primaryScriptField = value;
                    this.primaryScriptFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool PrimaryScriptSpecified
            {
                get
                {
                    return this.primaryScriptFieldSpecified;
                }
                set
                {
                    this.primaryScriptFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("secondaryScript")]
            public PageXmlScriptSimpleType SecondaryScript
            {
                get
                {
                    return this.secondaryScriptField;
                }
                set
                {
                    this.secondaryScriptField = value;
                    this.secondaryScriptFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool SecondaryScriptSpecified
            {
                get
                {
                    return this.secondaryScriptFieldSpecified;
                }
                set
                {
                    this.secondaryScriptFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("readingDirection")]
            public PageXmlReadingDirectionSimpleType ReadingDirection
            {
                get
                {
                    return this.readingDirectionField;
                }
                set
                {
                    this.readingDirectionField = value;
                    this.readingDirectionFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ReadingDirectionSpecified
            {
                get
                {
                    return this.readingDirectionFieldSpecified;
                }
                set
                {
                    this.readingDirectionFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("production")]
            public PageXmlProductionSimpleType Production
            {
                get
                {
                    return this.productionField;
                }
                set
                {
                    this.productionField = value;
                    this.productionFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ProductionSpecified
            {
                get
                {
                    return this.productionFieldSpecified;
                }
                set
                {
                    this.productionFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("custom")]
            public string Custom
            {
                get
                {
                    return this.customField;
                }
                set
                {
                    this.customField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("comments")]
            public string Comments
            {
                get
                {
                    return this.commentsField;
                }
                set
                {
                    this.commentsField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute()]
            public int Index
            {
                get
                {
                    return this.indexField;
                }
                set
                {
                    this.indexField = value;
                    this.indexFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool IndexSpecified
            {
                get
                {
                    return this.indexFieldSpecified;
                }
                set
                {
                    this.indexFieldSpecified = value;
                }
            }

            /// <remarks/>
            public override string ToString()
            {
                return string.Join("\n", this.TextEquivs.Select(t => t.Unicode));
            }
        }

        /// <summary>
        /// Data for a region that takes on the role of a table cell within a parent table region.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlTableCellRole
        {

            private int rowIndexField;

            private int columnIndexField;

            private int rowSpanField;

            private bool rowSpanFieldSpecified;

            private int colSpanField;

            private bool colSpanFieldSpecified;

            private bool headerField;

            private bool headerFieldSpecified;

            /// <summary>
            /// Cell position in table starting with row 0
            /// </summary>
            [XmlAttributeAttribute("rowIndex")]
            public int RowIndex
            {
                get
                {
                    return this.rowIndexField;
                }
                set
                {
                    this.rowIndexField = value;
                }
            }

            /// <summary>
            /// Cell position in table starting with column 0
            /// </summary>
            [XmlAttributeAttribute("columnIndex")]
            public int ColumnIndex
            {
                get
                {
                    return this.columnIndexField;
                }
                set
                {
                    this.columnIndexField = value;
                }
            }

            /// <summary>
            /// Number of rows the cell spans (optional; default is 1)
            /// </summary>
            [XmlAttributeAttribute("rowSpan")]
            public int RowSpan
            {
                get
                {
                    return this.rowSpanField;
                }
                set
                {
                    this.rowSpanField = value;
                    this.rowSpanFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool RowSpanSpecified
            {
                get
                {
                    return this.rowSpanFieldSpecified;
                }
                set
                {
                    this.rowSpanFieldSpecified = value;
                }
            }

            /// <summary>
            /// Number of columns the cell spans (optional; default is 1)
            /// </summary>
            [XmlAttributeAttribute("colSpan")]
            public int ColSpan
            {
                get
                {
                    return this.colSpanField;
                }
                set
                {
                    this.colSpanField = value;
                    this.colSpanFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ColSpanSpecified
            {
                get
                {
                    return this.colSpanFieldSpecified;
                }
                set
                {
                    this.colSpanFieldSpecified = value;
                }
            }

            /// <summary>
            /// Is the cell a column or row header?
            /// </summary>
            [XmlAttributeAttribute("header")]
            public bool Header
            {
                get
                {
                    return this.headerField;
                }
                set
                {
                    this.headerField = value;
                    this.headerFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool HeaderSpecified
            {
                get
                {
                    return this.headerFieldSpecified;
                }
                set
                {
                    this.headerFieldSpecified = value;
                }
            }
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlRoles
        {

            private PageXmlTableCellRole tableCellRoleField;

            /// <summary>
            /// Data for a region that takes on the role of a table cell within a parent table region.
            /// </summary>
            public PageXmlTableCellRole TableCellRole
            {
                get
                {
                    return this.tableCellRoleField;
                }
                set
                {
                    this.tableCellRoleField = value;
                }
            }
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [XmlIncludeAttribute(typeof(PageXmlMapRegion))]
        [XmlIncludeAttribute(typeof(PageXmlCustomRegion))]
        [XmlIncludeAttribute(typeof(PageXmlUnknownRegion))]
        [XmlIncludeAttribute(typeof(PageXmlNoiseRegion))]
        [XmlIncludeAttribute(typeof(PageXmlAdvertRegion))]
        [XmlIncludeAttribute(typeof(PageXmlMusicRegion))]
        [XmlIncludeAttribute(typeof(PageXmlChemRegion))]
        [XmlIncludeAttribute(typeof(PageXmlMathsRegion))]
        [XmlIncludeAttribute(typeof(PageXmlSeparatorRegion))]
        [XmlIncludeAttribute(typeof(PageXmlChartRegion))]
        [XmlIncludeAttribute(typeof(PageXmlTableRegion))]
        [XmlIncludeAttribute(typeof(PageXmlGraphicRegion))]
        [XmlIncludeAttribute(typeof(PageXmlLineDrawingRegion))]
        [XmlIncludeAttribute(typeof(PageXmlImageRegion))]
        [XmlIncludeAttribute(typeof(PageXmlTextRegion))]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public abstract class PageXmlRegion
        {
            #region private
            private PageXmlAlternativeImage[] alternativeImageField;

            private PageXmlCoords coordsField;

            private PageXmlUserAttribute[] userDefinedField;

            private PageXmlLabels[] labelsField;

            private PageXmlRoles rolesField;

            private PageXmlRegion[] itemsField;

            private string idField;

            private string customField;

            private string commentsField;

            private bool continuationField;

            private bool continuationFieldSpecified;
            #endregion

            /// <summary>
            /// Alternative region images (e.g.black-and-white).
            /// </summary>
            [XmlElementAttribute("AlternativeImage")]
            public PageXmlAlternativeImage[] AlternativeImage
            {
                get
                {
                    return this.alternativeImageField;
                }
                set
                {
                    this.alternativeImageField = value;
                }
            }

            /// <remarks/>
            public PageXmlCoords Coords
            {
                get
                {
                    return this.coordsField;
                }
                set
                {
                    this.coordsField = value;
                }
            }

            /// <remarks/>
            [XmlArrayItemAttribute("UserAttribute", IsNullable = false)]
            public PageXmlUserAttribute[] UserDefined
            {
                get
                {
                    return this.userDefinedField;
                }
                set
                {
                    this.userDefinedField = value;
                }
            }

            /// <summary>
            /// Semantic labels / tags
            /// </summary>
            [XmlElementAttribute("Labels")]
            public PageXmlLabels[] Labels
            {
                get
                {
                    return this.labelsField;
                }
                set
                {
                    this.labelsField = value;
                }
            }

            /// <summary>
            /// Roles the region takes (e.g. in context of a parent region).
            /// </summary>
            public PageXmlRoles Roles
            {
                get
                {
                    return this.rolesField;
                }
                set
                {
                    this.rolesField = value;
                }
            }

            /// <remarks/>
            [XmlElementAttribute("AdvertRegion", typeof(PageXmlAdvertRegion))]
            [XmlElementAttribute("ChartRegion", typeof(PageXmlChartRegion))]
            [XmlElementAttribute("ChemRegion", typeof(PageXmlChemRegion))]
            [XmlElementAttribute("CustomRegion", typeof(PageXmlCustomRegion))]
            [XmlElementAttribute("GraphicRegion", typeof(PageXmlGraphicRegion))]
            [XmlElementAttribute("ImageRegion", typeof(PageXmlImageRegion))]
            [XmlElementAttribute("LineDrawingRegion", typeof(PageXmlLineDrawingRegion))]
            [XmlElementAttribute("MathsRegion", typeof(PageXmlMathsRegion))]
            [XmlElementAttribute("MusicRegion", typeof(PageXmlMusicRegion))]
            [XmlElementAttribute("NoiseRegion", typeof(PageXmlNoiseRegion))]
            [XmlElementAttribute("SeparatorRegion", typeof(PageXmlSeparatorRegion))]
            [XmlElementAttribute("TableRegion", typeof(PageXmlTableRegion))]
            [XmlElementAttribute("TextRegion", typeof(PageXmlTextRegion))]
            [XmlElementAttribute("UnknownRegion", typeof(PageXmlUnknownRegion))]
            public PageXmlRegion[] Items
            {
                get
                {
                    return this.itemsField;
                }
                set
                {
                    this.itemsField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("id", DataType = "ID")]
            public string Id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }


            /// <summary>
            /// For generic use
            /// </summary>
            [XmlAttributeAttribute("custom")]
            public string Custom
            {
                get
                {
                    return this.customField;
                }
                set
                {
                    this.customField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("comments")]
            public string Comments
            {
                get
                {
                    return this.commentsField;
                }
                set
                {
                    this.commentsField = value;
                }
            }

            /// <summary>
            /// Is this region a continuation of another region
            /// (in previous column or page, for example)?
            /// </summary>
            [XmlAttributeAttribute("continuation")]
            public bool Continuation
            {
                get
                {
                    return this.continuationField;
                }
                set
                {
                    this.continuationField = value;
                    this.continuationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ContinuationSpecified
            {
                get
                {
                    return this.continuationFieldSpecified;
                }
                set
                {
                    this.continuationFieldSpecified = value;
                }
            }
        }

        /// <summary>
        /// Regions containing advertisements.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlAdvertRegion : PageXmlRegion
        {

            private float orientationField;

            private bool orientationFieldSpecified;

            private PageXmlColourSimpleType bgColourField;

            private bool bgColourFieldSpecified;

            /// <summary>
            /// The angle the rectangle encapsulating a region
            /// has to be rotated in clockwise direction
            /// in order to correct the present skew
            /// (negative values indicate anti-clockwise rotation).
            /// Range: -179.999,180
            /// </summary>
            [XmlAttributeAttribute("orientation")]
            public float Orientation
            {
                get
                {
                    return this.orientationField;
                }
                set
                {
                    this.orientationField = value;
                    this.orientationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool OrientationSpecified
            {
                get
                {
                    return this.orientationFieldSpecified;
                }
                set
                {
                    this.orientationFieldSpecified = value;
                }
            }

            /// <summary>
            /// The background colour of the region
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
        }

        /// <summary>
        /// Regions containing charts or graphs of any type, should be marked as chart regions.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlChartRegion : PageXmlRegion
        {
            #region private    
            private float orientationField;

            private bool orientationFieldSpecified;

            private PageXmlChartSimpleType typeField;

            private bool typeFieldSpecified;

            private int numColoursField;

            private bool numColoursFieldSpecified;

            private PageXmlColourSimpleType bgColourField;

            private bool bgColourFieldSpecified;

            private bool embTextField;

            private bool embTextFieldSpecified;
            #endregion

            /// <summary>
            /// The angle the rectangle encapsulating a region
            /// has to be rotated in clockwise direction
            /// in order to correct the present skew
            /// (negative values indicate anti-clockwise rotation).
            /// Range: -179.999,180
            /// </summary>
            [XmlAttributeAttribute("orientation")]
            public float Orientation
            {
                get
                {
                    return this.orientationField;
                }
                set
                {
                    this.orientationField = value;
                    this.orientationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool OrientationSpecified
            {
                get
                {
                    return this.orientationFieldSpecified;
                }
                set
                {
                    this.orientationFieldSpecified = value;
                }
            }

            /// <summary>
            /// The type of chart in the region
            /// </summary>
            [XmlAttributeAttribute("type")]
            public PageXmlChartSimpleType Type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                    this.typeFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool TypeSpecified
            {
                get
                {
                    return this.typeFieldSpecified;
                }
                set
                {
                    this.typeFieldSpecified = value;
                }
            }

            /// <summary>
            /// An approximation of the number of colours used in the region
            /// </summary>
            [XmlAttributeAttribute("numColours")]
            public int NumColours
            {
                get
                {
                    return this.numColoursField;
                }
                set
                {
                    this.numColoursField = value;
                    this.numColoursFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool NumColoursSpecified
            {
                get
                {
                    return this.numColoursFieldSpecified;
                }
                set
                {
                    this.numColoursFieldSpecified = value;
                }
            }

            /// <summary>
            /// The background colour of the region
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
            /// Specifies whether the region also contains text
            /// </summary>
            [XmlAttributeAttribute("embText")]
            public bool EmbText
            {
                get
                {
                    return this.embTextField;
                }
                set
                {
                    this.embTextField = value;
                    this.embTextFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool EmbTextSpecified
            {
                get
                {
                    return this.embTextFieldSpecified;
                }
                set
                {
                    this.embTextFieldSpecified = value;
                }
            }
        }

        /// <summary>
        /// Regions containing chemical formulas.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlChemRegion : PageXmlRegion
        {

            private float orientationField;

            private bool orientationFieldSpecified;

            private PageXmlColourSimpleType bgColourField;

            private bool bgColourFieldSpecified;

            /// <summary>
            /// The angle the rectangle encapsulating a
            /// region has to be rotated in clockwise
            /// direction in order to correct the present
            /// skew(negative values indicate
            /// anti-clockwise rotation). Range:
            /// -179.999,180
            /// </summary>
            [XmlAttributeAttribute("orientation")]
            public float Orientation
            {
                get
                {
                    return this.orientationField;
                }
                set
                {
                    this.orientationField = value;
                    this.orientationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool OrientationSpecified
            {
                get
                {
                    return this.orientationFieldSpecified;
                }
                set
                {
                    this.orientationFieldSpecified = value;
                }
            }

            /// <summary>
            /// The background colour of the region
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
        }

        /// <summary>
        /// Regions containing content that is not covered
        /// by the default types(text, graphic, image,
        /// line drawing, chart, table, separator, maths,
        /// map, music, chem, advert, noise, unknown).
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlCustomRegion : PageXmlRegion
        {

            private string typeField;

            /// <summary>
            /// Information on the type of content represented by this region
            /// </summary>
            [XmlAttributeAttribute("type")]
            public string Type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                }
            }
        }

        /// <summary>
        /// Regions containing simple graphics, such as a company
        /// logo, should be marked as graphic regions.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlGraphicRegion : PageXmlRegion
        {
            #region private
            private float orientationField;

            private bool orientationFieldSpecified;

            private PageXmlGraphicsSimpleType typeField;

            private bool typeFieldSpecified;

            private int numColoursField;

            private bool numColoursFieldSpecified;

            private bool embTextField;

            private bool embTextFieldSpecified;
            #endregion

            /// <summary>
            /// The angle the rectangle encapsulating a region
            /// has to be rotated in clockwise direction
            /// in order to correct the present skew
            /// (negative values indicate anti-clockwise rotation).
            /// Range: -179.999,180
            /// </summary>
            [XmlAttributeAttribute("orientation")]
            public float Orientation
            {
                get
                {
                    return this.orientationField;
                }
                set
                {
                    this.orientationField = value;
                    this.orientationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool OrientationSpecified
            {
                get
                {
                    return this.orientationFieldSpecified;
                }
                set
                {
                    this.orientationFieldSpecified = value;
                }
            }

            /// <summary>
            /// The type of graphic in the region
            /// </summary>
            [XmlAttributeAttribute("type")]
            public PageXmlGraphicsSimpleType Type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                    this.typeFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool TypeSpecified
            {
                get
                {
                    return this.typeFieldSpecified;
                }
                set
                {
                    this.typeFieldSpecified = value;
                }
            }

            /// <summary>
            /// An approximation of the number of colours used in the region
            /// </summary>
            [XmlAttributeAttribute("numColours")]
            public int NumColours
            {
                get
                {
                    return this.numColoursField;
                }
                set
                {
                    this.numColoursField = value;
                    this.numColoursFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool NumColoursSpecified
            {
                get
                {
                    return this.numColoursFieldSpecified;
                }
                set
                {
                    this.numColoursFieldSpecified = value;
                }
            }

            /// <summary>
            /// Specifies whether the region also contains text.
            /// </summary>
            [XmlAttributeAttribute("embText")]
            public bool EmbText
            {
                get
                {
                    return this.embTextField;
                }
                set
                {
                    this.embTextField = value;
                    this.embTextFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool EmbTextSpecified
            {
                get
                {
                    return this.embTextFieldSpecified;
                }
                set
                {
                    this.embTextFieldSpecified = value;
                }
            }
        }

        /// <summary>
        /// An image is considered to be more intricate and complex than a graphic. These can be photos or drawings.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlImageRegion : PageXmlRegion
        {
            #region private
            private float orientationField;

            private bool orientationFieldSpecified;

            private PageXmlColourDepthSimpleType colourDepthField;

            private bool colourDepthFieldSpecified;

            private PageXmlColourSimpleType bgColourField;

            private bool bgColourFieldSpecified;

            private bool embTextField;

            private bool embTextFieldSpecified;
            #endregion

            /// <summary>
            /// The angle the rectangle encapsulating a region
            /// has to be rotated in clockwise direction
            /// in order to correct the present skew
            /// (negative values indicate anti-clockwise rotation).
            /// Range: -179.999,180
            /// </summary>
            [XmlAttributeAttribute("orientation")]
            public float Orientation
            {
                get
                {
                    return this.orientationField;
                }
                set
                {
                    this.orientationField = value;
                    this.orientationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool OrientationSpecified
            {
                get
                {
                    return this.orientationFieldSpecified;
                }
                set
                {
                    this.orientationFieldSpecified = value;
                }
            }

            /// <summary>
            /// The colour bit depth required for the region
            /// </summary>
            [XmlAttributeAttribute("colourDepth")]
            public PageXmlColourDepthSimpleType ColourDepth
            {
                get
                {
                    return this.colourDepthField;
                }
                set
                {
                    this.colourDepthField = value;
                    this.colourDepthFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ColourDepthSpecified
            {
                get
                {
                    return this.colourDepthFieldSpecified;
                }
                set
                {
                    this.colourDepthFieldSpecified = value;
                }
            }

            /// <summary>
            /// The background colour of the region
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
            /// Specifies whether the region also contains text
            /// </summary>
            [XmlAttributeAttribute("embText")]
            public bool EmbText
            {
                get
                {
                    return this.embTextField;
                }
                set
                {
                    this.embTextField = value;
                    this.embTextFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool EmbTextSpecified
            {
                get
                {
                    return this.embTextFieldSpecified;
                }
                set
                {
                    this.embTextFieldSpecified = value;
                }
            }
        }

        /// <summary>
        /// A line drawing is a single colour illustration without solid areas.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlLineDrawingRegion : PageXmlRegion
        {
            #region private
            private float orientationField;

            private bool orientationFieldSpecified;

            private PageXmlColourSimpleType penColourField;

            private bool penColourFieldSpecified;

            private PageXmlColourSimpleType bgColourField;

            private bool bgColourFieldSpecified;

            private bool embTextField;

            private bool embTextFieldSpecified;
            #endregion

            /// <summary>
            /// The angle the rectangle encapsulating a region
            /// has to be rotated in clockwise direction
            /// in order to correct the present skew
            /// (negative values indicate anti-clockwise rotation).
            /// Range: -179.999,180
            /// </summary>
            [XmlAttributeAttribute("orientation")]
            public float Orientation
            {
                get
                {
                    return this.orientationField;
                }
                set
                {
                    this.orientationField = value;
                    this.orientationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool OrientationSpecified
            {
                get
                {
                    return this.orientationFieldSpecified;
                }
                set
                {
                    this.orientationFieldSpecified = value;
                }
            }

            /// <summary>
            /// The pen (foreground) colour of the region
            /// </summary>
            [XmlAttributeAttribute("penColour")]
            public PageXmlColourSimpleType PenColour
            {
                get
                {
                    return this.penColourField;
                }
                set
                {
                    this.penColourField = value;
                    this.penColourFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool PenColourSpecified
            {
                get
                {
                    return this.penColourFieldSpecified;
                }
                set
                {
                    this.penColourFieldSpecified = value;
                }
            }

            /// <summary>
            /// The background colour of the region
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
            /// Specifies whether the region also contains text
            /// </summary>
            [XmlAttributeAttribute("embText")]
            public bool EmbText
            {
                get
                {
                    return this.embTextField;
                }
                set
                {
                    this.embTextField = value;
                    this.embTextFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool EmbTextSpecified
            {
                get
                {
                    return this.embTextFieldSpecified;
                }
                set
                {
                    this.embTextFieldSpecified = value;
                }
            }
        }

        /// <summary>
        /// Regions containing equations and mathematical symbols should be marked as maths regions.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlMathsRegion : PageXmlRegion
        {

            private float orientationField;

            private bool orientationFieldSpecified;

            private PageXmlColourSimpleType bgColourField;

            private bool bgColourFieldSpecified;

            /// <summary>
            /// The angle the rectangle encapsulating a region
            /// has to be rotated in clockwise direction
            /// in order to correct the present skew
            /// (negative values indicate anti-clockwise rotation).
            /// Range: -179.999,180
            /// </summary>
            [XmlAttributeAttribute("orientation")]
            public float Orientation
            {
                get
                {
                    return this.orientationField;
                }
                set
                {
                    this.orientationField = value;
                    this.orientationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool OrientationSpecified
            {
                get
                {
                    return this.orientationFieldSpecified;
                }
                set
                {
                    this.orientationFieldSpecified = value;
                }
            }

            /// <summary>
            /// The background colour of the region
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
        }

        /// <summary>
        /// Regions containing musical notations.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlMusicRegion : PageXmlRegion
        {

            private float orientationField;

            private bool orientationFieldSpecified;

            private PageXmlColourSimpleType bgColourField;

            private bool bgColourFieldSpecified;

            /// <summary>
            /// The angle the rectangle encapsulating a region
            /// has to be rotated in clockwise direction
            /// in order to correct the present skew
            /// (negative values indicate anti-clockwise rotation).
            /// Range: -179.999,180
            /// </summary>
            [XmlAttributeAttribute("orientation")]
            public float Orientation
            {
                get
                {
                    return this.orientationField;
                }
                set
                {
                    this.orientationField = value;
                    this.orientationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool OrientationSpecified
            {
                get
                {
                    return this.orientationFieldSpecified;
                }
                set
                {
                    this.orientationFieldSpecified = value;
                }
            }

            /// <summary>
            /// The background colour of the region
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
        }

        /// <summary>
        /// Noise regions are regions where no real data lies, only
        /// false data created by artifacts on the document or
        /// scanner noise.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlNoiseRegion : PageXmlRegion
        {
        }

        /// <summary>
        /// Separators are lines that lie between columns and 
        /// paragraphs and can be used to logically separate
        /// different articles from each other.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlSeparatorRegion : PageXmlRegion
        {

            private float orientationField;

            private bool orientationFieldSpecified;

            private PageXmlColourSimpleType colourField;

            private bool colourFieldSpecified;

            /// <summary>
            /// The angle the rectangle encapsulating a region
            /// has to be rotated in clockwise direction
            /// in order to correct the present skew
            /// (negative values indicate anti-clockwise rotation).
            /// Range: -179.999,180
            /// </summary>
            [XmlAttributeAttribute("orientation")]
            public float Orientation
            {
                get
                {
                    return this.orientationField;
                }
                set
                {
                    this.orientationField = value;
                    this.orientationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool OrientationSpecified
            {
                get
                {
                    return this.orientationFieldSpecified;
                }
                set
                {
                    this.orientationFieldSpecified = value;
                }
            }

            /// <summary>
            /// The colour of the separator
            /// </summary>
            [XmlAttributeAttribute("colour")]
            public PageXmlColourSimpleType Colour
            {
                get
                {
                    return this.colourField;
                }
                set
                {
                    this.colourField = value;
                    this.colourFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ColourSpecified
            {
                get
                {
                    return this.colourFieldSpecified;
                }
                set
                {
                    this.colourFieldSpecified = value;
                }
            }
        }

        /// <summary>
        /// Tabular data in any form is represented with a table
        /// region.Rows and columns may or may not have separator
        /// lines; these lines are not separator regions.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlTableRegion : PageXmlRegion
        {
            #region private
            private PageXmlGridPoints[] gridField;

            private float orientationField;

            private bool orientationFieldSpecified;

            private int rowsField;

            private bool rowsFieldSpecified;

            private int columnsField;

            private bool columnsFieldSpecified;

            private PageXmlColourSimpleType lineColourField;

            private bool lineColourFieldSpecified;

            private PageXmlColourSimpleType bgColourField;

            private bool bgColourFieldSpecified;

            private bool lineSeparatorsField;

            private bool lineSeparatorsFieldSpecified;

            private bool embTextField;

            private bool embTextFieldSpecified;
            #endregion

            /// <summary>
            /// Table grid (visible or virtual grid lines)
            /// </summary>
            [XmlArrayItemAttribute("GridPoints", IsNullable = false)]
            public PageXmlGridPoints[] Grid
            {
                get
                {
                    return this.gridField;
                }
                set
                {
                    this.gridField = value;
                }
            }

            /// <summary>
            /// The angle the rectangle encapsulating a	region
            /// has to be rotated in clockwise direction
            /// in order to correct the present skew
            /// (negative values indicate anti-clockwise rotation).
            /// Range: -179.999,180
            /// </summary>
            [XmlAttributeAttribute("orientation")]
            public float Orientation
            {
                get
                {
                    return this.orientationField;
                }
                set
                {
                    this.orientationField = value;
                    this.orientationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool OrientationSpecified
            {
                get
                {
                    return this.orientationFieldSpecified;
                }
                set
                {
                    this.orientationFieldSpecified = value;
                }
            }

            /// <summary>
            /// The number of rows present in the table
            /// </summary>
            [XmlAttributeAttribute("rows")]
            public int Rows
            {
                get
                {
                    return this.rowsField;
                }
                set
                {
                    this.rowsField = value;
                    this.rowsFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool RowsSpecified
            {
                get
                {
                    return this.rowsFieldSpecified;
                }
                set
                {
                    this.rowsFieldSpecified = value;
                }
            }

            /// <summary>
            /// The number of columns present in the table
            /// </summary>
            [XmlAttributeAttribute("columns")]
            public int Columns
            {
                get
                {
                    return this.columnsField;
                }
                set
                {
                    this.columnsField = value;
                    this.columnsFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ColumnsSpecified
            {
                get
                {
                    return this.columnsFieldSpecified;
                }
                set
                {
                    this.columnsFieldSpecified = value;
                }
            }

            /// <summary>
            /// The colour of the lines used in the region
            /// </summary>
            [XmlAttributeAttribute("lineColour")]
            public PageXmlColourSimpleType LineColour
            {
                get
                {
                    return this.lineColourField;
                }
                set
                {
                    this.lineColourField = value;
                    this.lineColourFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool LineColourSpecified
            {
                get
                {
                    return this.lineColourFieldSpecified;
                }
                set
                {
                    this.lineColourFieldSpecified = value;
                }
            }

            /// <summary>
            /// The background colour of the region
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
            /// Specifies the presence of line separators
            /// </summary>
            [XmlAttributeAttribute("lineSeparators")]
            public bool LineSeparators
            {
                get
                {
                    return this.lineSeparatorsField;
                }
                set
                {
                    this.lineSeparatorsField = value;
                    this.lineSeparatorsFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool LineSeparatorsSpecified
            {
                get
                {
                    return this.lineSeparatorsFieldSpecified;
                }
                set
                {
                    this.lineSeparatorsFieldSpecified = value;
                }
            }

            /// <summary>
            /// Specifies whether the region also contains text
            /// </summary>
            [XmlAttributeAttribute("embText")]
            public bool EmbText
            {
                get
                {
                    return this.embTextField;
                }
                set
                {
                    this.embTextField = value;
                    this.embTextFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool EmbTextSpecified
            {
                get
                {
                    return this.embTextFieldSpecified;
                }
                set
                {
                    this.embTextFieldSpecified = value;
                }
            }
        }

        /// <summary>
        /// Pure text is represented as a text region. This includes drop capitals, but practically 
        /// ornate text may be considered as a graphic.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlTextRegion : PageXmlRegion
        {
            #region private methods
            private PageXmlTextLine[] textLineField;

            private PageXmlTextEquiv[] textEquivField;

            private PageXmlTextStyle textStyleField;

            private float orientationField;

            private bool orientationFieldSpecified;

            private PageXmlTextSimpleType typeField;

            private bool typeFieldSpecified;

            private int leadingField;

            private bool leadingFieldSpecified;

            private PageXmlReadingDirectionSimpleType readingDirectionField;

            private bool readingDirectionFieldSpecified;

            private PageXmlTextLineOrderSimpleType textLineOrderField;

            private bool textLineOrderFieldSpecified;

            private float readingOrientationField;

            private bool readingOrientationFieldSpecified;

            private bool indentedField;

            private bool indentedFieldSpecified;

            private PageXmlAlignSimpleType alignField;

            private bool alignFieldSpecified;

            private PageXmlLanguageSimpleType primaryLanguageField;

            private bool primaryLanguageFieldSpecified;

            private PageXmlLanguageSimpleType secondaryLanguageField;

            private bool secondaryLanguageFieldSpecified;

            private PageXmlScriptSimpleType primaryScriptField;

            private bool primaryScriptFieldSpecified;

            private PageXmlScriptSimpleType secondaryScriptField;

            private bool secondaryScriptFieldSpecified;

            private PageXmlProductionSimpleType productionField;

            private bool productionFieldSpecified;
            #endregion

            /// <remarks/>
            [XmlElementAttribute("TextLine")]
            public PageXmlTextLine[] TextLines
            {
                get
                {
                    return this.textLineField;
                }
                set
                {
                    this.textLineField = value;
                }
            }

            /// <remarks/>
            [XmlElementAttribute("TextEquiv")]
            public PageXmlTextEquiv[] TextEquivs
            {
                get
                {
                    return this.textEquivField;
                }
                set
                {
                    this.textEquivField = value;
                }
            }

            /// <remarks/>
            public PageXmlTextStyle TextStyle
            {
                get
                {
                    return this.textStyleField;
                }
                set
                {
                    this.textStyleField = value;
                }
            }

            /// <summary>
            /// The angle the rectangle encapsulating the region
            /// has to be rotated in clockwise direction
            /// in order to correct the present skew
            /// (negative values indicate anti-clockwise rotation).
            /// (The rotated image can be further referenced
            /// via “AlternativeImage”.)
            /// Range: -179.999,180
            /// </summary>
            [XmlAttributeAttribute("orientation")]
            public float Orientation
            {
                get
                {
                    return this.orientationField;
                }
                set
                {
                    this.orientationField = value;
                    this.orientationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool OrientationSpecified
            {
                get
                {
                    return this.orientationFieldSpecified;
                }
                set
                {
                    this.orientationFieldSpecified = value;
                }
            }

            /// <summary>
            /// The nature of the text in the region
            /// </summary>
            [XmlAttributeAttribute("type")]
            public PageXmlTextSimpleType Type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                    this.typeFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool TypeSpecified
            {
                get
                {
                    return this.typeFieldSpecified;
                }
                set
                {
                    this.typeFieldSpecified = value;
                }
            }

            /// <summary>
            /// The degree of space in points between the lines of
            /// text(line spacing)
            /// </summary>
            [XmlAttributeAttribute("leading")]
            public int Leading
            {
                get
                {
                    return this.leadingField;
                }
                set
                {
                    this.leadingField = value;
                    this.leadingFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool LeadingSpecified
            {
                get
                {
                    return this.leadingFieldSpecified;
                }
                set
                {
                    this.leadingFieldSpecified = value;
                }
            }

            /// <summary>
            /// The direction in which text within lines
            /// should be read(order of words and characters),
            /// in addition to “textLineOrder”.
            /// </summary>
            [XmlAttributeAttribute("readingDirection")]
            public PageXmlReadingDirectionSimpleType ReadingDirection
            {
                get
                {
                    return this.readingDirectionField;
                }
                set
                {
                    this.readingDirectionField = value;
                    this.readingDirectionFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ReadingDirectionSpecified
            {
                get
                {
                    return this.readingDirectionFieldSpecified;
                }
                set
                {
                    this.readingDirectionFieldSpecified = value;
                }
            }

            /// <summary>
            /// The order of text lines within the block,
            /// in addition to “readingDirection”.
            /// </summary>
            [XmlAttributeAttribute("textLineOrder")]
            public PageXmlTextLineOrderSimpleType TextLineOrder
            {
                get
                {
                    return this.textLineOrderField;
                }
                set
                {
                    this.textLineOrderField = value;
                    this.textLineOrderFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool TextLineOrderSpecified
            {
                get
                {
                    return this.textLineOrderFieldSpecified;
                }
                set
                {
                    this.textLineOrderFieldSpecified = value;
                }
            }

            /// <summary>
            /// The angle the baseline of text within the region
            /// has to be rotated(relative to the rectangle
            /// encapsulating the region) in clockwise direction
            /// in order to correct the present skew,
            /// in addition to “orientation”
            /// (negative values indicate anti-clockwise rotation).
            /// Range: -179.999,180
            /// </summary>
            [XmlAttributeAttribute("readingOrientation")]
            public float ReadingOrientation
            {
                get
                {
                    return this.readingOrientationField;
                }
                set
                {
                    this.readingOrientationField = value;
                    this.readingOrientationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ReadingOrientationSpecified
            {
                get
                {
                    return this.readingOrientationFieldSpecified;
                }
                set
                {
                    this.readingOrientationFieldSpecified = value;
                }
            }

            /// <summary>
            /// Defines whether a region of text is indented or not
            /// </summary>
            [XmlAttributeAttribute("indented")]
            public bool Indented
            {
                get
                {
                    return this.indentedField;
                }
                set
                {
                    this.indentedField = value;
                    this.indentedFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool IndentedSpecified
            {
                get
                {
                    return this.indentedFieldSpecified;
                }
                set
                {
                    this.indentedFieldSpecified = value;
                }
            }

            /// <summary>
            /// Text align
            /// </summary>
            [XmlAttributeAttribute("align")]
            public PageXmlAlignSimpleType Align
            {
                get
                {
                    return this.alignField;
                }
                set
                {
                    this.alignField = value;
                    this.alignFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool AlignSpecified
            {
                get
                {
                    return this.alignFieldSpecified;
                }
                set
                {
                    this.alignFieldSpecified = value;
                }
            }

            /// <summary>
            /// The primary language used in the region
            /// </summary>
            [XmlAttributeAttribute("primaryLanguage")]
            public PageXmlLanguageSimpleType PrimaryLanguage
            {
                get
                {
                    return this.primaryLanguageField;
                }
                set
                {
                    this.primaryLanguageField = value;
                    this.primaryLanguageFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool PrimaryLanguageSpecified
            {
                get
                {
                    return this.primaryLanguageFieldSpecified;
                }
                set
                {
                    this.primaryLanguageFieldSpecified = value;
                }
            }

            /// <summary>
            /// The secondary language used in the region
            /// </summary>
            [XmlAttributeAttribute("secondaryLanguage")]
            public PageXmlLanguageSimpleType SecondaryLanguage
            {
                get
                {
                    return this.secondaryLanguageField;
                }
                set
                {
                    this.secondaryLanguageField = value;
                    this.secondaryLanguageFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool SecondaryLanguageSpecified
            {
                get
                {
                    return this.secondaryLanguageFieldSpecified;
                }
                set
                {
                    this.secondaryLanguageFieldSpecified = value;
                }
            }

            /// <summary>
            /// The primary script used in the region
            /// </summary>
            [XmlAttributeAttribute("primaryScript")]
            public PageXmlScriptSimpleType PrimaryScript
            {
                get
                {
                    return this.primaryScriptField;
                }
                set
                {
                    this.primaryScriptField = value;
                    this.primaryScriptFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool PrimaryScriptSpecified
            {
                get
                {
                    return this.primaryScriptFieldSpecified;
                }
                set
                {
                    this.primaryScriptFieldSpecified = value;
                }
            }

            /// <summary>
            /// The secondary script used in the region
            /// </summary>
            [XmlAttributeAttribute("secondaryScript")]
            public PageXmlScriptSimpleType SecondaryScript
            {
                get
                {
                    return this.secondaryScriptField;
                }
                set
                {
                    this.secondaryScriptField = value;
                    this.secondaryScriptFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool SecondaryScriptSpecified
            {
                get
                {
                    return this.secondaryScriptFieldSpecified;
                }
                set
                {
                    this.secondaryScriptFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("production")]
            public PageXmlProductionSimpleType Production
            {
                get
                {
                    return this.productionField;
                }
                set
                {
                    this.productionField = value;
                    this.productionFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ProductionSpecified
            {
                get
                {
                    return this.productionFieldSpecified;
                }
                set
                {
                    this.productionFieldSpecified = value;
                }
            }
        }


        /// <summary>
        /// To be used if the region type cannot be ascertained.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlUnknownRegion : PageXmlRegion
        {
        }

        /// <summary>
        /// Regions containing maps.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlMapRegion : PageXmlRegion
        {

            private float orientationField;

            private bool orientationFieldSpecified;

            /// <summary>
            /// The angle the rectangle encapsulating a
            /// region has to be rotated in clockwise
            /// direction in order to correct the present
            /// skew(negative values indicate
            /// anti-clockwise rotation). Range:
            /// -179.999,180
            /// </summary>
            [XmlAttributeAttribute("orientation")]
            public float Orientation
            {
                get
                {
                    return this.orientationField;
                }
                set
                {
                    this.orientationField = value;
                    this.orientationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool OrientationSpecified
            {
                get
                {
                    return this.orientationFieldSpecified;
                }
                set
                {
                    this.orientationFieldSpecified = value;
                }
            }
        }

        /// <summary>
        /// One-to-one relation between to layout object. Use 'link'
        /// for loose relations and 'join' for strong relations
        /// (where something is fragmented for instance).
        /// 
        /// <para>Examples for 'link': caption - image floating -
        /// paragraph paragraph - paragraph (when a paragraph is
        /// split across columns and the last word of the first
        /// paragraph DOES NOT continue in the second paragraph)
        /// drop-cap - paragraph (when the drop-cap is a whole word)</para>
        /// 
        /// Examples for 'join': word - word (separated word at the
        /// end of a line) drop-cap - paragraph (when the drop-cap
        /// is not a whole word) paragraph - paragraph (when a
        /// pragraph is split across columns and the last word of
        /// the first paragraph DOES continue in the second
        /// paragraph)
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlRelation
        {

            private PageXmlLabels[] labelsField;

            private PageXmlRegionRef sourceRegionRefField;

            private PageXmlRegionRef targetRegionRefField;

            private string idField;

            private PageXmlRelationType typeField;

            private bool typeFieldSpecified;

            private string customField;

            private string commentsField;

            /// <summary>
            /// Semantic labels / tags
            /// </summary>
            [XmlElementAttribute("Labels")]
            public PageXmlLabels[] Labels
            {
                get
                {
                    return this.labelsField;
                }
                set
                {
                    this.labelsField = value;
                }
            }

            /// <remarks/>
            public PageXmlRegionRef SourceRegionRef
            {
                get
                {
                    return this.sourceRegionRefField;
                }
                set
                {
                    this.sourceRegionRefField = value;
                }
            }

            /// <remarks/>
            public PageXmlRegionRef TargetRegionRef
            {
                get
                {
                    return this.targetRegionRefField;
                }
                set
                {
                    this.targetRegionRefField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("id", DataType = "ID")]
            public string Id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("type")]
            public PageXmlRelationType Type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                    this.typeFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool TypeSpecified
            {
                get
                {
                    return this.typeFieldSpecified;
                }
                set
                {
                    this.typeFieldSpecified = value;
                }
            }

            /// <summary>
            /// For generic use
            /// </summary>
            [XmlAttributeAttribute("custom")]
            public string Custom
            {
                get
                {
                    return this.customField;
                }
                set
                {
                    this.customField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("comments")]
            public string Comments
            {
                get
                {
                    return this.commentsField;
                }
                set
                {
                    this.commentsField = value;
                }
            }
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlRegionRef
        {

            private string regionRefField;

            /// <remarks/>
            [XmlAttributeAttribute("regionRef", DataType = "IDREF")]
            public string RegionRef
            {
                get
                {
                    return this.regionRefField;
                }
                set
                {
                    this.regionRefField = value;
                }
            }
        }

        /// <summary>
        /// Container for one-to-one relations between layout
        /// objects (for example: DropCap - paragraph, caption -
        /// image).
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlRelations
        {

            private PageXmlRelation[] relationField;

            /// <remarks/>
            [XmlElementAttribute("Relation")]
            public PageXmlRelation[] Relations
            {
                get
                {
                    return this.relationField;
                }
                set
                {
                    this.relationField = value;
                }
            }
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlLayer
        {

            private PageXmlRegionRef[] regionRefField;

            private string idField;

            private int zIndexField;

            private string captionField;

            /// <remarks/>
            [XmlElementAttribute("RegionRef")]
            public PageXmlRegionRef[] RegionRefs
            {
                get
                {
                    return this.regionRefField;
                }
                set
                {
                    this.regionRefField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("id", DataType = "ID")]
            public string Id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("zIndex")]
            public int ZIndex
            {
                get
                {
                    return this.zIndexField;
                }
                set
                {
                    this.zIndexField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("caption")]
            public string Caption
            {
                get
                {
                    return this.captionField;
                }
                set
                {
                    this.captionField = value;
                }
            }
        }

        /// <summary>
        /// Can be used to express the z-index of overlapping
        /// regions.An element with a greater z-index is always in
        /// front of another element with lower z-index.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlLayers
        {

            private PageXmlLayer[] layerField;

            /// <remarks/>
            [XmlElementAttribute("Layer")]
            public PageXmlLayer[] Layers
            {
                get
                {
                    return this.layerField;
                }
                set
                {
                    this.layerField = value;
                }
            }
        }

        /// <summary>
        /// Numbered group (contains unordered elements)
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlUnorderedGroup
        {
            #region private
            private PageXmlUserAttribute[] userDefinedField;

            private PageXmlLabels[] labelsField;

            private object[] itemsField;

            private string idField;

            private string regionRefField;

            private string captionField;

            private PageXmlGroupSimpleType typeField;

            private bool typeFieldSpecified;

            private bool continuationField;

            private bool continuationFieldSpecified;

            private string customField;

            private string commentsField;
            #endregion

            /// <remarks/>
            [XmlArrayItemAttribute("UserAttribute", IsNullable = false)]
            public PageXmlUserAttribute[] UserDefined
            {
                get
                {
                    return this.userDefinedField;
                }
                set
                {
                    this.userDefinedField = value;
                }
            }

            /// <summary>
            /// Semantic labels / tags
            /// </summary>
            [XmlElementAttribute("Labels")]
            public PageXmlLabels[] Labels
            {
                get
                {
                    return this.labelsField;
                }
                set
                {
                    this.labelsField = value;
                }
            }

            /// <remarks/>
            [XmlElementAttribute("OrderedGroup", typeof(PageXmlOrderedGroup))]
            [XmlElementAttribute("RegionRef", typeof(PageXmlRegionRef))]
            [XmlElementAttribute("UnorderedGroup", typeof(PageXmlUnorderedGroup))]
            public object[] Items
            {
                get
                {
                    return this.itemsField;
                }
                set
                {
                    this.itemsField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("id", DataType = "ID")]
            public string Id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }

            /// <summary>
            /// Optional link to a parent region of nested regions.
            /// The parent region doubles as reading order group.
            /// Only the nested regions should be allowed as group members.
            /// </summary>
            [XmlAttributeAttribute("regionRef", DataType = "IDREF")]
            public string RegionRef
            {
                get
                {
                    return this.regionRefField;
                }
                set
                {
                    this.regionRefField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("caption")]
            public string Caption
            {
                get
                {
                    return this.captionField;
                }
                set
                {
                    this.captionField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("type")]
            public PageXmlGroupSimpleType Type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                    this.typeFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool TypeSpecified
            {
                get
                {
                    return this.typeFieldSpecified;
                }
                set
                {
                    this.typeFieldSpecified = value;
                }
            }

            /// <summary>
            /// Is this group a continuation of another group
            /// (from previous column or page, for example)?
            /// </summary>
            [XmlAttributeAttribute("continuation")]
            public bool Continuation
            {
                get
                {
                    return this.continuationField;
                }
                set
                {
                    this.continuationField = value;
                    this.continuationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ContinuationSpecified
            {
                get
                {
                    return this.continuationFieldSpecified;
                }
                set
                {
                    this.continuationFieldSpecified = value;
                }
            }

            /// <summary>
            /// For generic use
            /// </summary>
            [XmlAttributeAttribute("custom")]
            public string Custom
            {
                get
                {
                    return this.customField;
                }
                set
                {
                    this.customField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("comments")]
            public string Comments
            {
                get
                {
                    return this.commentsField;
                }
                set
                {
                    this.commentsField = value;
                }
            }
        }

        /// <summary>
        /// Numbered group (contains ordered elements)
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlOrderedGroup
        {
            #region private
            private PageXmlUserAttribute[] userDefinedField;

            private PageXmlLabels[] labelsField;

            private object[] itemsField;

            private string idField;

            private string regionRefField;

            private string captionField;

            private PageXmlGroupSimpleType typeField;

            private bool typeFieldSpecified;

            private bool continuationField;

            private bool continuationFieldSpecified;

            private string customField;

            private string commentsField;
            #endregion

            /// <remarks/>
            [XmlArrayItemAttribute("UserAttribute", IsNullable = false)]
            public PageXmlUserAttribute[] UserDefined
            {
                get
                {
                    return this.userDefinedField;
                }
                set
                {
                    this.userDefinedField = value;
                }
            }

            /// <summary>
            /// Semantic labels / tags
            /// </summary>
            [XmlElementAttribute("Labels")]
            public PageXmlLabels[] Labels
            {
                get
                {
                    return this.labelsField;
                }
                set
                {
                    this.labelsField = value;
                }
            }

            /// <remarks/>
            [XmlElementAttribute("OrderedGroupIndexed", typeof(PageXmlOrderedGroupIndexed))]
            [XmlElementAttribute("RegionRefIndexed", typeof(PageXmlRegionRefIndexed))]
            [XmlElementAttribute("UnorderedGroupIndexed", typeof(PageXmlUnorderedGroupIndexed))]
            public object[] Items
            {
                get
                {
                    return this.itemsField;
                }
                set
                {
                    this.itemsField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("id", DataType = "ID")]
            public string Id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }

            /// <summary>
            /// Optional link to a parent region of nested regions.
            /// The parent region doubles as reading order group.
            /// Only the nested regions should be allowed as group members.
            /// </summary>
            [XmlAttributeAttribute("regionRef", DataType = "IDREF")]
            public string regionRef
            {
                get
                {
                    return this.regionRefField;
                }
                set
                {
                    this.regionRefField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("caption")]
            public string Caption
            {
                get
                {
                    return this.captionField;
                }
                set
                {
                    this.captionField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("type")]
            public PageXmlGroupSimpleType Type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                    this.typeFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool TypeSpecified
            {
                get
                {
                    return this.typeFieldSpecified;
                }
                set
                {
                    this.typeFieldSpecified = value;
                }
            }

            /// <summary>
            /// Is this group a continuation of another group
            /// (from previous column or page, for example)?
            /// </summary>
            [XmlAttributeAttribute("continuation")]
            public bool Continuation
            {
                get
                {
                    return this.continuationField;
                }
                set
                {
                    this.continuationField = value;
                    this.continuationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ContinuationSpecified
            {
                get
                {
                    return this.continuationFieldSpecified;
                }
                set
                {
                    this.continuationFieldSpecified = value;
                }
            }

            /// <summary>
            /// For generic use
            /// </summary>
            [XmlAttributeAttribute("custom")]
            public string Custom
            {
                get
                {
                    return this.customField;
                }
                set
                {
                    this.customField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("comments")]
            public string Comments
            {
                get
                {
                    return this.commentsField;
                }
                set
                {
                    this.commentsField = value;
                }
            }
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlOrderedGroupIndexed
        {

            private PageXmlUserAttribute[] userDefinedField;

            private PageXmlLabels[] labelsField;

            private object[] itemsField;

            private string idField;

            private string regionRefField;

            private int indexField;

            private string captionField;

            private PageXmlGroupSimpleType typeField;

            private bool typeFieldSpecified;

            private bool continuationField;

            private bool continuationFieldSpecified;

            private string customField;

            private string commentsField;

            /// <remarks/>
            [XmlArrayItemAttribute("UserAttribute", IsNullable = false)]
            public PageXmlUserAttribute[] UserDefined
            {
                get
                {
                    return this.userDefinedField;
                }
                set
                {
                    this.userDefinedField = value;
                }
            }

            /// <remarks/>
            [XmlElementAttribute("Labels")]
            public PageXmlLabels[] Labels
            {
                get
                {
                    return this.labelsField;
                }
                set
                {
                    this.labelsField = value;
                }
            }

            /// <remarks/>
            [XmlElementAttribute("OrderedGroupIndexed", typeof(PageXmlOrderedGroupIndexed))]
            [XmlElementAttribute("RegionRefIndexed", typeof(PageXmlRegionRefIndexed))]
            [XmlElementAttribute("UnorderedGroupIndexed", typeof(PageXmlUnorderedGroupIndexed))]
            public object[] Items
            {
                get
                {
                    return this.itemsField;
                }
                set
                {
                    this.itemsField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("id", DataType = "ID")]
            public string Id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("regionRef", DataType = "IDREF")]
            public string RegionRef
            {
                get
                {
                    return this.regionRefField;
                }
                set
                {
                    this.regionRefField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("index")]
            public int Index
            {
                get
                {
                    return this.indexField;
                }
                set
                {
                    this.indexField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("caption")]
            public string Caption
            {
                get
                {
                    return this.captionField;
                }
                set
                {
                    this.captionField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("type")]
            public PageXmlGroupSimpleType Type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                    this.typeFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool TypeSpecified
            {
                get
                {
                    return this.typeFieldSpecified;
                }
                set
                {
                    this.typeFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("continuation")]
            public bool Continuation
            {
                get
                {
                    return this.continuationField;
                }
                set
                {
                    this.continuationField = value;
                    this.continuationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ContinuationSpecified
            {
                get
                {
                    return this.continuationFieldSpecified;
                }
                set
                {
                    this.continuationFieldSpecified = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("custom")]
            public string Custom
            {
                get
                {
                    return this.customField;
                }
                set
                {
                    this.customField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("comments")]
            public string Comments
            {
                get
                {
                    return this.commentsField;
                }
                set
                {
                    this.commentsField = value;
                }
            }
        }

        /// <summary>
        /// Numbered region
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlRegionRefIndexed
        {

            private int indexField;

            private string regionRefField;

            /// <summary>
            /// Position (order number) of this item within the current hierarchy level.
            /// </summary>
            [XmlAttributeAttribute("index")]
            public int Index
            {
                get
                {
                    return this.indexField;
                }
                set
                {
                    this.indexField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("regionRef", DataType = "IDREF")]
            public string RegionRef
            {
                get
                {
                    return this.regionRefField;
                }
                set
                {
                    this.regionRefField = value;
                }
            }
        }

        /// <summary>
        /// Indexed group containing ordered elements
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlUnorderedGroupIndexed
        {
            #region private
            private PageXmlUserAttribute[] userDefinedField;

            private PageXmlLabels[] labelsField;

            private object[] itemsField;

            private string idField;

            private string regionRefField;

            private int indexField;

            private string captionField;

            private PageXmlGroupSimpleType typeField;

            private bool typeFieldSpecified;

            private bool continuationField;

            private bool continuationFieldSpecified;

            private string customField;

            private string commentsField;
            #endregion

            /// <remarks/>
            [XmlArrayItemAttribute("UserAttribute", IsNullable = false)]
            public PageXmlUserAttribute[] UserDefined
            {
                get
                {
                    return this.userDefinedField;
                }
                set
                {
                    this.userDefinedField = value;
                }
            }

            /// <summary>
            /// Semantic labels / tags
            /// </summary>
            [XmlElementAttribute("Labels")]
            public PageXmlLabels[] Labels
            {
                get
                {
                    return this.labelsField;
                }
                set
                {
                    this.labelsField = value;
                }
            }

            /// <remarks/>
            [XmlElementAttribute("OrderedGroup", typeof(PageXmlOrderedGroup))]
            [XmlElementAttribute("RegionRef", typeof(PageXmlRegionRef))]
            [XmlElementAttribute("UnorderedGroup", typeof(PageXmlUnorderedGroup))]
            public object[] Items
            {
                get
                {
                    return this.itemsField;
                }
                set
                {
                    this.itemsField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("id", DataType = "ID")]
            public string Id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }

            /// <summary>
            /// Optional link to a parent region of nested regions.
            /// The parent region doubles as reading order group.
            /// Only the nested regions should be allowed as group members.
            /// </summary>
            [XmlAttributeAttribute("regionRef", DataType = "IDREF")]
            public string RegionRef
            {
                get
                {
                    return this.regionRefField;
                }
                set
                {
                    this.regionRefField = value;
                }
            }

            /// <summary>
            /// Position (order number) of this item within the current hierarchy level.
            /// </summary>
            [XmlAttributeAttribute("index")]
            public int Index
            {
                get
                {
                    return this.indexField;
                }
                set
                {
                    this.indexField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("caption")]
            public string Caption
            {
                get
                {
                    return this.captionField;
                }
                set
                {
                    this.captionField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("type")]
            public PageXmlGroupSimpleType Type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                    this.typeFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool TypeSpecified
            {
                get
                {
                    return this.typeFieldSpecified;
                }
                set
                {
                    this.typeFieldSpecified = value;
                }
            }

            /// <summary>
            /// Is this group a continuation of another group (from
            /// previous column or page, for example)?
            /// </summary>
            [XmlAttributeAttribute("continuation")]
            public bool Continuation
            {
                get
                {
                    return this.continuationField;
                }
                set
                {
                    this.continuationField = value;
                    this.continuationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ContinuationSpecified
            {
                get
                {
                    return this.continuationFieldSpecified;
                }
                set
                {
                    this.continuationFieldSpecified = value;
                }
            }

            /// <summary>
            /// For generic use
            /// </summary>
            [XmlAttributeAttribute("custom")]
            public string Custom
            {
                get
                {
                    return this.customField;
                }
                set
                {
                    this.customField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("comments")]
            public string Comments
            {
                get
                {
                    return this.commentsField;
                }
                set
                {
                    this.commentsField = value;
                }
            }
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlReadingOrder
        {

            private object itemField;

            private float confField;

            private bool confFieldSpecified;

            /// <remarks/>
            [XmlElementAttribute("OrderedGroup", typeof(PageXmlOrderedGroup))]
            [XmlElementAttribute("UnorderedGroup", typeof(PageXmlUnorderedGroup))]
            public object Item
            {
                get
                {
                    return this.itemField;
                }
                set
                {
                    this.itemField = value;
                }
            }

            /// <summary>
            /// Confidence value (between 0 and 1)
            /// </summary>
            [XmlAttributeAttribute("conf")]
            public float Conf
            {
                get
                {
                    return this.confField;
                }
                set
                {
                    this.confField = value;
                    this.confFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ConfSpecified
            {
                get
                {
                    return this.confFieldSpecified;
                }
                set
                {
                    this.confFieldSpecified = value;
                }
            }
        }

        /// <summary>
        /// Determines the effective area on the paper of a printed page.
        /// Its size is equal for all pages of a book
        /// (exceptions: titlepage, multipage pictures).
        /// It contains all living elements (except marginals)
        /// like body type, footnotes, headings, running titles.
        /// It does not contain pagenumber (if not part of running title),
        /// marginals, signature mark, preview words.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlPrintSpace
        {

            private PageXmlCoords coordsField;

            /// <remarks/>
            public PageXmlCoords Coords
            {
                get
                {
                    return this.coordsField;
                }
                set
                {
                    this.coordsField = value;
                }
            }
        }

        /// <summary>
        /// Border of the actual page (if the scanned image
        /// contains parts not belonging to the page).
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlBorder
        {

            private PageXmlCoords coordsField;

            /// <remarks/>
            public PageXmlCoords Coords
            {
                get
                {
                    return this.coordsField;
                }
                set
                {
                    this.coordsField = value;
                }
            }
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlPage
        {

            private PageXmlAlternativeImage[] alternativeImageField;

            private PageXmlBorder borderField;

            private PageXmlPrintSpace printSpaceField;

            private PageXmlReadingOrder readingOrderField;

            private PageXmlLayers layersField;

            private PageXmlRelations relationsField;

            private PageXmlTextStyle textStyleField;

            private PageXmlUserAttribute[] userDefinedField;

            private PageXmlLabels[] labelsField;

            private PageXmlRegion[] itemsField;

            private string imageFilenameField;

            private int imageWidthField;

            private int imageHeightField;

            private float imageXResolutionField;

            private bool imageXResolutionFieldSpecified;

            private float imageYResolutionField;

            private bool imageYResolutionFieldSpecified;

            private PageXmlPageImageResolutionUnit imageResolutionUnitField;

            private bool imageResolutionUnitFieldSpecified;

            private string customField;

            private float orientationField;

            private bool orientationFieldSpecified;

            private PageXmlPageSimpleType typeField;

            private bool typeFieldSpecified;

            private PageXmlLanguageSimpleType primaryLanguageField;

            private bool primaryLanguageFieldSpecified;

            private PageXmlLanguageSimpleType secondaryLanguageField;

            private bool secondaryLanguageFieldSpecified;

            private PageXmlScriptSimpleType primaryScriptField;

            private bool primaryScriptFieldSpecified;

            private PageXmlScriptSimpleType secondaryScriptField;

            private bool secondaryScriptFieldSpecified;

            private PageXmlReadingDirectionSimpleType readingDirectionField;

            private bool readingDirectionFieldSpecified;

            private PageXmlTextLineOrderSimpleType textLineOrderField;

            private bool textLineOrderFieldSpecified;

            private float confField;

            private bool confFieldSpecified;

            /// <summary>
            /// Alternative document page images (e.g.black-and-white).
            /// </summary>
            [XmlElementAttribute("AlternativeImage")]
            public PageXmlAlternativeImage[] AlternativeImage
            {
                get
                {
                    return this.alternativeImageField;
                }
                set
                {
                    this.alternativeImageField = value;
                }
            }

            /// <remarks/>
            public PageXmlBorder Border
            {
                get
                {
                    return this.borderField;
                }
                set
                {
                    this.borderField = value;
                }
            }

            /// <remarks/>
            public PageXmlPrintSpace PrintSpace
            {
                get
                {
                    return this.printSpaceField;
                }
                set
                {
                    this.printSpaceField = value;
                }
            }

            /// <summary>
            /// Order of blocks within the page.
            /// </summary>
            public PageXmlReadingOrder ReadingOrder
            {
                get
                {
                    return this.readingOrderField;
                }
                set
                {
                    this.readingOrderField = value;
                }
            }

            /// <summary>
            /// Unassigned regions are considered to be in the (virtual) default layer which is to be treated as below any other layers.
            /// </summary>
            public PageXmlLayers Layers
            {
                get
                {
                    return this.layersField;
                }
                set
                {
                    this.layersField = value;
                }
            }

            /// <remarks/>
            public PageXmlRelations Relations
            {
                get
                {
                    return this.relationsField;
                }
                set
                {
                    this.relationsField = value;
                }
            }

            /// <summary>
            /// Default text style
            /// </summary>
            public PageXmlTextStyle TextStyle
            {
                get
                {
                    return this.textStyleField;
                }
                set
                {
                    this.textStyleField = value;
                }
            }

            /// <remarks/>
            [XmlArrayItemAttribute("UserAttribute", IsNullable = false)]
            public PageXmlUserAttribute[] UserDefined
            {
                get
                {
                    return this.userDefinedField;
                }
                set
                {
                    this.userDefinedField = value;
                }
            }

            /// <summary>
            /// Semantic labels / tags
            /// </summary>
            [XmlElementAttribute("Labels")]
            public PageXmlLabels[] Labels
            {
                get
                {
                    return this.labelsField;
                }
                set
                {
                    this.labelsField = value;
                }
            }

            /// <remarks/>
            [XmlElementAttribute("AdvertRegion", typeof(PageXmlAdvertRegion))]
            [XmlElementAttribute("ChartRegion", typeof(PageXmlChartRegion))]
            [XmlElementAttribute("ChemRegion", typeof(PageXmlChemRegion))]
            [XmlElementAttribute("CustomRegion", typeof(PageXmlCustomRegion))]
            [XmlElementAttribute("GraphicRegion", typeof(PageXmlGraphicRegion))]
            [XmlElementAttribute("ImageRegion", typeof(PageXmlImageRegion))]
            [XmlElementAttribute("LineDrawingRegion", typeof(PageXmlLineDrawingRegion))]
            [XmlElementAttribute("MapRegion", typeof(PageXmlMapRegion))]
            [XmlElementAttribute("MathsRegion", typeof(PageXmlMathsRegion))]
            [XmlElementAttribute("MusicRegion", typeof(PageXmlMusicRegion))]
            [XmlElementAttribute("NoiseRegion", typeof(PageXmlNoiseRegion))]
            [XmlElementAttribute("SeparatorRegion", typeof(PageXmlSeparatorRegion))]
            [XmlElementAttribute("TableRegion", typeof(PageXmlTableRegion))]
            [XmlElementAttribute("TextRegion", typeof(PageXmlTextRegion))]
            [XmlElementAttribute("UnknownRegion", typeof(PageXmlUnknownRegion))]
            public PageXmlRegion[] Items
            {
                get
                {
                    return this.itemsField;
                }
                set
                {
                    this.itemsField = value;
                }
            }

            /// <summary>
            /// Contains the image file name including the file extension.
            /// </summary>
            [XmlAttributeAttribute("imageFilename")]
            public string ImageFilename
            {
                get
                {
                    return this.imageFilenameField;
                }
                set
                {
                    this.imageFilenameField = value;
                }
            }

            /// <summary>
            /// Specifies the width of the image.
            /// </summary>
            [XmlAttributeAttribute("imageWidth")]
            public int ImageWidth
            {
                get
                {
                    return this.imageWidthField;
                }
                set
                {
                    this.imageWidthField = value;
                }
            }

            /// <summary>
            /// Specifies the height of the image.
            /// </summary>
            [XmlAttributeAttribute("imageHeight")]
            public int ImageHeight
            {
                get
                {
                    return this.imageHeightField;
                }
                set
                {
                    this.imageHeightField = value;
                }
            }

            /// <summary>
            /// Specifies the image resolution in width.
            /// </summary>
            [XmlAttributeAttribute("imageXResolution")]
            public float ImageXResolution
            {
                get
                {
                    return this.imageXResolutionField;
                }
                set
                {
                    this.imageXResolutionField = value;
                    this.imageXResolutionFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ImageXResolutionSpecified
            {
                get
                {
                    return this.imageXResolutionFieldSpecified;
                }
                set
                {
                    this.imageXResolutionFieldSpecified = value;
                }
            }

            /// <summary>
            /// Specifies the image resolution in height.
            /// </summary>
            [XmlAttributeAttribute("imageYResolution")]
            public float ImageYResolution
            {
                get
                {
                    return this.imageYResolutionField;
                }
                set
                {
                    this.imageYResolutionField = value;
                    this.imageYResolutionFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ImageYResolutionSpecified
            {
                get
                {
                    return this.imageYResolutionFieldSpecified;
                }
                set
                {
                    this.imageYResolutionFieldSpecified = value;
                }
            }

            /// <summary>
            /// Specifies the unit of the resolution information referring to a standardised unit of measurement 
            /// (pixels per inch, pixels per centimeter or other).
            /// </summary>
            [XmlAttributeAttribute("imageResolutionUnit")]
            public PageXmlPageImageResolutionUnit ImageResolutionUnit
            {
                get
                {
                    return this.imageResolutionUnitField;
                }
                set
                {
                    this.imageResolutionUnitField = value;
                    this.imageResolutionUnitFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ImageResolutionUnitSpecified
            {
                get
                {
                    return this.imageResolutionUnitFieldSpecified;
                }
                set
                {
                    this.imageResolutionUnitFieldSpecified = value;
                }
            }

            /// <summary>
            /// For generic use
            /// </summary>
            [XmlAttributeAttribute("custom")]
            public string Custom
            {
                get
                {
                    return this.customField;
                }
                set
                {
                    this.customField = value;
                }
            }

            /// <summary>
            /// The angle the rectangle encapsulating the page (or its Border) has to be rotated in clockwise direction
            /// in order to correct the present skew (negative values indicate anti-clockwise rotation).
            /// (The rotated image can be further referenced via “AlternativeImage”.)
            /// <para>Range: -179.999, 180</para>
            /// </summary>
            [XmlAttributeAttribute("orientation")]
            public float Orientation
            {
                get
                {
                    return this.orientationField;
                }
                set
                {
                    this.orientationField = value;
                    this.orientationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool OrientationSpecified
            {
                get
                {
                    return this.orientationFieldSpecified;
                }
                set
                {
                    this.orientationFieldSpecified = value;
                }
            }

            /// <summary>
            /// The type of the page within the document
            /// (e.g.cover page).
            /// </summary>
            [XmlAttributeAttribute("type")]
            public PageXmlPageSimpleType Type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                    this.typeFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool TypeSpecified
            {
                get
                {
                    return this.typeFieldSpecified;
                }
                set
                {
                    this.typeFieldSpecified = value;
                }
            }

            /// <summary>
            /// The primary language used in the page (lower-level definitions override the page-level definition).
            /// </summary>
            [XmlAttributeAttribute("primaryLanguage")]
            public PageXmlLanguageSimpleType PrimaryLanguage
            {
                get
                {
                    return this.primaryLanguageField;
                }
                set
                {
                    this.primaryLanguageField = value;
                    this.primaryLanguageFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool PrimaryLanguageSpecified
            {
                get
                {
                    return this.primaryLanguageFieldSpecified;
                }
                set
                {
                    this.primaryLanguageFieldSpecified = value;
                }
            }

            /// <summary>
            /// The secondary language used in the page (lower-level definitions override the page-level definition).
            /// </summary>
            [XmlAttributeAttribute("secondaryLanguage")]
            public PageXmlLanguageSimpleType SecondaryLanguage
            {
                get
                {
                    return this.secondaryLanguageField;
                }
                set
                {
                    this.secondaryLanguageField = value;
                    this.secondaryLanguageFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool SecondaryLanguageSpecified
            {
                get
                {
                    return this.secondaryLanguageFieldSpecified;
                }
                set
                {
                    this.secondaryLanguageFieldSpecified = value;
                }
            }

            /// <summary>
            /// The primary script used in the page (lower-level definitions override the page-level definition).
            /// </summary>
            [XmlAttributeAttribute("primaryScript")]
            public PageXmlScriptSimpleType PrimaryScript
            {
                get
                {
                    return this.primaryScriptField;
                }
                set
                {
                    this.primaryScriptField = value;
                    this.primaryScriptFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool PrimaryScriptSpecified
            {
                get
                {
                    return this.primaryScriptFieldSpecified;
                }
                set
                {
                    this.primaryScriptFieldSpecified = value;
                }
            }

            /// <summary>
            /// The secondary script used in the page (lower-level definitions override the page-level definition).
            /// </summary>
            [XmlAttributeAttribute("secondaryScript")]
            public PageXmlScriptSimpleType SecondaryScript
            {
                get
                {
                    return this.secondaryScriptField;
                }
                set
                {
                    this.secondaryScriptField = value;
                    this.secondaryScriptFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool SecondaryScriptSpecified
            {
                get
                {
                    return this.secondaryScriptFieldSpecified;
                }
                set
                {
                    this.secondaryScriptFieldSpecified = value;
                }
            }

            /// <summary>
            /// The direction in which text within lines should be read(order of words and characters), 
            /// in addition to “textLineOrder” (lower-level definitions override the page-level definition).    
            /// </summary>
            [XmlAttributeAttribute("readingDirection")]
            public PageXmlReadingDirectionSimpleType ReadingDirection
            {
                get
                {
                    return this.readingDirectionField;
                }
                set
                {
                    this.readingDirectionField = value;
                    this.readingDirectionFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ReadingDirectionSpecified
            {
                get
                {
                    return this.readingDirectionFieldSpecified;
                }
                set
                {
                    this.readingDirectionFieldSpecified = value;
                }
            }

            /// <summary>
            /// The order of text lines within a block, in addition to “readingDirection” 
            /// (lower-level definitions override the page-level definition).
            /// </summary>
            [XmlAttributeAttribute("textLineOrder")]
            public PageXmlTextLineOrderSimpleType TextLineOrder
            {
                get
                {
                    return this.textLineOrderField;
                }
                set
                {
                    this.textLineOrderField = value;
                    this.textLineOrderFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool TextLineOrderSpecified
            {
                get
                {
                    return this.textLineOrderFieldSpecified;
                }
                set
                {
                    this.textLineOrderFieldSpecified = value;
                }
            }

            /// <summary>
            /// Confidence value for whole page (between 0 and 1)
            /// </summary>
            [XmlAttributeAttribute("conf")]
            public float Conf
            {
                get
                {
                    return this.confField;
                }
                set
                {
                    this.confField = value;
                    this.confFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool ConfSpecified
            {
                get
                {
                    return this.confFieldSpecified;
                }
                set
                {
                    this.confFieldSpecified = value;
                }
            }
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlMetadataItem
        {

            private PageXmlLabels[] labelsField;

            private PageXmlMetadataItemType typeField;

            private bool typeFieldSpecified;

            private string nameField;

            private string valueField;

            private DateTime dateField;

            private bool dateFieldSpecified;

            /// <summary>
            /// Semantic labels / tags
            /// </summary>
            [XmlElementAttribute("Labels")]
            public PageXmlLabels[] Labels
            {
                get
                {
                    return this.labelsField;
                }
                set
                {
                    this.labelsField = value;
                }
            }

            /// <summary>
            /// Type of metadata (e.g. author)
            /// </summary>
            [XmlAttributeAttribute("type")]
            public PageXmlMetadataItemType Type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                    this.typeFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool TypeSpecified
            {
                get
                {
                    return this.typeFieldSpecified;
                }
                set
                {
                    this.typeFieldSpecified = value;
                }
            }

            /// <summary>
            /// E.g. imagePhotometricInterpretation
            /// </summary>
            [XmlAttributeAttribute("name")]
            public string Name
            {
                get
                {
                    return this.nameField;
                }
                set
                {
                    this.nameField = value;
                }
            }

            /// <summary>
            /// E.g. RGB
            /// </summary>
            [XmlAttributeAttribute("value")]
            public string Value
            {
                get
                {
                    return this.valueField;
                }
                set
                {
                    this.valueField = value;
                }
            }

            /// <remarks/>
            [XmlAttributeAttribute("date")]
            public DateTime Date
            {
                get
                {
                    return this.dateField;
                }
                set
                {
                    this.dateField = value;
                    this.dateFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnoreAttribute()]
            public bool DateSpecified
            {
                get
                {
                    return this.dateFieldSpecified;
                }
                set
                {
                    this.dateFieldSpecified = value;
                }
            }
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(AnonymousType = true, Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlMetadataItemType
        {

            /// <remarks/>
            [XmlEnumAttribute("author")]
            Author,

            /// <remarks/>
            [XmlEnumAttribute("imageProperties")]
            ImageProperties,

            /// <remarks/>
            [XmlEnumAttribute("processingStep")]
            ProcessingStep,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            Other,
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(AnonymousType = true, Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlUserAttributeType
        {

            /// <remarks/>
            [XmlEnumAttribute("xsd:string")]
            XsdString,

            /// <remarks/>
            [XmlEnumAttribute("xsd:integer")]
            XsdInteger,

            /// <remarks/>
            [XmlEnumAttribute("xsd:boolean")]
            XsdBoolean,

            /// <remarks/>
            [XmlEnumAttribute("xsd:float")]
            XsdFloat,
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlTextDataSimpleType
        {

            /// <summary>
            /// Examples: "123.456", "+1234.456", "-1234.456", "-.456", "-456"
            /// </summary>
            [XmlEnumAttribute("xsd:decimal")]
            XsdDecimal,

            /// <summary>
            /// Examples: "123.456", "+1234.456", "-1.2344e56", "-.45E-6", "INF", "-INF", "NaN"
            /// </summary>
            [XmlEnumAttribute("xsd:float")]
            XsdFloat,

            /// <summary>
            /// Examples: "123456", "+00000012", "-1", "-456"
            /// </summary>
            [XmlEnumAttribute("xsd:integer")]
            XsdInteger,

            /// <summary>
            /// Examples: "true", "false", "1", "0"
            /// </summary>
            [XmlEnumAttribute("xsd:boolean")]
            XsdBoolean,

            /// <summary>
            /// Examples: "2001-10-26", "2001-10-26+02:00", "2001-10-26Z", "2001-10-26+00:00", "-2001-10-26", "-20000-04-01"
            /// </summary>
            [XmlEnumAttribute("xsd:date")]
            XsdDate,

            /// <summary>
            /// Examples: "21:32:52", "21:32:52+02:00", "19:32:52Z", "19:32:52+00:00", "21:32:52.12679"
            /// </summary>
            [XmlEnumAttribute("xsd:time")]
            XsdTime,

            /// <summary>
            /// Examples: "2001-10-26T21:32:52", "2001-10-26T21:32:52+02:00", "2001-10-26T19:32:52Z", "2001-10-26T19:32:52+00:00","-2001-10-26T21:32:52", "2001-10-26T21:32:52.12679"
            /// </summary>
            [XmlEnumAttribute("xsd:dateTime")]
            XsdDateTime,

            /// <summary>
            /// Generic text string
            /// </summary>
            [XmlEnumAttribute("xsd:string")]
            XsdString,

            /// <summary>
            /// An XSD type that is not listed or a custom type (use dataTypeDetails attribute).
            /// </summary>
            [XmlEnumAttribute("other")]
            Other,
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(AnonymousType = true, Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlGraphemeBaseCharType
        {

            /// <remarks/>
            [XmlEnumAttribute("base")]
            Base,

            /// <remarks/>
            [XmlEnumAttribute("combining")]
            Combining,
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlColourSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("black")]
            Black,

            /// <remarks/>
            [XmlEnumAttribute("blue")]
            Blue,

            /// <remarks/>
            [XmlEnumAttribute("brown")]
            Brown,

            /// <remarks/>
            [XmlEnumAttribute("cyan")]
            Cyan,

            /// <remarks/>
            [XmlEnumAttribute("green")]
            Green,

            /// <remarks/>
            [XmlEnumAttribute("grey")]
            Grey,

            /// <remarks/>
            [XmlEnumAttribute("indigo")]
            Indigo,

            /// <remarks/>
            [XmlEnumAttribute("magenta")]
            Magenta,

            /// <remarks/>
            [XmlEnumAttribute("orange")]
            Orange,

            /// <remarks/>
            [XmlEnumAttribute("pink")]
            Pink,

            /// <remarks/>
            [XmlEnumAttribute("red")]
            Red,

            /// <remarks/>
            [XmlEnumAttribute("turquoise")]
            Turquoise,

            /// <remarks/>
            [XmlEnumAttribute("violet")]
            Violet,

            /// <remarks/>
            [XmlEnumAttribute("white")]
            White,

            /// <remarks/>
            [XmlEnumAttribute("yellow")]
            Yellow,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            Other,
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlUnderlineStyleSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("singleLine")]
            SingleLine,

            /// <remarks/>
            [XmlEnumAttribute("doubleLine")]
            DoubleLine,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            Other,
        }

        /// <summary>
        /// iso15924 2016-07-14
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlScriptSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("Adlm - Adlam")]
            AdlmAdlam,

            /// <remarks/>
            [XmlEnumAttribute("Afak - Afaka")]
            AfakAfaka,

            /// <remarks/>
            [XmlEnumAttribute("Aghb - Caucasian Albanian")]
            AghbCaucasianAlbanian,

            /// <remarks/>
            [XmlEnumAttribute("Ahom - Ahom, Tai Ahom")]
            AhomAhomTaiAhom,

            /// <remarks/>
            [XmlEnumAttribute("Arab - Arabic")]
            ArabArabic,

            /// <remarks/>
            [XmlEnumAttribute("Aran - Arabic (Nastaliq variant)")]
            AranArabicNastaliqVariant,

            /// <remarks/>
            [XmlEnumAttribute("Armi - Imperial Aramaic")]
            ArmiImperialAramaic,

            /// <remarks/>
            [XmlEnumAttribute("Armn - Armenian")]
            ArmnArmenian,

            /// <remarks/>
            [XmlEnumAttribute("Avst - Avestan")]
            AvstAvestan,

            /// <remarks/>
            [XmlEnumAttribute("Bali - Balinese")]
            BaliBalinese,

            /// <remarks/>
            [XmlEnumAttribute("Bamu - Bamum")]
            BamuBamum,

            /// <remarks/>
            [XmlEnumAttribute("Bass - Bassa Vah")]
            BassBassaVah,

            /// <remarks/>
            [XmlEnumAttribute("Batk - Batak")]
            BatkBatak,

            /// <remarks/>
            [XmlEnumAttribute("Beng - Bengali")]
            BengBengali,

            /// <remarks/>
            [XmlEnumAttribute("Bhks - Bhaiksuki")]
            BhksBhaiksuki,

            /// <remarks/>
            [XmlEnumAttribute("Blis - Blissymbols")]
            BlisBlissymbols,

            /// <remarks/>
            [XmlEnumAttribute("Bopo - Bopomofo")]
            BopoBopomofo,

            /// <remarks/>
            [XmlEnumAttribute("Brah - Brahmi")]
            BrahBrahmi,

            /// <remarks/>
            [XmlEnumAttribute("Brai - Braille")]
            BraiBraille,

            /// <remarks/>
            [XmlEnumAttribute("Bugi - Buginese")]
            BugiBuginese,

            /// <remarks/>
            [XmlEnumAttribute("Buhd - Buhid")]
            BuhdBuhid,

            /// <remarks/>
            [XmlEnumAttribute("Cakm - Chakma")]
            CakmChakma,

            /// <remarks/>
            [XmlEnumAttribute("Cans - Unified Canadian Aboriginal Syllabics")]
            CansUnifiedCanadianAboriginalSyllabics,

            /// <remarks/>
            [XmlEnumAttribute("Cari - Carian")]
            CariCarian,

            /// <remarks/>
            [XmlEnumAttribute("Cham - Cham")]
            ChamCham,

            /// <remarks/>
            [XmlEnumAttribute("Cher - Cherokee")]
            CherCherokee,

            /// <remarks/>
            [XmlEnumAttribute("Cirt - Cirth")]
            CirtCirth,

            /// <remarks/>
            [XmlEnumAttribute("Copt - Coptic")]
            CoptCoptic,

            /// <remarks/>
            [XmlEnumAttribute("Cprt - Cypriot")]
            CprtCypriot,

            /// <remarks/>
            [XmlEnumAttribute("Cyrl - Cyrillic")]
            CyrlCyrillic,

            /// <remarks/>
            [XmlEnumAttribute("Cyrs - Cyrillic (Old Church Slavonic variant)")]
            CyrsCyrillicOldChurchSlavonicVariant,

            /// <remarks/>
            [XmlEnumAttribute("Deva - Devanagari (Nagari)")]
            DevaDevanagariNagari,

            /// <remarks/>
            [XmlEnumAttribute("Dsrt - Deseret (Mormon)")]
            DsrtDeseretMormon,

            /// <remarks/>
            [XmlEnumAttribute("Dupl - Duployan shorthand, Duployan stenography")]
            DuplDuployanShorthandDuployanStenography,

            /// <remarks/>
            [XmlEnumAttribute("Egyd - Egyptian demotic")]
            EgydEgyptianDemotic,

            /// <remarks/>
            [XmlEnumAttribute("Egyh - Egyptian hieratic")]
            EgyhEgyptianHieratic,

            /// <remarks/>
            [XmlEnumAttribute("Egyp - Egyptian hieroglyphs")]
            EgypEgyptianHieroglyphs,

            /// <remarks/>
            [XmlEnumAttribute("Elba - Elbasan")]
            ElbaElbasan,

            /// <remarks/>
            [XmlEnumAttribute("Ethi - Ethiopic")]
            EthiEthiopic,

            /// <remarks/>
            [XmlEnumAttribute("Geok - Khutsuri (Asomtavruli and Nuskhuri)")]
            GeokKhutsuriAsomtavruliAndNuskhuri,

            /// <remarks/>
            [XmlEnumAttribute("Geor - Georgian (Mkhedruli)")]
            GeorGeorgianMkhedruli,

            /// <remarks/>
            [XmlEnumAttribute("Glag - Glagolitic")]
            GlagGlagolitic,

            /// <remarks/>
            [XmlEnumAttribute("Goth - Gothic")]
            GothGothic,

            /// <remarks/>
            [XmlEnumAttribute("Gran - Grantha")]
            GranGrantha,

            /// <remarks/>
            [XmlEnumAttribute("Grek - Greek")]
            GrekGreek,

            /// <remarks/>
            [XmlEnumAttribute("Gujr - Gujarati")]
            GujrGujarati,

            /// <remarks/>
            [XmlEnumAttribute("Guru - Gurmukhi")]
            GuruGurmukhi,

            /// <remarks/>
            [XmlEnumAttribute("Hanb - Han with Bopomofo")]
            HanbHanwithBopomofo,

            /// <remarks/>
            [XmlEnumAttribute("Hang - Hangul")]
            HangHangul,

            /// <remarks/>
            [XmlEnumAttribute("Hani - Han (Hanzi, Kanji, Hanja)")]
            HaniHanHanziKanjiHanja,

            /// <remarks/>
            [XmlEnumAttribute("Hano - Hanunoo (Hanunóo)")]
            HanoHanunooHanunóo,

            /// <remarks/>
            [XmlEnumAttribute("Hans - Han (Simplified variant)")]
            HansHanSimplifiedVariant,

            /// <remarks/>
            [XmlEnumAttribute("Hant - Han (Traditional variant)")]
            HantHanTraditionalVariant,

            /// <remarks/>
            [XmlEnumAttribute("Hatr - Hatran")]
            HatrHatran,

            /// <remarks/>
            [XmlEnumAttribute("Hebr - Hebrew")]
            HebrHebrew,

            /// <remarks/>
            [XmlEnumAttribute("Hira - Hiragana")]
            HiraHiragana,

            /// <remarks/>
            [XmlEnumAttribute("Hluw - Anatolian Hieroglyphs")]
            HluwAnatolianHieroglyphs,

            /// <remarks/>
            [XmlEnumAttribute("Hmng - Pahawh Hmong")]
            HmngPahawhHmong,

            /// <remarks/>
            [XmlEnumAttribute("Hrkt - Japanese syllabaries")]
            HrktJapaneseSyllabaries,

            /// <remarks/>
            [XmlEnumAttribute("Hung - Old Hungarian (Hungarian Runic)")]
            HungOldHungarianHungarianRunic,

            /// <remarks/>
            [XmlEnumAttribute("Inds - Indus (Harappan)")]
            IndsIndusHarappan,

            /// <remarks/>
            [XmlEnumAttribute("Ital - Old Italic (Etruscan, Oscan etc.)")]
            ItalOldItalicEtruscanOscanEtc,

            /// <remarks/>
            [XmlEnumAttribute("Jamo - Jamo")]
            JamoJamo,

            /// <remarks/>
            [XmlEnumAttribute("Java - Javanese")]
            JavaJavanese,

            /// <remarks/>
            [XmlEnumAttribute("Jpan - Japanese")]
            JpanJapanese,

            /// <remarks/>
            [XmlEnumAttribute("Jurc - Jurchen")]
            JurcJurchen,

            /// <remarks/>
            [XmlEnumAttribute("Kali - Kayah Li")]
            KaliKayahLi,

            /// <remarks/>
            [XmlEnumAttribute("Kana - Katakana")]
            KanaKatakana,

            /// <remarks/>
            [XmlEnumAttribute("Khar - Kharoshthi")]
            KharKharoshthi,

            /// <remarks/>
            [XmlEnumAttribute("Khmr - Khmer")]
            KhmrKhmer,

            /// <remarks/>
            [XmlEnumAttribute("Khoj - Khojki")]
            KhojKhojki,

            /// <remarks/>
            [XmlEnumAttribute("Kitl - Khitan large script")]
            KitlKhitanlargescript,

            /// <remarks/>
            [XmlEnumAttribute("Kits - Khitan small script")]
            KitsKhitansmallscript,

            /// <remarks/>
            [XmlEnumAttribute("Knda - Kannada")]
            KndaKannada,

            /// <remarks/>
            [XmlEnumAttribute("Kore - Korean (alias for Hangul + Han)")]
            KoreKoreanaliasforHangulHan,

            /// <remarks/>
            [XmlEnumAttribute("Kpel - Kpelle")]
            KpelKpelle,

            /// <remarks/>
            [XmlEnumAttribute("Kthi - Kaithi")]
            KthiKaithi,

            /// <remarks/>
            [XmlEnumAttribute("Lana - Tai Tham (Lanna)")]
            LanaTaiThamLanna,

            /// <remarks/>
            [XmlEnumAttribute("Laoo - Lao")]
            LaooLao,

            /// <remarks/>
            [XmlEnumAttribute("Latf - Latin (Fraktur variant)")]
            LatfLatinFrakturvariant,

            /// <remarks/>
            [XmlEnumAttribute("Latg - Latin (Gaelic variant)")]
            LatgLatinGaelicvariant,

            /// <remarks/>
            [XmlEnumAttribute("Latn - Latin")]
            LatnLatin,

            /// <remarks/>
            [XmlEnumAttribute("Leke - Leke")]
            LekeLeke,

            /// <remarks/>
            [XmlEnumAttribute("Lepc - Lepcha (Róng)")]
            LepcLepchaRóng,

            /// <remarks/>
            [XmlEnumAttribute("Limb - Limbu")]
            LimbLimbu,

            /// <remarks/>
            [XmlEnumAttribute("Lina - Linear A")]
            LinaLinearA,

            /// <remarks/>
            [XmlEnumAttribute("Linb - Linear B")]
            LinbLinearB,

            /// <remarks/>
            [XmlEnumAttribute("Lisu - Lisu (Fraser)")]
            LisuLisuFraser,

            /// <remarks/>
            [XmlEnumAttribute("Loma - Loma")]
            LomaLoma,

            /// <remarks/>
            [XmlEnumAttribute("Lyci - Lycian")]
            LyciLycian,

            /// <remarks/>
            [XmlEnumAttribute("Lydi - Lydian")]
            LydiLydian,

            /// <remarks/>
            [XmlEnumAttribute("Mahj - Mahajani")]
            MahjMahajani,

            /// <remarks/>
            [XmlEnumAttribute("Mand - Mandaic, Mandaean")]
            MandMandaicMandaean,

            /// <remarks/>
            [XmlEnumAttribute("Mani - Manichaean")]
            ManiManichaean,

            /// <remarks/>
            [XmlEnumAttribute("Marc - Marchen")]
            MarcMarchen,

            /// <remarks/>
            [XmlEnumAttribute("Maya - Mayan hieroglyphs")]
            MayaMayanhieroglyphs,

            /// <remarks/>
            [XmlEnumAttribute("Mend - Mende Kikakui")]
            MendMendeKikakui,

            /// <remarks/>
            [XmlEnumAttribute("Merc - Meroitic Cursive")]
            MercMeroiticCursive,

            /// <remarks/>
            [XmlEnumAttribute("Mero - Meroitic Hieroglyphs")]
            MeroMeroiticHieroglyphs,

            /// <remarks/>
            [XmlEnumAttribute("Mlym - Malayalam")]
            MlymMalayalam,

            /// <remarks/>
            [XmlEnumAttribute("Modi - Modi, Moḍī")]
            ModiModiMoḍī,

            /// <remarks/>
            [XmlEnumAttribute("Mong - Mongolian")]
            MongMongolian,

            /// <remarks/>
            [XmlEnumAttribute("Moon - Moon (Moon code, Moon script, Moon type)")]
            MoonMoonMooncodeMoonscriptMoontype,

            /// <remarks/>
            [XmlEnumAttribute("Mroo - Mro, Mru")]
            MrooMroMru,

            /// <remarks/>
            [XmlEnumAttribute("Mtei - Meitei Mayek (Meithei, Meetei)")]
            MteiMeiteiMayekMeitheiMeetei,

            /// <remarks/>
            [XmlEnumAttribute("Mult - Multani")]
            MultMultani,

            /// <remarks/>
            [XmlEnumAttribute("Mymr - Myanmar (Burmese)")]
            MymrMyanmarBurmese,

            /// <remarks/>
            [XmlEnumAttribute("Narb - Old North Arabian (Ancient North Arabian)")]
            NarbOldNorthArabianAncientNorthArabian,

            /// <remarks/>
            [XmlEnumAttribute("Nbat - Nabataean")]
            NbatNabataean,

            /// <remarks/>
            [XmlEnumAttribute("Newa - Newa, Newar, Newari")]
            NewaNewaNewarNewari,

            /// <remarks/>
            [XmlEnumAttribute("Nkgb - Nakhi Geba")]
            NkgbNakhiGeba,

            /// <remarks/>
            [XmlEnumAttribute("Nkoo - N’Ko")]
            NkooNKo,

            /// <remarks/>
            [XmlEnumAttribute("Nshu - Nüshu")]
            NshuNüshu,

            /// <remarks/>
            [XmlEnumAttribute("Ogam - Ogham")]
            OgamOgham,

            /// <remarks/>
            [XmlEnumAttribute("Olck - Ol Chiki (Ol Cemet’, Ol, Santali)")]
            OlckOlChikiOlCemetOlSantali,

            /// <remarks/>
            [XmlEnumAttribute("Orkh - Old Turkic, Orkhon Runic")]
            OrkhOldTurkicOrkhonRunic,

            /// <remarks/>
            [XmlEnumAttribute("Orya - Oriya")]
            OryaOriya,

            /// <remarks/>
            [XmlEnumAttribute("Osge - Osage")]
            OsgeOsage,

            /// <remarks/>
            [XmlEnumAttribute("Osma - Osmanya")]
            OsmaOsmanya,

            /// <remarks/>
            [XmlEnumAttribute("Palm - Palmyrene")]
            PalmPalmyrene,

            /// <remarks/>
            [XmlEnumAttribute("Pauc - Pau Cin Hau")]
            PaucPauCinHau,

            /// <remarks/>
            [XmlEnumAttribute("Perm - Old Permic")]
            PermOldPermic,

            /// <remarks/>
            [XmlEnumAttribute("Phag - Phags-pa")]
            PhagPhagspa,

            /// <remarks/>
            [XmlEnumAttribute("Phli - Inscriptional Pahlavi")]
            PhliInscriptionalPahlavi,

            /// <remarks/>
            [XmlEnumAttribute("Phlp - Psalter Pahlavi")]
            PhlpPsalterPahlavi,

            /// <remarks/>
            [XmlEnumAttribute("Phlv - Book Pahlavi")]
            PhlvBookPahlavi,

            /// <remarks/>
            [XmlEnumAttribute("Phnx - Phoenician")]
            PhnxPhoenician,

            /// <remarks/>
            [XmlEnumAttribute("Piqd - Klingon (KLI pIqaD)")]
            PiqdKlingonKLIpIqaD,

            /// <remarks/>
            [XmlEnumAttribute("Plrd - Miao (Pollard)")]
            PlrdMiaoPollard,

            /// <remarks/>
            [XmlEnumAttribute("Prti - Inscriptional Parthian")]
            PrtiInscriptionalParthian,

            /// <remarks/>
            [XmlEnumAttribute("Rjng - Rejang (Redjang, Kaganga)")]
            RjngRejangRedjangKaganga,

            /// <remarks/>
            [XmlEnumAttribute("Roro - Rongorongo")]
            RoroRongorongo,

            /// <remarks/>
            [XmlEnumAttribute("Runr - Runic")]
            RunrRunic,

            /// <remarks/>
            [XmlEnumAttribute("Samr - Samaritan")]
            SamrSamaritan,

            /// <remarks/>
            [XmlEnumAttribute("Sara - Sarati")]
            SaraSarati,

            /// <remarks/>
            [XmlEnumAttribute("Sarb - Old South Arabian")]
            SarbOldSouthArabian,

            /// <remarks/>
            [XmlEnumAttribute("Saur - Saurashtra")]
            SaurSaurashtra,

            /// <remarks/>
            [XmlEnumAttribute("Sgnw - SignWriting")]
            SgnwSignWriting,

            /// <remarks/>
            [XmlEnumAttribute("Shaw - Shavian (Shaw)")]
            ShawShavianShaw,

            /// <remarks/>
            [XmlEnumAttribute("Shrd - Sharada, Śāradā")]
            ShrdSharadaŚāradā,

            /// <remarks/>
            [XmlEnumAttribute("Sidd - Siddham")]
            SiddSiddham,

            /// <remarks/>
            [XmlEnumAttribute("Sind - Khudawadi, Sindhi")]
            SindKhudawadiSindhi,

            /// <remarks/>
            [XmlEnumAttribute("Sinh - Sinhala")]
            SinhSinhala,

            /// <remarks/>
            [XmlEnumAttribute("Sora - Sora Sompeng")]
            SoraSoraSompeng,

            /// <remarks/>
            [XmlEnumAttribute("Sund - Sundanese")]
            SundSundanese,

            /// <remarks/>
            [XmlEnumAttribute("Sylo - Syloti Nagri")]
            SyloSylotiNagri,

            /// <remarks/>
            [XmlEnumAttribute("Syrc - Syriac")]
            SyrcSyriac,

            /// <remarks/>
            [XmlEnumAttribute("Syre - Syriac (Estrangelo variant)")]
            SyreSyriacEstrangeloVariant,

            /// <remarks/>
            [XmlEnumAttribute("Syrj - Syriac (Western variant)")]
            SyrjSyriacWesternVariant,

            /// <remarks/>
            [XmlEnumAttribute("Syrn - Syriac (Eastern variant)")]
            SyrnSyriacEasternVariant,

            /// <remarks/>
            [XmlEnumAttribute("Tagb - Tagbanwa")]
            TagbTagbanwa,

            /// <remarks/>
            [XmlEnumAttribute("Takr - Takri")]
            TakrTakri,

            /// <remarks/>
            [XmlEnumAttribute("Tale - Tai Le")]
            TaleTaiLe,

            /// <remarks/>
            [XmlEnumAttribute("Talu - New Tai Lue")]
            TaluNewTaiLue,

            /// <remarks/>
            [XmlEnumAttribute("Taml - Tamil")]
            TamlTamil,

            /// <remarks/>
            [XmlEnumAttribute("Tang - Tangut")]
            TangTangut,

            /// <remarks/>
            [XmlEnumAttribute("Tavt - Tai Viet")]
            TavtTaiViet,

            /// <remarks/>
            [XmlEnumAttribute("Telu - Telugu")]
            TeluTelugu,

            /// <remarks/>
            [XmlEnumAttribute("Teng - Tengwar")]
            TengTengwar,

            /// <remarks/>
            [XmlEnumAttribute("Tfng - Tifinagh (Berber)")]
            TfngTifinaghBerber,

            /// <remarks/>
            [XmlEnumAttribute("Tglg - Tagalog (Baybayin, Alibata)")]
            TglgTagalogBaybayinAlibata,

            /// <remarks/>
            [XmlEnumAttribute("Thaa - Thaana")]
            ThaaThaana,

            /// <remarks/>
            [XmlEnumAttribute("Thai - Thai")]
            ThaiThai,

            /// <remarks/>
            [XmlEnumAttribute("Tibt - Tibetan")]
            TibtTibetan,

            /// <remarks/>
            [XmlEnumAttribute("Tirh - Tirhuta")]
            TirhTirhuta,

            /// <remarks/>
            [XmlEnumAttribute("Ugar - Ugaritic")]
            UgarUgaritic,

            /// <remarks/>
            [XmlEnumAttribute("Vaii - Vai")]
            VaiiVai,

            /// <remarks/>
            [XmlEnumAttribute("Visp - Visible Speech")]
            VispVisibleSpeech,

            /// <remarks/>
            [XmlEnumAttribute("Wara - Warang Citi (Varang Kshiti)")]
            WaraWarangCitiVarangKshiti,

            /// <remarks/>
            [XmlEnumAttribute("Wole - Woleai")]
            WoleWoleai,

            /// <remarks/>
            [XmlEnumAttribute("Xpeo - Old Persian")]
            XpeoOldPersian,

            /// <remarks/>
            [XmlEnumAttribute("Xsux - Cuneiform, Sumero-Akkadian")]
            XsuxCuneiformSumeroAkkadian,

            /// <remarks/>
            [XmlEnumAttribute("Yiii - Yi")]
            YiiiYi,

            /// <remarks/>
            [XmlEnumAttribute("Zinh - Code for inherited script")]
            ZinhCodeForInheritedScript,

            /// <remarks/>
            [XmlEnumAttribute("Zmth - Mathematical notation")]
            ZmthMathematicalNotation,

            /// <remarks/>
            [XmlEnumAttribute("Zsye - Symbols (Emoji variant)")]
            ZsyeSymbolsEmojiVariant,

            /// <remarks/>
            [XmlEnumAttribute("Zsym - Symbols")]
            ZsymSymbols,

            /// <remarks/>
            [XmlEnumAttribute("Zxxx - Code for unwritten documents")]
            ZxxxCodeForUnwrittenDocuments,

            /// <remarks/>
            [XmlEnumAttribute("Zyyy - Code for undetermined script")]
            ZyyyCodeForUndeterminedScript,

            /// <remarks/>
            [XmlEnumAttribute("Zzzz - Code for uncoded script")]
            ZzzzCodeForUncodedScript,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            Other,
        }

        /// <summary>
        /// Text production type
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlProductionSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("printed")]
            Printed,

            /// <remarks/>
            [XmlEnumAttribute("typewritten")]
            Typewritten,

            /// <remarks/>
            [XmlEnumAttribute("handwritten-cursive")]
            HandwrittenCursive,

            /// <remarks/>
            [XmlEnumAttribute("handwritten-printscript")]
            HandwrittenPrintscript,

            /// <remarks/>
            [XmlEnumAttribute("medieval-manuscript")]
            MedievalManuscript,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            Other,
        }

        /// <summary>
        /// ISO 639.x 2016-07-14
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlLanguageSimpleType
        {

            /// <remarks/>
            Abkhaz,

            /// <remarks/>
            Afar,

            /// <remarks/>
            Afrikaans,

            /// <remarks/>
            Akan,

            /// <remarks/>
            Albanian,

            /// <remarks/>
            Amharic,

            /// <remarks/>
            Arabic,

            /// <remarks/>
            Aragonese,

            /// <remarks/>
            Armenian,

            /// <remarks/>
            Assamese,

            /// <remarks/>
            Avaric,

            /// <remarks/>
            Avestan,

            /// <remarks/>
            Aymara,

            /// <remarks/>
            Azerbaijani,

            /// <remarks/>
            Bambara,

            /// <remarks/>
            Bashkir,

            /// <remarks/>
            Basque,

            /// <remarks/>
            Belarusian,

            /// <remarks/>
            Bengali,

            /// <remarks/>
            Bihari,

            /// <remarks/>
            Bislama,

            /// <remarks/>
            Bosnian,

            /// <remarks/>
            Breton,

            /// <remarks/>
            Bulgarian,

            /// <remarks/>
            Burmese,

            /// <remarks/>
            Cambodian,

            /// <remarks/>
            Cantonese,

            /// <remarks/>
            Catalan,

            /// <remarks/>
            Chamorro,

            /// <remarks/>
            Chechen,

            /// <remarks/>
            Chichewa,

            /// <remarks/>
            Chinese,

            /// <remarks/>
            Chuvash,

            /// <remarks/>
            Cornish,

            /// <remarks/>
            Corsican,

            /// <remarks/>
            Cree,

            /// <remarks/>
            Croatian,

            /// <remarks/>
            Czech,

            /// <remarks/>
            Danish,

            /// <remarks/>
            Divehi,

            /// <remarks/>
            Dutch,

            /// <remarks/>
            Dzongkha,

            /// <remarks/>
            English,

            /// <remarks/>
            Esperanto,

            /// <remarks/>
            Estonian,

            /// <remarks/>
            Ewe,

            /// <remarks/>
            Faroese,

            /// <remarks/>
            Fijian,

            /// <remarks/>
            Finnish,

            /// <remarks/>
            French,

            /// <remarks/>
            Fula,

            /// <remarks/>
            Gaelic,

            /// <remarks/>
            Galician,

            /// <remarks/>
            Ganda,

            /// <remarks/>
            Georgian,

            /// <remarks/>
            German,

            /// <remarks/>
            Greek,

            /// <remarks/>
            Guaraní,

            /// <remarks/>
            Gujarati,

            /// <remarks/>
            Haitian,

            /// <remarks/>
            Hausa,

            /// <remarks/>
            Hebrew,

            /// <remarks/>
            Herero,

            /// <remarks/>
            Hindi,

            /// <remarks/>
            [XmlEnumAttribute("Hiri Motu")]
            HiriMotu,

            /// <remarks/>
            Hungarian,

            /// <remarks/>
            Icelandic,

            /// <remarks/>
            Ido,

            /// <remarks/>
            Igbo,

            /// <remarks/>
            Indonesian,

            /// <remarks/>
            Interlingua,

            /// <remarks/>
            Interlingue,

            /// <remarks/>
            Inuktitut,

            /// <remarks/>
            Inupiaq,

            /// <remarks/>
            Irish,

            /// <remarks/>
            Italian,

            /// <remarks/>
            Japanese,

            /// <remarks/>
            Javanese,

            /// <remarks/>
            Kalaallisut,

            /// <remarks/>
            Kannada,

            /// <remarks/>
            Kanuri,

            /// <remarks/>
            Kashmiri,

            /// <remarks/>
            Kazakh,

            /// <remarks/>
            Khmer,

            /// <remarks/>
            Kikuyu,

            /// <remarks/>
            Kinyarwanda,

            /// <remarks/>
            Kirundi,

            /// <remarks/>
            Komi,

            /// <remarks/>
            Kongo,

            /// <remarks/>
            Korean,

            /// <remarks/>
            Kurdish,

            /// <remarks/>
            Kwanyama,

            /// <remarks/>
            Kyrgyz,

            /// <remarks/>
            Lao,

            /// <remarks/>
            Latin,

            /// <remarks/>
            Latvian,

            /// <remarks/>
            Limburgish,

            /// <remarks/>
            Lingala,

            /// <remarks/>
            Lithuanian,

            /// <remarks/>
            [XmlEnumAttribute("Luba-Katanga")]
            LubaKatanga,

            /// <remarks/>
            Luxembourgish,

            /// <remarks/>
            Macedonian,

            /// <remarks/>
            Malagasy,

            /// <remarks/>
            Malay,

            /// <remarks/>
            Malayalam,

            /// <remarks/>
            Maltese,

            /// <remarks/>
            Manx,

            /// <remarks/>
            Māori,

            /// <remarks/>
            Marathi,

            /// <remarks/>
            Marshallese,

            /// <remarks/>
            Mongolian,

            /// <remarks/>
            Nauru,

            /// <remarks/>
            Navajo,

            /// <remarks/>
            Ndonga,

            /// <remarks/>
            Nepali,

            /// <remarks/>
            [XmlEnumAttribute("North Ndebele")]
            NorthNdebele,

            /// <remarks/>
            [XmlEnumAttribute("Northern Sami")]
            NorthernSami,

            /// <remarks/>
            Norwegian,

            /// <remarks/>
            [XmlEnumAttribute("Norwegian Bokmål")]
            NorwegianBokmål,

            /// <remarks/>
            [XmlEnumAttribute("Norwegian Nynorsk")]
            NorwegianNynorsk,

            /// <remarks/>
            Nuosu,

            /// <remarks/>
            Occitan,

            /// <remarks/>
            Ojibwe,

            /// <remarks/>
            [XmlEnumAttribute("Old Church Slavonic")]
            OldChurchSlavonic,

            /// <remarks/>
            Oriya,

            /// <remarks/>
            Oromo,

            /// <remarks/>
            Ossetian,

            /// <remarks/>
            Pāli,

            /// <remarks/>
            Panjabi,

            /// <remarks/>
            Pashto,

            /// <remarks/>
            Persian,

            /// <remarks/>
            Polish,

            /// <remarks/>
            Portuguese,

            /// <remarks/>
            Punjabi,

            /// <remarks/>
            Quechua,

            /// <remarks/>
            Romanian,

            /// <remarks/>
            Romansh,

            /// <remarks/>
            Russian,

            /// <remarks/>
            Samoan,

            /// <remarks/>
            Sango,

            /// <remarks/>
            Sanskrit,

            /// <remarks/>
            Sardinian,

            /// <remarks/>
            Serbian,

            /// <remarks/>
            Shona,

            /// <remarks/>
            Sindhi,

            /// <remarks/>
            Sinhala,

            /// <remarks/>
            Slovak,

            /// <remarks/>
            Slovene,

            /// <remarks/>
            Somali,

            /// <remarks/>
            [XmlEnumAttribute("South Ndebele")]
            SouthNdebele,

            /// <remarks/>
            [XmlEnumAttribute("Southern Sotho")]
            SouthernSotho,

            /// <remarks/>
            Spanish,

            /// <remarks/>
            Sundanese,

            /// <remarks/>
            Swahili,

            /// <remarks/>
            Swati,

            /// <remarks/>
            Swedish,

            /// <remarks/>
            Tagalog,

            /// <remarks/>
            Tahitian,

            /// <remarks/>
            Tajik,

            /// <remarks/>
            Tamil,

            /// <remarks/>
            Tatar,

            /// <remarks/>
            Telugu,

            /// <remarks/>
            Thai,

            /// <remarks/>
            Tibetan,

            /// <remarks/>
            Tigrinya,

            /// <remarks/>
            Tonga,

            /// <remarks/>
            Tsonga,

            /// <remarks/>
            Tswana,

            /// <remarks/>
            Turkish,

            /// <remarks/>
            Turkmen,

            /// <remarks/>
            Twi,

            /// <remarks/>
            Uighur,

            /// <remarks/>
            Ukrainian,

            /// <remarks/>
            Urdu,

            /// <remarks/>
            Uzbek,

            /// <remarks/>
            Venda,

            /// <remarks/>
            Vietnamese,

            /// <remarks/>
            Volapük,

            /// <remarks/>
            Walloon,

            /// <remarks/>
            Welsh,

            /// <remarks/>
            [XmlEnumAttribute("Western Frisian")]
            WesternFrisian,

            /// <remarks/>
            Wolof,

            /// <remarks/>
            Xhosa,

            /// <remarks/>
            Yiddish,

            /// <remarks/>
            Yoruba,

            /// <remarks/>
            Zhuang,

            /// <remarks/>
            Zulu,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            Other,
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlReadingDirectionSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("left-to-right")]
            LeftToRight,

            /// <remarks/>
            [XmlEnumAttribute("right-to-left")]
            RightToLeft,

            /// <remarks/>
            [XmlEnumAttribute("top-to-bottom")]
            TopToBottom,

            /// <remarks/>
            [XmlEnumAttribute("bottom-to-top")]
            BottomToTop,
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlChartSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("bar")]
            Bar,

            /// <remarks/>
            [XmlEnumAttribute("line")]
            Line,

            /// <remarks/>
            [XmlEnumAttribute("pie")]
            Pie,

            /// <remarks/>
            [XmlEnumAttribute("scatter")]
            Scatter,

            /// <remarks/>
            [XmlEnumAttribute("surface")]
            Surface,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            Other,
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlGraphicsSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("logo")]
            Logo,

            /// <remarks/>
            [XmlEnumAttribute("letterhead")]
            Letterhead,

            /// <remarks/>
            [XmlEnumAttribute("decoration")]
            Decoration,

            /// <remarks/>
            [XmlEnumAttribute("frame")]
            Frame,

            /// <remarks/>
            [XmlEnumAttribute("handwritten-annotation")]
            HandwrittenAnnotation,

            /// <remarks/>
            [XmlEnumAttribute("stamp")]
            Stamp,

            /// <remarks/>
            [XmlEnumAttribute("signature")]
            Signature,

            /// <remarks/>
            [XmlEnumAttribute("barcode")]
            Barcode,

            /// <remarks/>
            [XmlEnumAttribute("paper-grow")]
            PaperGrow,

            /// <remarks/>
            [XmlEnumAttribute("punch-hole")]
            PunchHole,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            Other,
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlColourDepthSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("bilevel")]
            BiLevel,

            /// <remarks/>
            [XmlEnumAttribute("greyscale")]
            GreyScale,

            /// <remarks/>
            [XmlEnumAttribute("colour")]
            Colour,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            Other,
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlTextSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("paragraph")]
            Paragraph,

            /// <remarks/>
            [XmlEnumAttribute("heading")]
            Heading,

            /// <remarks/>
            [XmlEnumAttribute("caption")]
            Caption,

            /// <remarks/>
            [XmlEnumAttribute("header")]
            Header,

            /// <remarks/>
            [XmlEnumAttribute("footer")]
            Footer,

            /// <remarks/>
            [XmlEnumAttribute("page-number")]
            PageNumber,

            /// <remarks/>
            [XmlEnumAttribute("drop-capital")]
            DropCapital,

            /// <remarks/>
            [XmlEnumAttribute("credit")]
            Credit,

            /// <remarks/>
            [XmlEnumAttribute("floating")]
            Floating,

            /// <remarks/>
            [XmlEnumAttribute("signature-mark")]
            SignatureMark,

            /// <remarks/>
            [XmlEnumAttribute("catch-word")]
            CatchWord,

            /// <remarks/>
            [XmlEnumAttribute("marginalia")]
            Marginalia,

            /// <remarks/>
            [XmlEnumAttribute("footnote")]
            FootNote,

            /// <remarks/>
            [XmlEnumAttribute("footnote-continued")]
            FootNoteContinued,

            /// <remarks/>
            [XmlEnumAttribute("endnote")]
            EndNote,

            /// <remarks/>
            [XmlEnumAttribute("TOC-entry")]
            TocEntry,

            /// <remarks/>
            [XmlEnumAttribute("list-label")]
            LisLabel,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            Other,
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlTextLineOrderSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("top-to-bottom")]
            TopToBottom,

            /// <remarks/>
            [XmlEnumAttribute("bottom-to-top")]
            BottomToTop,

            /// <remarks/>
            [XmlEnumAttribute("left-to-right")]
            LeftToRight,

            /// <remarks/>
            [XmlEnumAttribute("right-to-left")]
            RightToLeft,
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlAlignSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("left")]
            Left,

            /// <remarks/>
            [XmlEnumAttribute("centre")]
            Centre,

            /// <remarks/>
            [XmlEnumAttribute("right")]
            Right,

            /// <remarks/>
            [XmlEnumAttribute("justify")]
            Justify,
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(AnonymousType = true, Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlRelationType
        {

            /// <remarks/>
            [XmlEnumAttribute("link")]
            Link,

            /// <remarks/>
            [XmlEnumAttribute("join")]
            Join,
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlGroupSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("paragraph")]
            Paragraph,

            /// <remarks/>
            [XmlEnumAttribute("list")]
            List,

            /// <remarks/>
            [XmlEnumAttribute("list-item")]
            ListItem,

            /// <remarks/>
            [XmlEnumAttribute("figure")]
            Figure,

            /// <remarks/>
            [XmlEnumAttribute("article")]
            Article,

            /// <remarks/>
            [XmlEnumAttribute("div")]
            Div,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            Other,
        }

        /// <summary>
        /// Specifies the unit of the resolution information referring to a standardised unit of measurement (pixels per inch, pixels per centimeter or other).
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(AnonymousType = true, Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlPageImageResolutionUnit
        {

            /// <remarks/>
            PPI,

            /// <remarks/>
            PPCM,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            other,
        }

        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlPageSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("front-cover")]
            FrontCover,

            /// <remarks/>
            [XmlEnumAttribute("back-cover")]
            BackCover,

            /// <remarks/>
            [XmlEnumAttribute("title")]
            Title,

            /// <remarks/>
            [XmlEnumAttribute("table-of-contents")]
            TableOfContents,

            /// <remarks/>
            [XmlEnumAttribute("index")]
            Index,

            /// <remarks/>
            [XmlEnumAttribute("content")]
            Content,

            /// <remarks/>
            [XmlEnumAttribute("blank")]
            Blank,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            Other,
        }
    }
    #endregion
}
