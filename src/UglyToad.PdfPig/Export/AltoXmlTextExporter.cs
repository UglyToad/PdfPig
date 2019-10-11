using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.Geometry;
using UglyToad.PdfPig.Util;

namespace UglyToad.PdfPig.Export
{
    /// <summary>
    /// Alto 4.1 (XML) text exporter.
    /// <para>See https://github.com/altoxml/schema </para>
    /// </summary>
    public class AltoXmlTextExporter : ITextExporter
    {
        private IPageSegmenter pageSegmenter;
        private IWordExtractor wordExtractor;

        private decimal scale;
        private string indentChar;

        int pageCount = 0;
        int pageSpaceCount = 0;
        int graphicalElementCount = 0;
        int illustrationCount = 0;
        int textBlockCount = 0;
        int textLineCount = 0;
        int stringCount = 0;
        int glyphCount = 0;

        /// <summary>
        /// Alto 4.1 (XML)
        /// <para>See https://github.com/altoxml/schema </para>
        /// </summary>
        /// <param name="wordExtractor"></param>
        /// <param name="pageSegmenter"></param>
        /// <param name="scale"></param>
        /// <param name="indent">Indent character.</param>
        public AltoXmlTextExporter(IWordExtractor wordExtractor, IPageSegmenter pageSegmenter, double scale = 1.0, string indent = "\t")
        {
            this.wordExtractor = wordExtractor;
            this.pageSegmenter = pageSegmenter;
            this.scale = (decimal)scale;
            this.indentChar = indent;
        }

        /// <summary>
        /// Get the Alto (XML) string of the pages layout.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="includePaths">Draw <see cref="PdfPath"/>s present in the page.</param>
        public string Get(PdfDocument document, bool includePaths = false)
        {
            AltoDocument alto = CreateAltoDocument("unknown");
            var altoPages = new List<AltoDocument.AltoPage>();

            for (var i = 0; i < document.NumberOfPages; i++)
            {
                var page = document.GetPage(i + 1);
                altoPages.Add(ToAltoPage(page, includePaths));
            }
            alto.Layout.Pages = altoPages.ToArray();

            return Serialize(alto);
        }

        /// <summary>
        /// Get the Alto (XML) string of the page layout. Excludes <see cref="PdfPath"/>s.
        /// </summary>
        /// <param name="page"></param>
        public string Get(Page page)
        {
            return Get(page, false); 
        }

        /// <summary>
        /// Get the Alto (XML) string of the page layout.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="includePaths">Draw <see cref="PdfPath"/>s present in the page.</param>
        public string Get(Page page, bool includePaths)
        {
            AltoDocument alto = CreateAltoDocument("unknown");
            var altoPages = new List<AltoDocument.AltoPage>();

            alto.Layout.Pages = new AltoDocument.AltoPage[] { ToAltoPage(page, includePaths) };

            return Serialize(alto);
        }

        /// <summary>
        /// Create an empty <see cref="AltoDocument"/>.
        /// </summary>
        /// <param name="fileName"></param>
        private AltoDocument CreateAltoDocument(string fileName)
        {
            return new AltoDocument()
            {
                Layout = new AltoDocument.AltoLayout()
                {
                    StyleRefs = null
                },
                Description = GetAltoDescription(fileName),
                SchemaVersion = "4",
            };
        }

        private AltoDocument.AltoPage ToAltoPage(Page page, bool includePaths)
        {
            pageCount = page.Number;
            pageSpaceCount++;

            var altoPage = new AltoDocument.AltoPage()
            {
                Height = (float)Math.Round(page.Height * scale),
                Width = (float)Math.Round(page.Width * scale),
                Accuracy = float.NaN,
                Quality = AltoDocument.AltoQuality.OK,
                QualityDetail = null,
                BottomMargin = null,
                LeftMargin = null,
                RightMargin = null,
                TopMargin = null,
                Pc = float.NaN,
                PhysicalImgNr = page.Number,
                PrintedImgNr = null,
                PageClass = null,
                Position = AltoDocument.AltoPosition.Cover,
                Processing = null,
                ProcessingRefs = null,
                StyleRefs = null,
                PrintSpace = new AltoDocument.AltoPageSpace()
                {
                    Height = (float)Math.Round(page.Height * scale),    // TBD
                    Width = (float)Math.Round(page.Width * scale),      // TBD
                    VPos = 0f,                                          // TBD
                    HPos = 0f,                                          // TBD
                    ComposedBlocks = null,                              // TBD
                    GraphicalElements = null,                           // TBD
                    Illustrations = null,                               // TBD
                    ProcessingRefs = null,                              // TBD
                    StyleRefs = null,                                   // TBD
                    Id = "P" + pageCount + "_PS" + pageSpaceCount.ToString("#00000")
                },
                Id = "P" + pageCount
            };

            var words = page.GetWords(wordExtractor);
            if (words.Count() > 0)
            {
                var blocks = pageSegmenter.GetBlocks(words);
                altoPage.PrintSpace.TextBlock = blocks.Select(b => ToAltoTextBlock(b, page.Height)).ToArray();   
            }
            
            var images = page.GetImages();
            if (images.Count() > 0)
            {
                altoPage.PrintSpace.Illustrations = images.Select(i => ToAltoIllustration(i, page.Height)).ToArray();
            }

            if (includePaths)
            {
                var graphicalElements = page.ExperimentalAccess.Paths.Select(p => ToAltoGraphicalElement(p, page.Height));
                if (graphicalElements.Count() > 0)
                {
                    altoPage.PrintSpace.GraphicalElements = graphicalElements.ToArray();
                }
            }
            return altoPage;
        }

        private AltoDocument.AltoGraphicalElement ToAltoGraphicalElement(PdfPath pdfPath, decimal height)
        {
            graphicalElementCount++;

            var rectangle = pdfPath.GetBoundingRectangle();
            if (rectangle.HasValue)
            {
                return new AltoDocument.AltoGraphicalElement()
                {
                    VPos = (float)Math.Round((height - rectangle.Value.Top) * scale),
                    HPos = (float)Math.Round(rectangle.Value.Left * scale),
                    Height = (float)Math.Round(rectangle.Value.Height * scale),
                    Width = (float)Math.Round(rectangle.Value.Width * scale),
                    Rotation = 0,
                    StyleRefs = null,
                    TagRefs = null,
                    title = null,
                    type = null,
                    Id = "P" + pageCount + "_GE" + graphicalElementCount.ToString("#00000")
                };
            }
            return null;
        }

        private AltoDocument.AltoIllustration ToAltoIllustration(IPdfImage pdfImage, decimal height)
        {
            illustrationCount++;
            var rectangle = pdfImage.Bounds;

            return new AltoDocument.AltoIllustration()
            {
                VPos = (float)Math.Round((height - rectangle.Top) * scale),
                HPos = (float)Math.Round(rectangle.Left * scale),
                Height = (float)Math.Round(rectangle.Height * scale),
                Width = (float)Math.Round(rectangle.Width * scale),
                FileId = "",
                Rotation = 0,
                Id = "P" + pageCount + "_I" + illustrationCount.ToString("#00000")
            };
        }

        private AltoDocument.AltoTextBlock ToAltoTextBlock(TextBlock textBlock, decimal height)
        {
            textBlockCount++;

            return new AltoDocument.AltoTextBlock()
            {
                VPos = (float)Math.Round((height - textBlock.BoundingBox.Top) * scale),
                HPos = (float)Math.Round(textBlock.BoundingBox.Left * scale),
                Height = (float)Math.Round(textBlock.BoundingBox.Height * scale),
                Width = (float)Math.Round(textBlock.BoundingBox.Width * scale),
                Rotation = 0,
                TextLines = textBlock.TextLines.Select(l => ToAltoTextLine(l, height)).ToArray(),
                StyleRefs = null,
                TagRefs = null,
                title = null,
                type = null,
                Id = "P" + pageCount + "_TB" + textBlockCount.ToString("#00000")
            };
        }

        private AltoDocument.AltoTextBlockTextLine ToAltoTextLine(TextLine textLine, decimal height)
        {
            textLineCount++;
            var strings = textLine.Words.Select(w => ToAltoString(w, height)).ToArray();

            return new AltoDocument.AltoTextBlockTextLine()
            {
                VPos = (float)Math.Round((height - textLine.BoundingBox.Top) * scale),
                HPos = (float)Math.Round(textLine.BoundingBox.Left * scale),
                Height = (float)Math.Round(textLine.BoundingBox.Height * scale),
                Width = (float)Math.Round(textLine.BoundingBox.Width * scale),
                BaseLine = float.NaN,
                Strings = strings,
                Lang = null,
                StyleRefs = null,
                TagRefs = null,
                Id = "P" + pageCount + "_TL" + textLineCount.ToString("#00000")
            };
        }

        private AltoDocument.AltoString ToAltoString(Word word, decimal height)
        {
            stringCount++;
            var glyphs = word.Letters.Select(l => ToAltoGlyph(l, height)).ToArray();
            return new AltoDocument.AltoString()
            {
                VPos = (float)Math.Round((height - word.BoundingBox.Top) * scale),
                HPos = (float)Math.Round(word.BoundingBox.Left * scale),
                Height = (float)Math.Round(word.BoundingBox.Height * scale),
                Width = (float)Math.Round(word.BoundingBox.Width * scale),
                Glyph = glyphs,
                Cc = string.Join("", glyphs.Select(g => 9f * (1f - g.Gc))), // from 0->1 to 9->0
                Content = word.Text,
                Lang = null,
                StyleRefs = null,
                SubsContent = null,
                TagRefs = null,
                Wc = float.NaN,
                Id = "P" + pageCount + "_ST" + stringCount.ToString("#00000")
            };
        }

        private AltoDocument.AltoGlyph ToAltoGlyph(Letter letter, decimal height)
        {
            glyphCount++;
            return new AltoDocument.AltoGlyph()
            {
                VPos = (float)Math.Round((height - letter.GlyphRectangle.Top) * scale),
                HPos = (float)Math.Round(letter.GlyphRectangle.Left * scale),
                Height = (float)Math.Round(letter.GlyphRectangle.Height * scale),
                Width = (float)Math.Round(letter.GlyphRectangle.Width * scale),
                Gc = 1.0f,
                Content = letter.Value,
                Id = "P" + pageCount + "_ST" + stringCount.ToString("#00000") + "_G" + glyphCount.ToString("#00")
            };
        }

        private AltoDocument.AltoDescription GetAltoDescription(string fileName)
        {
            var processing = new AltoDocument.AltoDescriptionProcessing()
            {
                ProcessingAgency = null,
                ProcessingCategory = AltoDocument.AltoProcessingCategory.Other,
                ProcessingDateTime = DateTime.UtcNow.ToString(),
                ProcessingSoftware = new AltoDocument.AltoProcessingSoftware()
                {
                    SoftwareName = "PdfPig",
                    SoftwareCreator = @"https://github.com/UglyToad/PdfPig",
                    ApplicationDescription = "Read and extract text and other content from PDFs in C# (port of PdfBox)",
                    SoftwareVersion = "x.x.xx"
                },
                ProcessingStepDescription = null,
                ProcessingStepSettings = pageSegmenter.GetType().Name + "|" + wordExtractor.GetType().Name,
                Id = "P" + pageCount + "_D1"
            };

            var documentIdentifier = new AltoDocument.AltoDocumentIdentifier()
            {
                DocumentIdentifierLocation = null,
                Value = null
            };

            var fileIdentifier = new AltoDocument.AltoFileIdentifier()
            {
                FileIdentifierLocation = null,
                Value = null
            };

            return new AltoDocument.AltoDescription()
            {
                MeasurementUnit = AltoDocument.AltoMeasurementUnit.Pixel,
                Processings = new[] { processing },
                SourceImageInformation = new AltoDocument.AltoSourceImageInformation()
                {
                    DocumentIdentifiers = new AltoDocument.AltoDocumentIdentifier[] { documentIdentifier },
                    FileIdentifiers = new AltoDocument.AltoFileIdentifier[] { fileIdentifier },
                    FileName = fileName
                }
            };
        }

        private string Serialize(AltoDocument altoDocument)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AltoDocument));
            var settings = new XmlWriterSettings()
            {
                Encoding = System.Text.Encoding.UTF8,
                Indent = true,
                IndentChars = indentChar,
            };

            using (var memoryStream = new System.IO.MemoryStream())
            using (var xmlWriter = XmlWriter.Create(memoryStream, settings))
            {
                serializer.Serialize(xmlWriter, altoDocument);
                return System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }

        private static AltoDocument Deserialize(string xmlPath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AltoDocument));

            using (var reader = XmlReader.Create(xmlPath))
            {
                return (AltoDocument)serializer.Deserialize(reader);
            }
        }

        #region ALTO Schema
        /********************************************************************************
         * Alto version 4.1 https://github.com/altoxml/schema/blob/master/v4/alto-4-1.xsd
         * Auto-generated by xsd and improved by BobLD
         ********************************************************************************/

        /// <summary>
        /// [Alto] Alto Schema root
        /// <para>Version 4.1</para>
        /// See https://github.com/altoxml/schema
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [DebuggerStepThroughAttribute()]
        [DesignerCategoryAttribute("code")]
        [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        [XmlRootAttribute("alto", Namespace = "http://www.loc.gov/standards/alto/ns-v4#", IsNullable = false)]
        public class AltoDocument
        {
            private AltoDescription descriptionField;

            private AltoStyles stylesField;

            private AltoTags tagsField;

            private AltoLayout layoutField;

            private string sCHEMAVERSIONField;

            /// <summary>
            /// Describes general settings of the alto file like measurement units and metadata
            /// </summary>
            public AltoDescription Description
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

            /// <summary>
            /// Styles define properties of layout elements. A style defined in a parent element
            /// is used as default style for all related children elements.
            /// </summary>
            public AltoStyles Styles
            {
                get
                {
                    return this.stylesField;
                }
                set
                {
                    this.stylesField = value;
                }
            }

            /// <summary>
            /// Tag define properties of additional characteristic. The tags are referenced from 
            /// related content element on Block or String element by attribute TAGREF via the tag ID.
            /// 
            /// This container element contains the individual elements for LayoutTags, StructureTags,
            /// RoleTags, NamedEntityTags and OtherTags
            /// </summary>
            public AltoTags Tags
            {
                get
                {
                    return this.tagsField;
                }
                set
                {
                    this.tagsField = value;
                }
            }

            /// <summary>
            /// The root layout element.
            /// </summary>
            public AltoLayout Layout
            {
                get
                {
                    return this.layoutField;
                }
                set
                {
                    this.layoutField = value;
                }
            }

            /// <summary>
            /// Schema version of the ALTO file.
            /// </summary>
            [XmlAttributeAttribute("SCHEMAVERSION")]
            public string SchemaVersion
            {
                get
                {
                    return this.sCHEMAVERSIONField;
                }
                set
                {
                    this.sCHEMAVERSIONField = value;
                }
            }

            /// <summary>
            /// [Alto] Description
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoDescription
            {

                private AltoMeasurementUnit measurementUnitField;

                private AltoSourceImageInformation sourceImageInformationField;

                private AltoDescriptionOcrProcessing[] oCRProcessingField;

                private AltoDescriptionProcessing[] processingField;

                /// <remarks/>
                public AltoMeasurementUnit MeasurementUnit
                {
                    get
                    {
                        return this.measurementUnitField;
                    }
                    set
                    {
                        this.measurementUnitField = value;
                    }
                }

                /// <remarks/>
                [XmlElementAttribute("sourceImageInformation")]
                public AltoSourceImageInformation SourceImageInformation
                {
                    get
                    {
                        return this.sourceImageInformationField;
                    }
                    set
                    {
                        this.sourceImageInformationField = value;
                    }
                }

                /// <summary>
                /// Element deprecated. 'Processing' should be used instead.
                /// </summary>
                [XmlElementAttribute("OCRProcessing")]
                //[Obsolete("Element deprecated. 'Processing' should be used instead.")]
                public AltoDescriptionOcrProcessing[] OCRProcessing
                {
                    get
                    {
                        return this.oCRProcessingField;
                    }
                    set
                    {
                        this.oCRProcessingField = value;
                    }
                }

                /// <remarks/>
                [XmlElementAttribute("Processing")]
                public AltoDescriptionProcessing[] Processings
                {
                    get
                    {
                        return this.processingField;
                    }
                    set
                    {
                        this.processingField = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] Information to identify the image file from which the OCR text was created.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoSourceImageInformation
            {

                private string fileNameField;

                private AltoFileIdentifier[] fileIdentifierField;

                private AltoDocumentIdentifier[] documentIdentifierField;

                /// <remarks/>
                [XmlElementAttribute("fileName")]
                public string FileName
                {
                    get
                    {
                        return this.fileNameField;
                    }
                    set
                    {
                        this.fileNameField = value;
                    }
                }

                /// <remarks/>
                [XmlElementAttribute("fileIdentifier")]
                public AltoFileIdentifier[] FileIdentifiers
                {
                    get
                    {
                        return this.fileIdentifierField;
                    }
                    set
                    {
                        this.fileIdentifierField = value;
                    }
                }

                /// <remarks/>
                [XmlElementAttribute("documentIdentifier")]
                public AltoDocumentIdentifier[] DocumentIdentifiers
                {
                    get
                    {
                        return this.documentIdentifierField;
                    }
                    set
                    {
                        this.documentIdentifierField = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] A unique identifier for the image file. This is drawn from MIX.
            /// 
            /// <para>This identifier must be unique within the local  
            /// To facilitate file sharing or interoperability with other systems, 
            /// fileIdentifierLocation may be added to designate the system or 
            /// application where the identifier is unique.</para>
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoFileIdentifier
            {

                private string fileIdentifierLocationField;

                private string valueField;

                /// <remarks/>
                [XmlAttributeAttribute("fileIdentifierLocation")]
                public string FileIdentifierLocation
                {
                    get
                    {
                        return this.fileIdentifierLocationField;
                    }
                    set
                    {
                        this.fileIdentifierLocationField = value;
                    }
                }

                /// <remarks/>
                [XmlTextAttribute()]
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
            /// [Alto] A white space.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoSP
            {

                private string idField;

                private float hEIGHTField;

                private bool hEIGHTFieldSpecified;

                private float wIDTHField;

                private bool wIDTHFieldSpecified;

                private float hPOSField;

                private bool hPOSFieldSpecified;

                private float vPOSField;

                private bool vPOSFieldSpecified;

                /// <remarks/>
                [XmlAttributeAttribute("ID", DataType = "ID")]
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
                [XmlAttributeAttribute("HEIGHT")]
                public float Height
                {
                    get
                    {
                        return this.hEIGHTField;
                    }
                    set
                    {
                        this.hEIGHTField = value;
                        if (!float.IsNaN(value)) this.hEIGHTFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool HeightSpecified
                {
                    get
                    {
                        return this.hEIGHTFieldSpecified;
                    }
                    set
                    {
                        this.hEIGHTFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("WIDTH")]
                public float Width
                {
                    get
                    {
                        return this.wIDTHField;
                    }
                    set
                    {
                        this.wIDTHField = value;
                        if (!float.IsNaN(value)) this.wIDTHFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool WidthSpecified
                {
                    get
                    {
                        return this.wIDTHFieldSpecified;
                    }
                    set
                    {
                        this.wIDTHFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("HPOS")]
                public float HPos
                {
                    get
                    {
                        return this.hPOSField;
                    }
                    set
                    {
                        this.hPOSField = value;
                        if (!float.IsNaN(value)) this.hPOSFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool HPosSpecified
                {
                    get
                    {
                        return this.hPOSFieldSpecified;
                    }
                    set
                    {
                        this.hPOSFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("VPOS")]
                public float VPos
                {
                    get
                    {
                        return this.vPOSField;
                    }
                    set
                    {
                        this.vPOSField = value;
                        if (!float.IsNaN(value)) this.vPOSFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool VPosSpecified
                {
                    get
                    {
                        return this.vPOSFieldSpecified;
                    }
                    set
                    {
                        this.vPOSFieldSpecified = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] Alternative (combined) character for the glyph, outlined by OCR engine or similar recognition processes.
            /// In case the variant are two (combining) characters, two characters are outlined in one Variant element.
            /// E.g. a Glyph element with CONTENT="m" can have a Variant element with the content "rn".
            /// Details for different use-cases see on the samples on GitHub.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoVariant
            {

                private string cONTENTField;

                private float vcField;

                private bool vcFieldSpecified;

                /// <summary>
                /// Each Variant represents an option for the glyph that the OCR software detected as possible alternatives.
                /// In case the variant are two(combining) characters, two characters are outlined in one Variant element.
                /// E.g.a Glyph element with CONTENT="m" can have a Variant element with the content "rn".
                /// 
                /// <para>Details for different use-cases see on the samples on GitHub.</para>
                /// </summary>
                [XmlAttributeAttribute("CONTENT")]
                public string Content
                {
                    get
                    {
                        return this.cONTENTField;
                    }
                    set
                    {
                        this.cONTENTField = value;
                    }
                }

                /// <summary>
                /// This VC attribute records a float value between 0.0 and 1.0 that expresses the level of confidence 
                /// for the variant where is 1 is certain.
                /// This attribute is optional. If it is not available, the default value for the variant is “0”.
                /// The VC attribute semantic is the same as the GC attribute on the Glyph element.
                /// </summary>
                [XmlAttributeAttribute("VC")]
                public float Vc
                {
                    get
                    {
                        return this.vcField;
                    }
                    set
                    {
                        this.vcField = value;
                        if (!float.IsNaN(value)) this.vcFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool VCSpecified
                {
                    get
                    {
                        return this.vcFieldSpecified;
                    }
                    set
                    {
                        this.vcFieldSpecified = value;
                    }
                }
            }

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
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoGlyph
            {
                private AltoShape shapeField;

                private AltoVariant[] variantField;

                private string idField;

                private string cONTENTField;

                private float gcField;

                private bool gcFieldSpecified;

                private float hEIGHTField;

                private bool hEIGHTFieldSpecified;

                private float wIDTHField;

                private bool wIDTHFieldSpecified;

                private float hPOSField;

                private bool hPOSFieldSpecified;

                private float vPOSField;

                private bool vPOSFieldSpecified;

                /// <remarks/>
                public AltoShape Shape
                {
                    get
                    {
                        return this.shapeField;
                    }
                    set
                    {
                        this.shapeField = value;
                    }
                }

                /// <summary>
                /// Alternative (combined) character for the glyph, outlined by OCR engine or similar recognition processes.
                /// In case the variant are two (combining) characters, two characters are outlined in one Variant element.
                /// E.g. a Glyph element with CONTENT="m" can have a Variant element with the content "rn".
                /// <para>Details for different use-cases see on the samples on GitHub.</para>
                /// </summary>
                [XmlElementAttribute("Variant")]
                public AltoVariant[] Variant
                {
                    get
                    {
                        return this.variantField;
                    }
                    set
                    {
                        this.variantField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("ID", DataType = "ID")]
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
                /// CONTENT contains the precomposed representation (combining character) of the character from the parent String element.
                /// The sequence position of the Gylph element matches the position of the character in the String.
                /// </summary>
                [XmlAttributeAttribute("CONTENT")]
                public string Content
                {
                    get
                    {
                        return this.cONTENTField;
                    }
                    set
                    {
                        this.cONTENTField = value;
                    }
                }

                /// <summary>
                /// This GC attribute records a float value between 0.0 and 1.0 that expresses the level of confidence for the variant where is 1 is certain.
                /// This attribute is optional. If it is not available, the default value for the variant is “0”.
                /// 
                /// <para>The GC attribute semantic is the same as the WC attribute on the String element and VC on Variant element.</para>
                /// </summary>
                [XmlAttributeAttribute("GC")]
                public float Gc
                {
                    get
                    {
                        return this.gcField;
                    }
                    set
                    {
                        this.gcField = value;
                        if (!float.IsNaN(value)) this.gcFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool GCSpecified
                {
                    get
                    {
                        return this.gcFieldSpecified;
                    }
                    set
                    {
                        this.gcFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("HEIGHT")]
                public float Height
                {
                    get
                    {
                        return this.hEIGHTField;
                    }
                    set
                    {
                        this.hEIGHTField = value;
                        if (!float.IsNaN(value)) this.hEIGHTFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool HeightSpecified
                {
                    get
                    {
                        return this.hEIGHTFieldSpecified;
                    }
                    set
                    {
                        this.hEIGHTFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("WIDTH")]
                public float Width
                {
                    get
                    {
                        return this.wIDTHField;
                    }
                    set
                    {
                        this.wIDTHField = value;
                        if (!float.IsNaN(value)) this.wIDTHFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool WidthSpecified
                {
                    get
                    {
                        return this.wIDTHFieldSpecified;
                    }
                    set
                    {
                        this.wIDTHFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("HPOS")]
                public float HPos
                {
                    get
                    {
                        return this.hPOSField;
                    }
                    set
                    {
                        this.hPOSField = value;
                        if (!float.IsNaN(value)) this.hPOSFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool HPosSpecified
                {
                    get
                    {
                        return this.hPOSFieldSpecified;
                    }
                    set
                    {
                        this.hPOSFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("VPOS")]
                public float VPos
                {
                    get
                    {
                        return this.vPOSField;
                    }
                    set
                    {
                        this.vPOSField = value;
                        if (!float.IsNaN(value)) this.vPOSFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool VPosSpecified
                {
                    get
                    {
                        return this.vPOSFieldSpecified;
                    }
                    set
                    {
                        this.vPOSFieldSpecified = value;
                    }
                }

                /// <remarks/>
                public override string ToString()
                {
                    return this.Content;
                }
            }

            /// <summary>
            /// [Alto] Describes the bounding shape of a block, if it is not rectangular.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoShape
            {

                private object itemField;

                /// <remarks/>
                [XmlElementAttribute("Circle", typeof(AltoCircle))]
                [XmlElementAttribute("Ellipse", typeof(AltoEllipse))]
                [XmlElementAttribute("Polygon", typeof(AltoPolygon))]
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
            }

            /// <summary>
            /// [Alto] A circle shape. HPOS and VPOS describe the center of the circle.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoCircle
            {

                private float hPOSField;

                private float vPOSField;

                private float rADIUSField;

                /// <remarks/>
                [XmlAttributeAttribute("HPOS")]
                public float HPos
                {
                    get
                    {
                        return this.hPOSField;
                    }
                    set
                    {
                        this.hPOSField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("VPOS")]
                public float VPos
                {
                    get
                    {
                        return this.vPOSField;
                    }
                    set
                    {
                        this.vPOSField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("RADIUS")]
                public float Radius
                {
                    get
                    {
                        return this.rADIUSField;
                    }
                    set
                    {
                        this.rADIUSField = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] An ellipse shape. HPOS and VPOS describe the center of the ellipse.
            /// HLENGTH and VLENGTH are the width and height of the described ellipse.
            /// <para>The attribute ROTATION tells the rotation of the e.g. text or 
            /// illustration within the block.The value is in degrees counterclockwise.</para>
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoEllipse
            {

                private float hPOSField;

                private float vPOSField;

                private float hLENGTHField;

                private float vLENGTHField;

                private float rOTATIONField;

                private bool rOTATIONFieldSpecified;

                /// <remarks/>
                [XmlAttributeAttribute("HPOS")]
                public float HPos
                {
                    get
                    {
                        return this.hPOSField;
                    }
                    set
                    {
                        this.hPOSField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("VPOS")]
                public float VPos
                {
                    get
                    {
                        return this.vPOSField;
                    }
                    set
                    {
                        this.vPOSField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("HLENGTH")]
                public float HLength
                {
                    get
                    {
                        return this.hLENGTHField;
                    }
                    set
                    {
                        this.hLENGTHField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("VLENGTH")]
                public float VLength
                {
                    get
                    {
                        return this.vLENGTHField;
                    }
                    set
                    {
                        this.vLENGTHField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("ROTATION")]
                public float Rotation
                {
                    get
                    {
                        return this.rOTATIONField;
                    }
                    set
                    {
                        this.rOTATIONField = value;
                        if (!float.IsNaN(value)) this.rOTATIONFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool ROTATIONSpecified
                {
                    get
                    {
                        return this.rOTATIONFieldSpecified;
                    }
                    set
                    {
                        this.rOTATIONFieldSpecified = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] A polygon shape.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoPolygon
            {

                private string pOINTSField;

                /// <remarks/>
                [XmlAttributeAttribute("POINTS")]
                public string Points
                {
                    get
                    {
                        return this.pOINTSField;
                    }
                    set
                    {
                        this.pOINTSField = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] Alternative
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoAlternative
            {

                private string pURPOSEField;

                private string valueField;

                /// <remarks/>
                [XmlAttributeAttribute("PURPOSE")]
                public string Purpose
                {
                    get
                    {
                        return this.pURPOSEField;
                    }
                    set
                    {
                        this.pURPOSEField = value;
                    }
                }

                /// <remarks/>
                [XmlTextAttribute()]
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
            /// [Alto] A sequence of chars. Strings are separated by white spaces or hyphenation chars.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoString
            {

                private AltoShape shapeField;

                private AltoAlternative[] aLTERNATIVEField;

                private AltoGlyph[] glyphField;

                private string idField;

                private string sTYLEREFSField;

                private string tAGREFSField;

                private string pROCESSINGREFSField;

                private float hEIGHTField;

                private bool hEIGHTFieldSpecified;

                private float wIDTHField;

                private bool wIDTHFieldSpecified;

                private float hPOSField;

                private bool hPOSFieldSpecified;

                private float vPOSField;

                private bool vPOSFieldSpecified;

                private string cONTENTField;

                private AltoFontStyles sTYLEField;

                private bool sTYLEFieldSpecified;

                private AltoSubsType sUBS_TYPEField;

                private bool sUBS_TYPEFieldSpecified;

                private string sUBS_CONTENTField;

                private float wcField;

                private bool wcFieldSpecified;

                private string ccField;

                private bool csField;

                private bool csFieldSpecified;

                private string lANGField;

                /// <remarks/>
                public AltoShape Shape
                {
                    get
                    {
                        return this.shapeField;
                    }
                    set
                    {
                        this.shapeField = value;
                    }
                }

                /// <remarks/>
                [XmlElementAttribute("ALTERNATIVE")]
                public AltoAlternative[] Alternative
                {
                    get
                    {
                        return this.aLTERNATIVEField;
                    }
                    set
                    {
                        this.aLTERNATIVEField = value;
                    }
                }

                /// <remarks/>
                [XmlElementAttribute("Glyph")]
                public AltoGlyph[] Glyph
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
                [XmlAttributeAttribute("ID", DataType = "ID")]
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
                [XmlAttributeAttribute("STYLEREFS", DataType = "IDREFS")]
                public string StyleRefs
                {
                    get
                    {
                        return this.sTYLEREFSField;
                    }
                    set
                    {
                        this.sTYLEREFSField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("TAGREFS", DataType = "IDREFS")]
                public string TagRefs
                {
                    get
                    {
                        return this.tAGREFSField;
                    }
                    set
                    {
                        this.tAGREFSField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("PROCESSINGREFS", DataType = "IDREFS")]
                public string ProcessingRefs
                {
                    get
                    {
                        return this.pROCESSINGREFSField;
                    }
                    set
                    {
                        this.pROCESSINGREFSField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("HEIGHT")]
                public float Height
                {
                    get
                    {
                        return this.hEIGHTField;
                    }
                    set
                    {
                        this.hEIGHTField = value;
                        if (!float.IsNaN(value)) this.hEIGHTFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool HeightSpecified
                {
                    get
                    {
                        return this.hEIGHTFieldSpecified;
                    }
                    set
                    {
                        this.hEIGHTFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("WIDTH")]
                public float Width
                {
                    get
                    {
                        return this.wIDTHField;
                    }
                    set
                    {
                        this.wIDTHField = value;
                        if (!float.IsNaN(value)) this.wIDTHFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool WidthSpecified
                {
                    get
                    {
                        return this.wIDTHFieldSpecified;
                    }
                    set
                    {
                        this.wIDTHFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("HPOS")]
                public float HPos
                {
                    get
                    {
                        return this.hPOSField;
                    }
                    set
                    {
                        this.hPOSField = value;
                        if (!float.IsNaN(value)) this.hPOSFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool HPosSpecified
                {
                    get
                    {
                        return this.hPOSFieldSpecified;
                    }
                    set
                    {
                        this.hPOSFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("VPOS")]
                public float VPos
                {
                    get
                    {
                        return this.vPOSField;
                    }
                    set
                    {
                        this.vPOSField = value;
                        if (!float.IsNaN(value)) this.vPOSFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool VPosSpecified
                {
                    get
                    {
                        return this.vPOSFieldSpecified;
                    }
                    set
                    {
                        this.vPOSFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("CONTENT")]
                public string Content
                {
                    get
                    {
                        return this.cONTENTField;
                    }
                    set
                    {
                        this.cONTENTField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("STYLE")]
                public AltoFontStyles Style
                {
                    get
                    {
                        return this.sTYLEField;
                    }
                    set
                    {
                        this.sTYLEField = value;
                        this.sTYLEFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool StyleSpecified
                {
                    get
                    {
                        return this.sTYLEFieldSpecified;
                    }
                    set
                    {
                        this.sTYLEFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("SUBS_TYPE")]
                public AltoSubsType SubsType
                {
                    get
                    {
                        return this.sUBS_TYPEField;
                    }
                    set
                    {
                        this.sUBS_TYPEField = value;
                        this.sUBS_TYPEFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool SubsTypeSpecified
                {
                    get
                    {
                        return this.sUBS_TYPEFieldSpecified;
                    }
                    set
                    {
                        this.sUBS_TYPEFieldSpecified = value;
                    }
                }

                /// <summary>
                /// Content of the substitution.
                /// </summary>
                [XmlAttributeAttribute("SUBS_CONTENT")]
                public string SubsContent
                {
                    get
                    {
                        return this.sUBS_CONTENTField;
                    }
                    set
                    {
                        this.sUBS_CONTENTField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("WC")]
                public float Wc
                {
                    get
                    {
                        return this.wcField;
                    }
                    set
                    {
                        this.wcField = value;
                        if (!float.IsNaN(value)) this.wcFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool WcSpecified
                {
                    get
                    {
                        return this.wcFieldSpecified;
                    }
                    set
                    {
                        this.wcFieldSpecified = value;
                    }
                }

                /// <summary>
                /// Confidence level of each character in that string. A list of numbers,
                /// one number between 0 (sure) and 9 (unsure) for each character.
                /// </summary>
                [XmlAttributeAttribute("CC")]
                public string Cc
                {
                    get
                    {
                        return this.ccField;
                    }
                    set
                    {
                        this.ccField = value;
                    }
                }

                /// <summary>
                /// Correction Status. Indicates whether manual correction has been done or not. 
                /// The correction status should be recorded at the highest level possible (Block, TextLine, String).
                /// </summary>
                [XmlAttributeAttribute("CS")]
                public bool Cs
                {
                    get
                    {
                        return this.csField;
                    }
                    set
                    {
                        this.csField = value;
                        this.csFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool CsSpecified
                {
                    get
                    {
                        return this.csFieldSpecified;
                    }
                    set
                    {
                        this.csFieldSpecified = value;
                    }
                }

                /// <summary>
                /// Attribute to record language of the string. The language should be recorded at the highest level possible.
                /// </summary>
                [XmlAttributeAttribute("LANG", DataType = "language")]
                public string Lang
                {
                    get
                    {
                        return this.lANGField;
                    }
                    set
                    {
                        this.lANGField = value;
                    }
                }

                /// <remarks/>
                public override string ToString()
                {
                    return this.Content;
                }
            }

            /// <summary>
            /// [Alto] Base type for any kind of block on the page.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [XmlIncludeAttribute(typeof(AltoTextBlock))]
            [XmlIncludeAttribute(typeof(AltoGraphicalElement))]
            [XmlIncludeAttribute(typeof(AltoIllustration))]
            [XmlIncludeAttribute(typeof(AltoComposedBlock))]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoBlock
            {

                private AltoShape shapeField;

                private string idField;

                private string sTYLEREFSField;

                private string tAGREFSField;

                private string pROCESSINGREFSField;

                private float hEIGHTField;

                private bool hEIGHTFieldSpecified;

                private float wIDTHField;

                private bool wIDTHFieldSpecified;

                private float hPOSField;

                private bool hPOSFieldSpecified;

                private float vPOSField;

                private bool vPOSFieldSpecified;

                private float rOTATIONField;

                private bool rOTATIONFieldSpecified;

                private string iDNEXTField;

                private bool csField;

                private bool csFieldSpecified;

                private string typeField;

                private string hrefField;

                private string roleField;

                private string arcroleField;

                private string titleField;

                private AltoBlockTypeShow showField;

                private bool showFieldSpecified;

                private AltoBlockTypeActuate actuateField;

                private bool actuateFieldSpecified;

                /// <remarks/>
                public AltoBlock()
                {
                    this.typeField = "simple";
                }

                /// <remarks/>
                public AltoShape Shape
                {
                    get
                    {
                        return this.shapeField;
                    }
                    set
                    {
                        this.shapeField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("ID", DataType = "ID")]
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
                [XmlAttributeAttribute("STYLEREFS", DataType = "IDREFS")]
                public string StyleRefs
                {
                    get
                    {
                        return this.sTYLEREFSField;
                    }
                    set
                    {
                        this.sTYLEREFSField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("TAGREFS", DataType = "IDREFS")]
                public string TagRefs
                {
                    get
                    {
                        return this.tAGREFSField;
                    }
                    set
                    {
                        this.tAGREFSField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("PROCESSINGREFS", DataType = "IDREFS")]
                public string ProcessingRefs
                {
                    get
                    {
                        return this.pROCESSINGREFSField;
                    }
                    set
                    {
                        this.pROCESSINGREFSField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("HEIGHT")]
                public float Height
                {
                    get
                    {
                        return this.hEIGHTField;
                    }
                    set
                    {
                        this.hEIGHTField = value;
                        if (!float.IsNaN(value)) this.hEIGHTFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool HeightSpecified
                {
                    get
                    {
                        return this.hEIGHTFieldSpecified;
                    }
                    set
                    {
                        this.hEIGHTFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("WIDTH")]
                public float Width
                {
                    get
                    {
                        return this.wIDTHField;
                    }
                    set
                    {
                        this.wIDTHField = value;
                        if (!float.IsNaN(value)) this.wIDTHFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool WidthSpecified
                {
                    get
                    {
                        return this.wIDTHFieldSpecified;
                    }
                    set
                    {
                        this.wIDTHFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("HPOS")]
                public float HPos
                {
                    get
                    {
                        return this.hPOSField;
                    }
                    set
                    {
                        this.hPOSField = value;
                        if (!float.IsNaN(value)) this.hPOSFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool HPosSpecified
                {
                    get
                    {
                        return this.hPOSFieldSpecified;
                    }
                    set
                    {
                        this.hPOSFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("VPOS")]
                public float VPos
                {
                    get
                    {
                        return this.vPOSField;
                    }
                    set
                    {
                        this.vPOSField = value;
                        if (!float.IsNaN(value)) this.vPOSFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool VPosSpecified
                {
                    get
                    {
                        return this.vPOSFieldSpecified;
                    }
                    set
                    {
                        this.vPOSFieldSpecified = value;
                    }
                }

                /// <summary>
                /// Tells the rotation of e.g. text or illustration within the block. The value is in degree counterclockwise.
                /// </summary>
                [XmlAttributeAttribute("ROTATION")]
                public float Rotation
                {
                    get
                    {
                        return this.rOTATIONField;
                    }
                    set
                    {
                        this.rOTATIONField = value;
                        if (!float.IsNaN(value)) this.rOTATIONFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool RotationSpecified
                {
                    get
                    {
                        return this.rOTATIONFieldSpecified;
                    }
                    set
                    {
                        this.rOTATIONFieldSpecified = value;
                    }
                }

                /// <summary>
                /// The next block in reading sequence on the page.
                /// </summary>
                [XmlAttributeAttribute("IDNEXT", DataType = "IDREF")]
                public string IdNext
                {
                    get
                    {
                        return this.iDNEXTField;
                    }
                    set
                    {
                        this.iDNEXTField = value;
                    }
                }

                /// <summary>
                /// Correction Status. Indicates whether manual correction has been done or not. 
                /// The correction status should be recorded at the highest level possible (Block, TextLine, String).
                /// </summary>
                [XmlAttributeAttribute("CS")]
                public bool Cs
                {
                    get
                    {
                        return this.csField;
                    }
                    set
                    {
                        this.csField = value;
                        this.csFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool CsSpecified
                {
                    get
                    {
                        return this.csFieldSpecified;
                    }
                    set
                    {
                        this.csFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("type", Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
                public string type
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
                [XmlAttributeAttribute("href", Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink", DataType = "anyURI")]
                public string href
                {
                    get
                    {
                        return this.hrefField;
                    }
                    set
                    {
                        this.hrefField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("role", Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
                public string role
                {
                    get
                    {
                        return this.roleField;
                    }
                    set
                    {
                        this.roleField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("arcrole", Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
                public string arcrole
                {
                    get
                    {
                        return this.arcroleField;
                    }
                    set
                    {
                        this.arcroleField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("title", Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
                public string title
                {
                    get
                    {
                        return this.titleField;
                    }
                    set
                    {
                        this.titleField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("show", Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
                public AltoBlockTypeShow show
                {
                    get
                    {
                        return this.showField;
                    }
                    set
                    {
                        this.showField = value;
                        this.showFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool showSpecified
                {
                    get
                    {
                        return this.showFieldSpecified;
                    }
                    set
                    {
                        this.showFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("actuate", Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/1999/xlink")]
                public AltoBlockTypeActuate actuate
                {
                    get
                    {
                        return this.actuateField;
                    }
                    set
                    {
                        this.actuateField = value;
                        this.actuateFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool actuateSpecified
                {
                    get
                    {
                        return this.actuateFieldSpecified;
                    }
                    set
                    {
                        this.actuateFieldSpecified = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] A block of text.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoTextBlock : AltoBlock
            {

                private AltoTextBlockTextLine[] textLineField;

                private string languageField;

                private string lANGField;

                /// <remarks/>
                [XmlElementAttribute("TextLine")]
                public AltoTextBlockTextLine[] TextLines
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

                /// <summary>
                /// Attribute deprecated. LANG should be used instead.
                /// </summary>
                [XmlAttributeAttribute("language", DataType = "language")]
                //[Obsolete("Attribute deprecated. LANG should be used instead.")]
                public string Language
                {
                    get
                    {
                        return this.languageField;
                    }
                    set
                    {
                        this.languageField = value;
                    }
                }

                /// <summary>
                /// Attribute to record language of the textblock.
                /// </summary>
                [XmlAttributeAttribute("LANG", DataType = "language")]
                public string Lang
                {
                    get
                    {
                        return this.lANGField;
                    }
                    set
                    {
                        this.lANGField = value;
                    }
                }

                /// <remarks/>
                public override string ToString()
                {
                    return string.Join<AltoTextBlockTextLine>(" ", this.TextLines);
                }
            }

            /// <summary>
            /// [Alto] A single line of text.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoTextBlockTextLine
            {

                private AltoShape shapeField;

                private AltoString[] stringField;

                private AltoSP[] spField;

                private AltoTextBlockTextLineHyp hYPField;

                private string idField;

                private string sTYLEREFSField;

                private string tAGREFSField;

                private string pROCESSINGREFSField;

                private float hEIGHTField;

                private bool hEIGHTFieldSpecified;

                private float wIDTHField;

                private bool wIDTHFieldSpecified;

                private float hPOSField;

                private bool hPOSFieldSpecified;

                private float vPOSField;

                private bool vPOSFieldSpecified;

                private float bASELINEField;

                private bool bASELINEFieldSpecified;

                private string lANGField;

                private bool csField;

                private bool csFieldSpecified;

                /// <remarks/>
                public AltoShape Shape
                {
                    get
                    {
                        return this.shapeField;
                    }
                    set
                    {
                        this.shapeField = value;
                    }
                }

                /// <remarks/>
                [XmlElementAttribute("String")]
                public AltoString[] Strings
                {
                    get
                    {
                        return this.stringField;
                    }
                    set
                    {
                        this.stringField = value;
                    }
                }

                /// <remarks/>
                [XmlElementAttribute("SP")]
                public AltoSP[] Sp
                {
                    get
                    {
                        return this.spField;
                    }
                    set
                    {
                        this.spField = value;
                    }
                }

                /// <summary>
                /// A hyphenation char. Can appear only at the end of a line.
                /// </summary>
                [XmlElementAttribute("HYP")]
                public AltoTextBlockTextLineHyp Hyp
                {
                    get
                    {
                        return this.hYPField;
                    }
                    set
                    {
                        this.hYPField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("ID", DataType = "ID")]
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
                [XmlAttributeAttribute("STYLEREFS", DataType = "IDREFS")]
                public string StyleRefs
                {
                    get
                    {
                        return this.sTYLEREFSField;
                    }
                    set
                    {
                        this.sTYLEREFSField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("TAGREFS", DataType = "IDREFS")]
                public string TagRefs
                {
                    get
                    {
                        return this.tAGREFSField;
                    }
                    set
                    {
                        this.tAGREFSField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("PROCESSINGREFS", DataType = "IDREFS")]
                public string ProcessingRefs
                {
                    get
                    {
                        return this.pROCESSINGREFSField;
                    }
                    set
                    {
                        this.pROCESSINGREFSField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("HEIGHT")]
                public float Height
                {
                    get
                    {
                        return this.hEIGHTField;
                    }
                    set
                    {
                        this.hEIGHTField = value;
                        if (!float.IsNaN(value)) this.hEIGHTFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool HeightSpecified
                {
                    get
                    {
                        return this.hEIGHTFieldSpecified;
                    }
                    set
                    {
                        this.hEIGHTFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("WIDTH")]
                public float Width
                {
                    get
                    {
                        return this.wIDTHField;
                    }
                    set
                    {
                        this.wIDTHField = value;
                        if (!float.IsNaN(value)) this.wIDTHFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool WidthSpecified
                {
                    get
                    {
                        return this.wIDTHFieldSpecified;
                    }
                    set
                    {
                        this.wIDTHFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("HPOS")]
                public float HPos
                {
                    get
                    {
                        return this.hPOSField;
                    }
                    set
                    {
                        this.hPOSField = value;
                        if (!float.IsNaN(value)) this.hPOSFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool HPosSpecified
                {
                    get
                    {
                        return this.hPOSFieldSpecified;
                    }
                    set
                    {
                        this.hPOSFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("VPOS")]
                public float VPos
                {
                    get
                    {
                        return this.vPOSField;
                    }
                    set
                    {
                        this.vPOSField = value;
                        if (!float.IsNaN(value)) this.vPOSFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool VPosSpecified
                {
                    get
                    {
                        return this.vPOSFieldSpecified;
                    }
                    set
                    {
                        this.vPOSFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("BASELINE")]
                public float BaseLine
                {
                    get
                    {
                        return this.bASELINEField;
                    }
                    set
                    {
                        this.bASELINEField = value;
                        if (!float.IsNaN(value)) this.bASELINEFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool BaseLineSpecified
                {
                    get
                    {
                        return this.bASELINEFieldSpecified;
                    }
                    set
                    {
                        this.bASELINEFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("LANG", DataType = "language")]
                public string Lang
                {
                    get
                    {
                        return this.lANGField;
                    }
                    set
                    {
                        this.lANGField = value;
                    }
                }

                /// <summary>
                /// Correction Status. Indicates whether manual correction has been done or not. 
                /// The correction status should be recorded at the highest level possible (Block, TextLine, String).
                /// </summary>
                [XmlAttributeAttribute("CS")]
                public bool Cs
                {
                    get
                    {
                        return this.csField;
                    }
                    set
                    {
                        this.csField = value;
                        this.csFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool CsSpecified
                {
                    get
                    {
                        return this.csFieldSpecified;
                    }
                    set
                    {
                        this.csFieldSpecified = value;
                    }
                }

                /// <remarks/>
                public override string ToString()
                {
                    return string.Join<AltoString>(" ", this.Strings); // take in account order?
                }
            }

            /// <summary>
            /// [Alto] A hyphenation char. Can appear only at the end of a line.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoTextBlockTextLineHyp
            {

                private float hEIGHTField;

                private bool hEIGHTFieldSpecified;

                private float wIDTHField;

                private bool wIDTHFieldSpecified;

                private float hPOSField;

                private bool hPOSFieldSpecified;

                private float vPOSField;

                private bool vPOSFieldSpecified;

                private string cONTENTField;

                /// <remarks/>
                [XmlAttributeAttribute("HEIGHT")]
                public float Height
                {
                    get
                    {
                        return this.hEIGHTField;
                    }
                    set
                    {
                        this.hEIGHTField = value;
                        if (!float.IsNaN(value)) this.hEIGHTFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool HeightSpecified
                {
                    get
                    {
                        return this.hEIGHTFieldSpecified;
                    }
                    set
                    {
                        this.hEIGHTFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("WIDTH")]
                public float Width
                {
                    get
                    {
                        return this.wIDTHField;
                    }
                    set
                    {
                        this.wIDTHField = value;
                        if (!float.IsNaN(value)) this.wIDTHFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool WidthSpecified
                {
                    get
                    {
                        return this.wIDTHFieldSpecified;
                    }
                    set
                    {
                        this.wIDTHFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("HPOS")]
                public float HPos
                {
                    get
                    {
                        return this.hPOSField;
                    }
                    set
                    {
                        this.hPOSField = value;
                        if (!float.IsNaN(value)) this.hPOSFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool HPosSpecified
                {
                    get
                    {
                        return this.hPOSFieldSpecified;
                    }
                    set
                    {
                        this.hPOSFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("VPOS")]
                public float VPos
                {
                    get
                    {
                        return this.vPOSField;
                    }
                    set
                    {
                        this.vPOSField = value;
                        if (!float.IsNaN(value)) this.vPOSFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool VPosSpecified
                {
                    get
                    {
                        return this.vPOSFieldSpecified;
                    }
                    set
                    {
                        this.vPOSFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("CONTENT")]
                public string Content
                {
                    get
                    {
                        return this.cONTENTField;
                    }
                    set
                    {
                        this.cONTENTField = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] A graphic used to separate blocks. Usually a line or rectangle.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoGraphicalElement : AltoBlock
            {
            }

            /// <summary>
            /// [Alto] A picture or image.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoIllustration : AltoBlock
            {

                private string tYPEField;

                private string fILEIDField;

                /// <summary>
                /// A user defined string to identify the type of illustration like photo, map, drawing, chart, ...
                /// </summary>
                [XmlAttributeAttribute("TYPE")]
                public string Type
                {
                    get
                    {
                        return this.tYPEField;
                    }
                    set
                    {
                        this.tYPEField = value;
                    }
                }

                /// <summary>
                /// A link to an image which contains only the illustration.
                /// </summary>
                [XmlAttributeAttribute("FILEID")]
                public string FileId
                {
                    get
                    {
                        return this.fILEIDField;
                    }
                    set
                    {
                        this.fILEIDField = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] A block that consists of other blocks.
            /// <para>WARNING: The CIRCULAR GROUP REFERENCES was removed from the xsd.
            /// NEED TO ADD IT BACK!!!</para>
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoComposedBlock : AltoBlock
            {

                /*****************************************************************
                 * /!\                         WARNING                         /!\
                 * The CIRCULAR GROUP REFERENCES below was removed from the xsd
                 * NEED TO ADD IT BACK!!!
                 * <xsd:sequence minOccurs="0" maxOccurs="unbounded">
                 *      <xsd:group ref="BlockGroup"/>
                 * </xsd:sequence> 
                 *****************************************************************/

                private string tYPEField;

                private string fILEIDField;

                /// <summary>
                /// A user defined string to identify the type of composed block (e.g. table, advertisement, ...)
                /// </summary>
                [XmlAttributeAttribute("TYPE")]
                public string Type
                {
                    get
                    {
                        return this.tYPEField;
                    }
                    set
                    {
                        this.tYPEField = value;
                    }
                }

                /// <summary>
                /// An ID to link to an image which contains only the composed block. 
                /// The ID and the file link is defined in the related METS file.
                /// </summary>
                [XmlAttributeAttribute("FILEID")]
                public string FileId
                {
                    get
                    {
                        return this.fILEIDField;
                    }
                    set
                    {
                        this.fILEIDField = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] A region on a page
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoPageSpace
            {

                private AltoShape shapeField;

                private AltoTextBlock[] textBlockField;

                private AltoIllustration[] illustrationField;

                private AltoGraphicalElement[] graphicalElementField;

                private AltoComposedBlock[] composedBlockField;

                private string idField;

                private string sTYLEREFSField;

                private string pROCESSINGREFSField;

                private float hEIGHTField;

                private bool hEIGHTFieldSpecified;

                private float wIDTHField;

                private bool wIDTHFieldSpecified;

                private float hPOSField;

                private bool hPOSFieldSpecified;

                private float vPOSField;

                private bool vPOSFieldSpecified;

                /// <summary>
                /// 
                /// </summary>
                public AltoShape Shape
                {
                    get
                    {
                        return this.shapeField;
                    }
                    set
                    {
                        this.shapeField = value;
                    }
                }

                /// <remarks/>
                [XmlElementAttribute("TextBlock")]
                public AltoTextBlock[] TextBlock
                {
                    get
                    {
                        return this.textBlockField;
                    }
                    set
                    {
                        this.textBlockField = value;
                    }
                }

                /// <remarks/>
                [XmlElementAttribute("Illustration")]
                public AltoIllustration[] Illustrations
                {
                    get
                    {
                        return this.illustrationField;
                    }
                    set
                    {
                        this.illustrationField = value;
                    }
                }

                /// <remarks/>
                [XmlElementAttribute("GraphicalElement")]
                public AltoGraphicalElement[] GraphicalElements
                {
                    get
                    {
                        return this.graphicalElementField;
                    }
                    set
                    {
                        this.graphicalElementField = value;
                    }
                }

                /// <remarks/>
                [XmlElementAttribute("ComposedBlock")]
                public AltoComposedBlock[] ComposedBlocks
                {
                    get
                    {
                        return this.composedBlockField;
                    }
                    set
                    {
                        this.composedBlockField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("ID", DataType = "ID")]
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
                [XmlAttributeAttribute("STYLEREFS", DataType = "IDREFS")]
                public string StyleRefs
                {
                    get
                    {
                        return this.sTYLEREFSField;
                    }
                    set
                    {
                        this.sTYLEREFSField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("PROCESSINGREFS", DataType = "IDREFS")]
                public string ProcessingRefs
                {
                    get
                    {
                        return this.pROCESSINGREFSField;
                    }
                    set
                    {
                        this.pROCESSINGREFSField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("HEIGHT")]
                public float Height
                {
                    get
                    {
                        return this.hEIGHTField;
                    }
                    set
                    {
                        this.hEIGHTField = value;
                        if (!float.IsNaN(value)) this.hEIGHTFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool HeightSpecified
                {
                    get
                    {
                        return this.hEIGHTFieldSpecified;
                    }
                    set
                    {
                        this.hEIGHTFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("WIDTH")]
                public float Width
                {
                    get
                    {
                        return this.wIDTHField;
                    }
                    set
                    {
                        this.wIDTHField = value;
                        if (!float.IsNaN(value)) this.wIDTHFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool WidthSpecified
                {
                    get
                    {
                        return this.wIDTHFieldSpecified;
                    }
                    set
                    {
                        this.wIDTHFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("HPOS")]
                public float HPos
                {
                    get
                    {
                        return this.hPOSField;
                    }
                    set
                    {
                        this.hPOSField = value;
                        if (!float.IsNaN(value)) this.hPOSFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool HPosSpecified
                {
                    get
                    {
                        return this.hPOSFieldSpecified;
                    }
                    set
                    {
                        this.hPOSFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("VPOS")]
                public float VPos
                {
                    get
                    {
                        return this.vPOSField;
                    }
                    set
                    {
                        this.vPOSField = value;
                        if (!float.IsNaN(value)) this.vPOSFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool VPosSpecified
                {
                    get
                    {
                        return this.vPOSFieldSpecified;
                    }
                    set
                    {
                        this.vPOSFieldSpecified = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] One page of a book or journal.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoPage
            {

                private AltoPageSpace topMarginField;

                private AltoPageSpace leftMarginField;

                private AltoPageSpace rightMarginField;

                private AltoPageSpace bottomMarginField;

                private AltoPageSpace printSpaceField;

                private string idField;

                private string pAGECLASSField;

                private string sTYLEREFSField;

                private string pROCESSINGREFSField;

                private float hEIGHTField;

                private bool hEIGHTFieldSpecified;

                private float wIDTHField;

                private bool wIDTHFieldSpecified;

                private float pHYSICAL_IMG_NRField;

                private string pRINTED_IMG_NRField;

                private AltoQuality qUALITYField;

                private bool qUALITYFieldSpecified;

                private string qUALITY_DETAILField;

                private AltoPosition pOSITIONField;

                private bool pOSITIONFieldSpecified;

                private string pROCESSINGField;

                private float aCCURACYField;

                private bool aCCURACYFieldSpecified;

                private float pcField;

                private bool pcFieldSpecified;

                /// <summary>
                /// The area between the top line of print and the upper edge of the leaf. It may contain page number or running title.
                /// </summary>
                public AltoPageSpace TopMargin
                {
                    get
                    {
                        return this.topMarginField;
                    }
                    set
                    {
                        this.topMarginField = value;
                    }
                }

                /// <summary>
                /// The area between the printspace and the left border of a page. May contain margin notes.
                /// </summary>
                public AltoPageSpace LeftMargin
                {
                    get
                    {
                        return this.leftMarginField;
                    }
                    set
                    {
                        this.leftMarginField = value;
                    }
                }

                /// <summary>
                /// The area between the printspace and the right border of a page. May contain margin notes.
                /// </summary>
                public AltoPageSpace RightMargin
                {
                    get
                    {
                        return this.rightMarginField;
                    }
                    set
                    {
                        this.rightMarginField = value;
                    }
                }

                /// <summary>
                /// The area between the bottom line of letterpress or writing and the bottom edge of the leaf.
                /// It may contain a page number, a signature number or a catch word.
                /// </summary>
                public AltoPageSpace BottomMargin
                {
                    get
                    {
                        return this.bottomMarginField;
                    }
                    set
                    {
                        this.bottomMarginField = value;
                    }
                }

                /// <summary>
                /// Rectangle covering the printed area of a page. Page number and running title are not part of the print space.
                /// </summary>
                public AltoPageSpace PrintSpace
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

                /// <remarks/>
                [XmlAttributeAttribute("ID", DataType = "ID")]
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
                /// Any user-defined class like title page.
                /// </summary>
                [XmlAttributeAttribute("PAGECLASS")]
                public string PageClass
                {
                    get
                    {
                        return this.pAGECLASSField;
                    }
                    set
                    {
                        this.pAGECLASSField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("STYLEREFS", DataType = "IDREFS")]
                public string StyleRefs
                {
                    get
                    {
                        return this.sTYLEREFSField;
                    }
                    set
                    {
                        this.sTYLEREFSField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("PROCESSINGREFS", DataType = "IDREFS")]
                public string ProcessingRefs
                {
                    get
                    {
                        return this.pROCESSINGREFSField;
                    }
                    set
                    {
                        this.pROCESSINGREFSField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("HEIGHT")]
                public float Height
                {
                    get
                    {
                        return this.hEIGHTField;
                    }
                    set
                    {
                        this.hEIGHTField = value;
                        if (!float.IsNaN(value)) this.hEIGHTFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool HeightSpecified
                {
                    get
                    {
                        return this.hEIGHTFieldSpecified;
                    }
                    set
                    {
                        this.hEIGHTFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("WIDTH")]
                public float Width
                {
                    get
                    {
                        return this.wIDTHField;
                    }
                    set
                    {
                        this.wIDTHField = value;
                        if (!float.IsNaN(value)) this.wIDTHFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool WidthSpecified
                {
                    get
                    {
                        return this.wIDTHFieldSpecified;
                    }
                    set
                    {
                        this.wIDTHFieldSpecified = value;
                    }
                }

                /// <summary>
                /// The number of the page within the document.
                /// </summary>
                [XmlAttributeAttribute("PHYSICAL_IMG_NR")]
                public float PhysicalImgNr
                {
                    get
                    {
                        return this.pHYSICAL_IMG_NRField;
                    }
                    set
                    {
                        this.pHYSICAL_IMG_NRField = value;
                    }
                }

                /// <summary>
                /// The page number that is printed on the page.
                /// </summary>
                [XmlAttributeAttribute("PRINTED_IMG_NR")]
                public string PrintedImgNr
                {
                    get
                    {
                        return this.pRINTED_IMG_NRField;
                    }
                    set
                    {
                        this.pRINTED_IMG_NRField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("QUALITY")]
                public AltoQuality Quality
                {
                    get
                    {
                        return this.qUALITYField;
                    }
                    set
                    {
                        this.qUALITYField = value;
                        this.qUALITYFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool QualitySpecified
                {
                    get
                    {
                        return this.qUALITYFieldSpecified;
                    }
                    set
                    {
                        this.qUALITYFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("QUALITY_DETAIL")]
                public string QualityDetail
                {
                    get
                    {
                        return this.qUALITY_DETAILField;
                    }
                    set
                    {
                        this.qUALITY_DETAILField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("POSITION")]
                public AltoPosition Position
                {
                    get
                    {
                        return this.pOSITIONField;
                    }
                    set
                    {
                        this.pOSITIONField = value;
                        this.pOSITIONFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool PositionSpecified
                {
                    get
                    {
                        return this.pOSITIONFieldSpecified;
                    }
                    set
                    {
                        this.pOSITIONFieldSpecified = value;
                    }
                }

                /// <summary>
                /// A link to the processing description that has been used for this page.
                /// </summary>
                [XmlAttributeAttribute("PROCESSING", DataType = "IDREF")]
                public string Processing
                {
                    get
                    {
                        return this.pROCESSINGField;
                    }
                    set
                    {
                        this.pROCESSINGField = value;
                    }
                }

                /// <summary>
                /// Estimated percentage of OCR Accuracy in range from 0 to 100
                /// </summary>
                [XmlAttributeAttribute("ACCURACY")]
                public float Accuracy
                {
                    get
                    {
                        return this.aCCURACYField;
                    }
                    set
                    {
                        this.aCCURACYField = value;
                        if (!float.IsNaN(value)) this.aCCURACYFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool AccuracySpecified
                {
                    get
                    {
                        return this.aCCURACYFieldSpecified;
                    }
                    set
                    {
                        this.aCCURACYFieldSpecified = value;
                    }
                }

                /// <summary>
                /// 
                /// </summary>
                [XmlAttributeAttribute("PC")]
                public float Pc
                {
                    get
                    {
                        return this.pcField;
                    }
                    set
                    {
                        this.pcField = value;
                        if (!float.IsNaN(value)) this.pcFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool PcSpecified
                {
                    get
                    {
                        return this.pcFieldSpecified;
                    }
                    set
                    {
                        this.pcFieldSpecified = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] Layout
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoLayout
            {

                private AltoPage[] pageField;

                private string sTYLEREFSField;

                /// <remarks/>
                [XmlElementAttribute("Page")]
                public AltoPage[] Pages
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
                [XmlAttributeAttribute("STYLEREFS", DataType = "IDREFS")]
                public string StyleRefs
                {
                    get
                    {
                        return this.sTYLEREFSField;
                    }
                    set
                    {
                        this.sTYLEREFSField = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] Tag
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoTag
            {

                private AltoTagXmlData xmlDataField;

                private string idField;

                private string tYPEField;

                private string lABELField;

                private string dESCRIPTIONField;

                private string uRIField;

                /// <summary>
                /// The xml data wrapper element XmlData is used to contain XML encoded metadata.
                /// The content of an XmlData element can be in any namespace or in no namespace.
                /// As permitted by the XML Schema Standard, the processContents attribute value for the
                /// metadata in an XmlData is set to “lax”. Therefore, if the source schema and its location are
                /// identified by means of an XML schemaLocation attribute, then an XML processor will validate
                /// the elements for which it can find declarations.If a source schema is not identified, or cannot be
                /// found at the specified schemaLocation, then an XML validator will check for well-formedness,
                /// but otherwise skip over the elements appearing in the XmlData element.
                /// </summary>
                public AltoTagXmlData XmlData
                {
                    get
                    {
                        return this.xmlDataField;
                    }
                    set
                    {
                        this.xmlDataField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("ID", DataType = "ID")]
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
                /// Type can be used to classify and group the information within each tag element type.
                /// </summary>
                [XmlAttributeAttribute("TYPE")]
                public string Type
                {
                    get
                    {
                        return this.tYPEField;
                    }
                    set
                    {
                        this.tYPEField = value;
                    }
                }

                /// <summary>
                /// Content / information value of the tag.
                /// </summary>
                [XmlAttributeAttribute("LABEL")]
                public string Label
                {
                    get
                    {
                        return this.lABELField;
                    }
                    set
                    {
                        this.lABELField = value;
                    }
                }

                /// <summary>
                /// Description text for tag information for clarification.
                /// </summary>
                [XmlAttributeAttribute("DESCRIPTION")]
                public string Description
                {
                    get
                    {
                        return this.dESCRIPTIONField;
                    }
                    set
                    {
                        this.dESCRIPTIONField = value;
                    }
                }

                /// <summary>
                /// Any URI for authority or description relevant information.
                /// </summary>
                [XmlAttributeAttribute("URI", DataType = "anyURI")]
                public string Uri
                {
                    get
                    {
                        return this.uRIField;
                    }
                    set
                    {
                        this.uRIField = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] The xml data wrapper element XmlData is used to contain XML encoded metadata.
            /// The content of an XmlData element can be in any namespace or in no namespace.
            /// As permitted by the XML Schema Standard, the processContents attribute value for the
            /// metadata in an XmlData is set to “lax”. Therefore, if the source schema and its location are
            /// identified by means of an XML schemaLocation attribute, then an XML processor will validate
            /// the elements for which it can find declarations. If a source schema is not identified, or cannot be
            /// found at the specified schemaLocation, then an XML validator will check for well-formedness,
            /// but otherwise skip over the elements appearing in the XmlData element.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoTagXmlData
            {

                private XmlElement[] anyField;

                /// <remarks/>
                [XmlAnyElementAttribute()]
                public XmlElement[] Any
                {
                    get
                    {
                        return this.anyField;
                    }
                    set
                    {
                        this.anyField = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] There are following variation of tag types available:
            /// LayoutTag – criteria about arrangement or graphical appearance; 
            /// StructureTag – criteria about grouping or formation; 
            /// RoleTag – criteria about function or mission; 
            /// NamedEntityTag – criteria about assignment of terms to their relationship / meaning (NER); 
            /// OtherTag – criteria about any other characteristic not listed above, the TYPE attribute is intended to be used for classification within those.; 
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoTags
            {

                private AltoTag[] itemsField;

                private AltoItemsChoice[] itemsElementNameField;

                /// <remarks/>
                [XmlElementAttribute("LayoutTag", typeof(AltoTag))]
                [XmlElementAttribute("NamedEntityTag", typeof(AltoTag))]
                [XmlElementAttribute("OtherTag", typeof(AltoTag))]
                [XmlElementAttribute("RoleTag", typeof(AltoTag))]
                [XmlElementAttribute("StructureTag", typeof(AltoTag))]
                [XmlChoiceIdentifierAttribute("ItemsElementName")]
                public AltoTag[] Items
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
                [XmlElementAttribute("ItemsElementName")]
                [XmlIgnoreAttribute()]
                public AltoItemsChoice[] ItemsElementName
                {
                    get
                    {
                        return this.itemsElementNameField;
                    }
                    set
                    {
                        this.itemsElementNameField = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] A paragraph style defines formatting properties of text blocks.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoParagraphStyle
            {

                private string idField;

                private AltoParagraphStyleAlign aLIGNField;

                private bool aLIGNFieldSpecified;

                private float lEFTField;

                private bool lEFTFieldSpecified;

                private float rIGHTField;

                private bool rIGHTFieldSpecified;

                private float lINESPACEField;

                private bool lINESPACEFieldSpecified;

                private float fIRSTLINEField;

                private bool fIRSTLINEFieldSpecified;

                /// <remarks/>
                [XmlAttributeAttribute("ID", DataType = "ID")]
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
                /// Indicates the alignement of the paragraph. Could be left, right, center or justify.
                /// </summary>
                [XmlAttributeAttribute("ALIGN")]
                public AltoParagraphStyleAlign Align
                {
                    get
                    {
                        return this.aLIGNField;
                    }
                    set
                    {
                        this.aLIGNField = value;
                        this.aLIGNFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool AlignSpecified
                {
                    get
                    {
                        return this.aLIGNFieldSpecified;
                    }
                    set
                    {
                        this.aLIGNFieldSpecified = value;
                    }
                }

                /// <summary>
                /// Left indent of the paragraph in relation to the column.
                /// </summary>
                [XmlAttributeAttribute("LEFT")]
                public float Left
                {
                    get
                    {
                        return this.lEFTField;
                    }
                    set
                    {
                        this.lEFTField = value;
                        if (!float.IsNaN(value)) this.lEFTFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool LeftSpecified
                {
                    get
                    {
                        return this.lEFTFieldSpecified;
                    }
                    set
                    {
                        this.lEFTFieldSpecified = value;
                    }
                }

                /// <summary>
                /// Right indent of the paragraph in relation to the column.
                /// </summary>
                [XmlAttributeAttribute("RIGHT")]
                public float Right
                {
                    get
                    {
                        return this.rIGHTField;
                    }
                    set
                    {
                        this.rIGHTField = value;
                        if (!float.IsNaN(value)) this.rIGHTFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool RightSpecified
                {
                    get
                    {
                        return this.rIGHTFieldSpecified;
                    }
                    set
                    {
                        this.rIGHTFieldSpecified = value;
                    }
                }

                /// <summary>
                /// Line spacing between two lines of the paragraph. Measurement calculated from baseline to baseline.
                /// </summary>
                [XmlAttributeAttribute("LINESPACE")]
                public float LineSpace
                {
                    get
                    {
                        return this.lINESPACEField;
                    }
                    set
                    {
                        this.lINESPACEField = value;
                        if (!float.IsNaN(value)) this.lINESPACEFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool LineSpaceSpecified
                {
                    get
                    {
                        return this.lINESPACEFieldSpecified;
                    }
                    set
                    {
                        this.lINESPACEFieldSpecified = value;
                    }
                }

                /// <summary>
                /// Indent of the first line of the paragraph if this is different from the other lines. A negative 
                /// value indicates an indent to the left, a positive value indicates an indent to the right.
                /// </summary>
                [XmlAttributeAttribute("FIRSTLINE")]
                public float FirstLine
                {
                    get
                    {
                        return this.fIRSTLINEField;
                    }
                    set
                    {
                        this.fIRSTLINEField = value;
                        if (!float.IsNaN(value)) this.fIRSTLINEFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool FIRSTLINESpecified
                {
                    get
                    {
                        return this.fIRSTLINEFieldSpecified;
                    }
                    set
                    {
                        this.fIRSTLINEFieldSpecified = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] A text style defines font properties of text.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoTextStyle
            {

                private string idField;

                private string fONTFAMILYField;

                private AltoFontType fONTTYPEField;

                private bool fONTTYPEFieldSpecified;

                private AltoFontWidth fONTWIDTHField;

                private bool fONTWIDTHFieldSpecified;

                private float fONTSIZEField;

                private byte[] fONTCOLORField;

                private AltoFontStyles fONTSTYLEField;

                private bool fONTSTYLEFieldSpecified;

                /// <remarks/>
                [XmlAttributeAttribute("ID", DataType = "ID")]
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
                /// The font name.
                /// </summary>
                [XmlAttributeAttribute("FONTFAMILY")]
                public string FontFamily
                {
                    get
                    {
                        return this.fONTFAMILYField;
                    }
                    set
                    {
                        this.fONTFAMILYField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("FONTTYPE")]
                public AltoFontType FontType
                {
                    get
                    {
                        return this.fONTTYPEField;
                    }
                    set
                    {
                        this.fONTTYPEField = value;
                        this.fONTTYPEFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool FontTypeSpecified
                {
                    get
                    {
                        return this.fONTTYPEFieldSpecified;
                    }
                    set
                    {
                        this.fONTTYPEFieldSpecified = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("FONTWIDTH")]
                public AltoFontWidth FontWidth
                {
                    get
                    {
                        return this.fONTWIDTHField;
                    }
                    set
                    {
                        this.fONTWIDTHField = value;
                        this.fONTWIDTHFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool FontWidthSpecified
                {
                    get
                    {
                        return this.fONTWIDTHFieldSpecified;
                    }
                    set
                    {
                        this.fONTWIDTHFieldSpecified = value;
                    }
                }

                /// <summary>
                /// The font size, in points (1/72 of an inch).
                /// </summary>
                [XmlAttributeAttribute("FONTSIZE")]
                public float FontSize
                {
                    get
                    {
                        return this.fONTSIZEField;
                    }
                    set
                    {
                        this.fONTSIZEField = value;
                    }
                }

                /// <summary>
                /// Font color as RGB value
                /// </summary>
                [XmlAttributeAttribute("FONTCOLOR", DataType = "hexBinary")]
                public byte[] FontColor
                {
                    get
                    {
                        return this.fONTCOLORField;
                    }
                    set
                    {
                        this.fONTCOLORField = value;
                    }
                }

                /// <remarks/>
                [XmlAttributeAttribute("FONTSTYLE")]
                public AltoFontStyles FontStyle
                {
                    get
                    {
                        return this.fONTSTYLEField;
                    }
                    set
                    {
                        this.fONTSTYLEField = value;
                        this.fONTSTYLEFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool FontStyleSpecified
                {
                    get
                    {
                        return this.fONTSTYLEFieldSpecified;
                    }
                    set
                    {
                        this.fONTSTYLEFieldSpecified = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] Styles
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoStyles
            {

                private AltoTextStyle[] textStyleField;

                private AltoParagraphStyle[] paragraphStyleField;

                /// <remarks/>
                [XmlElementAttribute("TextStyle")]
                public AltoTextStyle[] TextStyle
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
                [XmlElementAttribute("ParagraphStyle")]
                public AltoParagraphStyle[] ParagraphStyle
                {
                    get
                    {
                        return this.paragraphStyleField;
                    }
                    set
                    {
                        this.paragraphStyleField = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] Information about a software application. Where applicable, the preferred method
            /// for determining this information is by selecting Help -- About.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoProcessingSoftware
            {

                private string softwareCreatorField;

                private string softwareNameField;

                private string softwareVersionField;

                private string applicationDescriptionField;

                /// <summary>
                /// The name of the organization or company that created the application.
                /// </summary>
                [XmlAttributeAttribute("softwareCreator")]
                public string SoftwareCreator
                {
                    get
                    {
                        return this.softwareCreatorField;
                    }
                    set
                    {
                        this.softwareCreatorField = value;
                    }
                }

                /// <summary>
                /// The name of the application.
                /// </summary>
                [XmlAttributeAttribute("softwareName")]
                public string SoftwareName
                {
                    get
                    {
                        return this.softwareNameField;
                    }
                    set
                    {
                        this.softwareNameField = value;
                    }
                }

                /// <summary>
                /// The version of the application.
                /// </summary>
                [XmlAttributeAttribute("softwareVersion")]
                public string SoftwareVersion
                {
                    get
                    {
                        return this.softwareVersionField;
                    }
                    set
                    {
                        this.softwareVersionField = value;
                    }
                }

                /// <summary>
                /// A description of any important characteristics of the application, especially for
                /// non-commercial applications. For example, if a non-commercial application is built
                /// using commercial components, e.g., an OCR engine SDK. Those components should be mentioned here.
                /// </summary>
                [XmlAttributeAttribute("applicationDescription")]
                public string ApplicationDescription
                {
                    get
                    {
                        return this.applicationDescriptionField;
                    }
                    set
                    {
                        this.applicationDescriptionField = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] Description of the processing step.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoProcessingStep
            {

                private AltoProcessingCategory processingCategoryField;

                private bool processingCategoryFieldSpecified;

                private string processingDateTimeField;

                private string processingAgencyField;

                private string[] processingStepDescriptionField;

                private string processingStepSettingsField;

                private AltoProcessingSoftware processingSoftwareField;

                /// <summary>
                /// Classification of the category of operation, how the file was created, including 
                /// generation, modification, preprocessing, postprocessing or any other steps.
                /// </summary>
                [XmlAttributeAttribute("processingCategory")]
                public AltoProcessingCategory ProcessingCategory
                {
                    get
                    {
                        return this.processingCategoryField;
                    }
                    set
                    {
                        this.processingCategoryField = value;
                        this.processingCategoryFieldSpecified = true;
                    }
                }

                /// <remarks/>
                [XmlIgnoreAttribute()]
                public bool ProcessingCategorySpecified
                {
                    get
                    {
                        return this.processingCategoryFieldSpecified;
                    }
                    set
                    {
                        this.processingCategoryFieldSpecified = value;
                    }
                }

                /// <summary>
                /// Date or DateTime the image was processed.
                /// </summary>
                [XmlAttributeAttribute("processingDateTime")]
                public string ProcessingDateTime
                {
                    get
                    {
                        return this.processingDateTimeField;
                    }
                    set
                    {
                        this.processingDateTimeField = value;
                    }
                }

                /// <summary>
                /// Identifies the organization level producer(s) of the processed image.
                /// </summary>
                [XmlAttributeAttribute("processingAgency")]
                public string ProcessingAgency
                {
                    get
                    {
                        return this.processingAgencyField;
                    }
                    set
                    {
                        this.processingAgencyField = value;
                    }
                }

                /// <summary>
                /// An ordinal listing of the image processing steps performed. For example, "image despeckling."
                /// </summary>
                [XmlElementAttribute("processingStepDescription")]
                public string[] ProcessingStepDescription
                {
                    get
                    {
                        return this.processingStepDescriptionField;
                    }
                    set
                    {
                        this.processingStepDescriptionField = value;
                    }
                }

                /// <summary>
                /// A description of any setting of the processing application. For example, for a multi-engine
                /// OCR application this might include the engines which were used. Ideally, this description 
                /// should be adequate so that someone else using the same application can produce identical results.
                /// </summary>
                [XmlAttributeAttribute("processingStepSettings")]
                public string ProcessingStepSettings
                {
                    get
                    {
                        return this.processingStepSettingsField;
                    }
                    set
                    {
                        this.processingStepSettingsField = value;
                    }
                }

                /// <remarks/>
                [XmlElementAttribute("processingSoftware")]
                public AltoProcessingSoftware ProcessingSoftware
                {
                    get
                    {
                        return this.processingSoftwareField;
                    }
                    set
                    {
                        this.processingSoftwareField = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] Ocr Processing
            /// <para>Element deprecated. 'AltoProcessing' should be used instead.</para>
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            //[Obsolete("Element deprecated. 'AltoProcessing' should be used instead.")]
            public class AltoOcrProcessing
            {

                private AltoProcessingStep[] preProcessingStepField;

                private AltoProcessingStep ocrProcessingStepField;

                private AltoProcessingStep[] postProcessingStepField;

                /// <remarks/>
                [XmlElementAttribute("preProcessingStep")]
                public AltoProcessingStep[] preProcessingStep
                {
                    get
                    {
                        return this.preProcessingStepField;
                    }
                    set
                    {
                        this.preProcessingStepField = value;
                    }
                }

                /// <remarks/>
                public AltoProcessingStep ocrProcessingStep
                {
                    get
                    {
                        return this.ocrProcessingStepField;
                    }
                    set
                    {
                        this.ocrProcessingStepField = value;
                    }
                }

                /// <remarks/>
                [XmlElementAttribute("postProcessingStep")]
                public AltoProcessingStep[] postProcessingStep
                {
                    get
                    {
                        return this.postProcessingStepField;
                    }
                    set
                    {
                        this.postProcessingStepField = value;
                    }
                }
            }

            /// <summary>
            /// [Alto] A unique identifier for the document.
            /// <para>This identifier must be unique within the local  
            /// To facilitate file sharing or interoperability with other systems, 
            /// documentIdentifierLocation may be added to designate the system or 
            /// application where the identifier is unique.</para>
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoDocumentIdentifier
            {

                private string documentIdentifierLocationField;

                private string valueField;

                /// <summary>
                /// A location qualifier, i.e., a namespace.
                /// </summary>
                [XmlAttributeAttribute("documentIdentifierLocation")]
                public string DocumentIdentifierLocation
                {
                    get
                    {
                        return this.documentIdentifierLocationField;
                    }
                    set
                    {
                        this.documentIdentifierLocationField = value;
                    }
                }

                /// <remarks/>
                [XmlTextAttribute()]
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
            /// [Alto] Description Ocr Processing
            /// <para>Element deprecated. 'AltoProcessing' should be used instead.</para>
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            //[Obsolete("Element deprecated. 'AltoProcessing' should be used instead.")]
            public class AltoDescriptionOcrProcessing : AltoOcrProcessing
            {

                private string idField;

                /// <remarks/>
                [XmlAttributeAttribute(DataType = "ID")]
                public string ID
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
            }

            /// <summary>
            /// [Alto] Description Processing
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [DebuggerStepThroughAttribute()]
            [DesignerCategoryAttribute("code")]
            [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public class AltoDescriptionProcessing : AltoProcessingStep
            {

                private string idField;

                /// <remarks/>
                [XmlAttributeAttribute("ID", DataType = "ID")]
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
            }

            /// <summary>
            /// [Alto] All measurement values inside the alto file are related to this unit, except the font size.
            /// 
            /// Coordinates as being used in HPOS and VPOS are absolute coordinates referring to the upper-left corner of a page.
            /// The upper left corner of the page is defined as coordinate (0/0). 
            /// 
            /// <para>values meaning:
            /// mm10: 1/10th of millimeter; 
            /// inch1200: 1/1200th of inch; 
            /// pixel: 1 pixel</para>
            /// 
            /// The values for pixel will be related to the resolution of the image based
            /// on which the layout is described. Incase the original image is not known
            /// the scaling factor can be calculated based on total width and height of
            /// the image and the according information of the PAGE element.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public enum AltoMeasurementUnit
            {

                /// <summary>
                /// 1 pixel
                /// </summary>
                [XmlEnumAttribute("pixel")]
                Pixel,

                /// <summary>
                /// 1/10th of millimeter
                /// </summary>
                [XmlEnumAttribute("mm10")]
                Mm10,

                /// <summary>
                /// 1/1200th of inch
                /// </summary>
                [XmlEnumAttribute("inch1200")]
                Inch1200,
            }

            /// <summary>
            /// [Alto] List of any combination of font styles
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [FlagsAttribute()]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public enum AltoFontStyles
            {
                /// <remarks/>
                [XmlEnumAttribute("bold")]
                Bold = 1,

                /// <remarks/>
                [XmlEnumAttribute("italics")]
                Italics = 2,

                /// <remarks/>
                [XmlEnumAttribute("subscript")]
                Subscript = 4,

                /// <remarks/>
                [XmlEnumAttribute("superscript")]
                Superscript = 8,

                /// <remarks/>
                [XmlEnumAttribute("smallcaps")]
                SmallCaps = 16,

                /// <remarks/>
                [XmlEnumAttribute("underline")]
                Underline = 32,
            }

            /// <summary>
            /// [Alto] Type of the substitution (if any)
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public enum AltoSubsType
            {

                /// <remarks/>
                HypPart1,

                /// <remarks/>
                HypPart2,

                /// <remarks/>
                Abbreviation,
            }

            /// <summary>
            /// [Alto/xlink] Block Type Show
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.w3.org/1999/xlink")]
            public enum AltoBlockTypeShow
            {
                /// <remarks/>
                [XmlEnumAttribute("new")]
                New,

                /// <remarks/>
                [XmlEnumAttribute("replace")]
                Replace,

                /// <remarks/>
                [XmlEnumAttribute("embed")]
                Embed,

                /// <remarks/>
                [XmlEnumAttribute("other")]
                Other,

                /// <remarks/>
                [XmlEnumAttribute("none")]
                None,
            }

            /// <summary>
            /// [Alto/xlink] Block Type Actuate
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.w3.org/1999/xlink")]
            public enum AltoBlockTypeActuate
            {
                /// <remarks/>
                [XmlEnumAttribute("onLoad")]
                OnLoad,

                /// <remarks/>
                [XmlEnumAttribute("onRequest")]
                OnRequest,

                /// <remarks/>
                [XmlEnumAttribute("other")]
                Other,

                /// <remarks/>
                [XmlEnumAttribute("none")]
                None,
            }

            /// <summary>
            /// [Alto] Gives brief information about original page quality
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public enum AltoQuality
            {

                /// <remarks/>
                OK,

                /// <remarks/>
                Missing,

                /// <remarks/>
                [XmlEnumAttribute("Missing in original")]
                MissingInOriginal,

                /// <remarks/>
                Damaged,

                /// <remarks/>
                Retained,

                /// <remarks/>
                Target,

                /// <remarks/>
                [XmlEnumAttribute("As in original")]
                AsInOriginal,
            }

            /// <summary>
            /// [Alto] Position of the page. Could be lefthanded, righthanded, cover, foldout or single if it has no special position.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public enum AltoPosition
            {

                /// <remarks/>
                Left,

                /// <remarks/>
                Right,

                /// <remarks/>
                Foldout,

                /// <remarks/>
                Single,

                /// <remarks/>
                Cover,
            }

            /// <summary>
            /// [Alto] There are following variation of tag types available:
            /// LayoutTag – criteria about arrangement or graphical appearance; 
            /// StructureTag – criteria about grouping or formation; 
            /// RoleTag – criteria about function or mission; 
            /// NamedEntityTag – criteria about assignment of terms to their relationship / meaning (NER); 
            /// OtherTag – criteria about any other characteristic not listed above, the TYPE attribute is intended to be used for classification within those.; 
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#", IncludeInSchema = false)]
            public enum AltoItemsChoice
            {

                /// <summary>
                /// Criteria about arrangement or graphical appearance
                /// </summary>
                LayoutTag,

                /// <summary>
                /// Criteria about assignment of terms to their relationship / meaning (NER)
                /// </summary>
                NamedEntityTag,

                /// <summary>
                /// Criteria about any other characteristic not listed above, the TYPE attribute is intended to be used for classification within those.
                /// </summary>
                OtherTag,

                /// <summary>
                /// Criteria about function or mission
                /// </summary>
                RoleTag,

                /// <summary>
                /// Criteria about grouping or formation
                /// </summary>
                StructureTag,
            }

            /// <summary>
            /// [Alto] Indicates the alignement of the paragraph. Could be left, right, center or justify.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public enum AltoParagraphStyleAlign
            {

                /// <remarks/>
                Left,

                /// <remarks/>
                Right,

                /// <remarks/>
                Center,

                /// <remarks/>
                Block,
            }

            /// <summary>
            /// [Alto] Serif or Sans-Serif
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public enum AltoFontType
            {

                /// <summary>
                /// 
                /// </summary>
                [XmlEnumAttribute("serif")]
                Serif,

                /// <summary>
                /// 
                /// </summary>
                [XmlEnumAttribute("sans-serif")]
                SansSerif,
            }

            /// <summary>
            /// [Alto] Fixed or proportional
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public enum AltoFontWidth
            {
                /// <remarks/>
                [XmlEnumAttribute("proportional")]
                Proportional,

                /// <remarks/>
                [XmlEnumAttribute("fixed")]
                Fixed,
            }

            /// <summary>
            /// [Alto] Classification of the category of operation, how the file was created, including generation, modification, 
            /// preprocessing, postprocessing or any other steps.
            /// </summary>
            [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [FlagsAttribute()]
            [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
            [SerializableAttribute()]
            [XmlTypeAttribute(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
            public enum AltoProcessingCategory
            {
                /// <remarks/>
                [XmlEnumAttribute("contentGeneration")]
                ContentGeneration = 1,

                /// <remarks/>
                [XmlEnumAttribute("contentModification")]
                ContentModification = 2,

                /// <remarks/>
                [XmlEnumAttribute("preOperation")]
                PreOperation = 4,

                /// <remarks/>
                [XmlEnumAttribute("postOperation")]
                PostOperation = 8,

                /// <remarks/>
                [XmlEnumAttribute("other")]
                Other = 16,
            }
        }
        #endregion
    }
}
