namespace UglyToad.PdfPig.Export
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Xml;
    using System.Xml.Serialization;
    using Alto;
    using Content;
    using DocumentLayoutAnalysis;
    using Geometry;
    using Util;

    /// <inheritdoc />
    /// <summary>
    /// Alto 4.1 (XML) text exporter.
    /// <para>See https://github.com/altoxml/schema </para>
    /// </summary>
    public class AltoXmlTextExporter : ITextExporter
    {
        private readonly IPageSegmenter pageSegmenter;
        private readonly IWordExtractor wordExtractor;

        private readonly double scale;
        private readonly string indentChar;

        private int pageCount;
        private int pageSpaceCount;
        private int graphicalElementCount;
        private int illustrationCount;
        private int textBlockCount;
        private int textLineCount;
        private int stringCount;
        private int glyphCount;

        /// <summary>
        /// Alto 4.1 (XML).
        /// <para>See https://github.com/altoxml/schema </para>
        /// </summary>
        /// <param name="wordExtractor">Extractor used to identify words in the document.</param>
        /// <param name="pageSegmenter">Segmenter used to split page into blocks.</param>
        /// <param name="scale">Scale multiplier to apply to output document, defaults to 1.</param>
        /// <param name="indent">Character to use for indentation, defaults to tab.</param>
        public AltoXmlTextExporter(IWordExtractor wordExtractor, IPageSegmenter pageSegmenter, double scale = 1, string indent = "\t")
        {
            this.wordExtractor = wordExtractor ?? throw new ArgumentNullException(nameof(wordExtractor));
            this.pageSegmenter = pageSegmenter ?? throw new ArgumentNullException(nameof(pageSegmenter));
            this.scale = scale;
            indentChar = indent ?? string.Empty;
        }

        /// <summary>
        /// Get the Alto (XML) string of the pages layout.
        /// </summary>
        /// <param name="document">The document to extract page layouts from.</param>
        /// <param name="includePaths">Draw <see cref="PdfPath"/>s present in the page.</param>
        public string Get(PdfDocument document, bool includePaths = false)
        {
            var altoDocument = CreateAltoDocument("unknown");
            var altoPages = document.GetPages().Select(x => ToAltoPage(x, includePaths)).ToArray();

            altoDocument.Layout.Pages = altoPages;

            return Serialize(altoDocument);
        }

        /// <inheritdoc />
        /// <summary>
        /// Get the Alto (XML) string of the page layout. Excludes <see cref="T:UglyToad.PdfPig.Geometry.PdfPath" />s.
        /// </summary>
        /// <param name="page">The page to export the XML layout for.</param>
        public string Get(Page page) => Get(page, false);

        /// <summary>
        /// Get the Alto (XML) string of the page layout.
        /// </summary>
        /// <param name="page">The page to export the XML layout for.</param>
        /// <param name="includePaths">Whether the output should include the <see cref="PdfPath"/>s present in the page.</param>
        public string Get(Page page, bool includePaths)
        {
            var document = CreateAltoDocument("unknown");

            document.Layout.Pages = new[] { ToAltoPage(page, includePaths) };

            return Serialize(document);
        }

        /// <summary>
        /// Create an empty <see cref="AltoDocument"/>.
        /// </summary>
        private AltoDocument CreateAltoDocument(string fileName)
        {
            return new AltoDocument
            {
                Layout = new AltoDocument.AltoLayout
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

            var altoPage = new AltoDocument.AltoPage
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
                PrintSpace = new AltoDocument.AltoPageSpace
                {
                    Height = (float)Math.Round(page.Height * scale),    // TBD
                    Width = (float)Math.Round(page.Width * scale),      // TBD
                    VerticalPosition = 0f,                                          // TBD
                    HorizontalPosition = 0f,                                          // TBD
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
            var blocks = pageSegmenter.GetBlocks(words).Select(b => ToAltoTextBlock(b, page.Height)).ToArray();

            altoPage.PrintSpace.TextBlock = blocks;

            altoPage.PrintSpace.Illustrations = page.GetImages().Select(i => ToAltoIllustration(i, page.Height)).ToArray();
            
            if (includePaths)
            {
                altoPage.PrintSpace.GraphicalElements = page.ExperimentalAccess.Paths
                    .Select(p => ToAltoGraphicalElement(p, page.Height))
                    .ToArray();
            }

            return altoPage;
        }

        private AltoDocument.AltoGraphicalElement ToAltoGraphicalElement(PdfPath pdfPath, double height)
        {
            graphicalElementCount++;

            var rectangle = pdfPath.GetBoundingRectangle();
            if (rectangle.HasValue)
            {
                return new AltoDocument.AltoGraphicalElement
                {
                    VerticalPosition = (float)Math.Round((height - rectangle.Value.Top) * scale),
                    HorizontalPosition = (float)Math.Round(rectangle.Value.Left * scale),
                    Height = (float)Math.Round(rectangle.Value.Height * scale),
                    Width = (float)Math.Round(rectangle.Value.Width * scale),
                    Rotation = 0,
                    StyleRefs = null,
                    TagRefs = null,
                    Title = null,
                    Type = null,
                    Id = "P" + pageCount + "_GE" + graphicalElementCount.ToString("#00000")
                };
            }
            return null;
        }

        private AltoDocument.AltoIllustration ToAltoIllustration(IPdfImage pdfImage, double height)
        {
            illustrationCount++;
            var rectangle = pdfImage.Bounds;

            return new AltoDocument.AltoIllustration
            {
                VerticalPosition = (float)Math.Round((height - rectangle.Top) * scale),
                HorizontalPosition = (float)Math.Round(rectangle.Left * scale),
                Height = (float)Math.Round(rectangle.Height * scale),
                Width = (float)Math.Round(rectangle.Width * scale),
                FileId = "",
                Rotation = 0,
                Id = "P" + pageCount + "_I" + illustrationCount.ToString("#00000")
            };
        }

        private AltoDocument.AltoTextBlock ToAltoTextBlock(TextBlock textBlock, double height)
        {
            textBlockCount++;

            return new AltoDocument.AltoTextBlock
            {
                VerticalPosition = (float)Math.Round((height - textBlock.BoundingBox.Top) * scale),
                HorizontalPosition = (float)Math.Round(textBlock.BoundingBox.Left * scale),
                Height = (float)Math.Round(textBlock.BoundingBox.Height * scale),
                Width = (float)Math.Round(textBlock.BoundingBox.Width * scale),
                Rotation = 0,
                TextLines = textBlock.TextLines.Select(l => ToAltoTextLine(l, height)).ToArray(),
                StyleRefs = null,
                TagRefs = null,
                Title = null,
                Type = null,
                Id = "P" + pageCount + "_TB" + textBlockCount.ToString("#00000")
            };
        }

        private AltoDocument.AltoTextBlockTextLine ToAltoTextLine(TextLine textLine, double height)
        {
            textLineCount++;
            var strings = textLine.Words
                .Where(x => x.Text.All(XmlConvert.IsXmlChar))
                .Select(w => ToAltoString(w, height)).ToArray();

            return new AltoDocument.AltoTextBlockTextLine
            {
                VerticalPosition = (float)Math.Round((height - textLine.BoundingBox.Top) * scale),
                HorizontalPosition = (float)Math.Round(textLine.BoundingBox.Left * scale),
                Height = (float)Math.Round(textLine.BoundingBox.Height * scale),
                Width = (float)Math.Round(textLine.BoundingBox.Width * scale),
                BaseLine = float.NaN,
                Strings = strings,
                Language = null,
                StyleRefs = null,
                TagRefs = null,
                Id = "P" + pageCount + "_TL" + textLineCount.ToString("#00000")
            };
        }
        private AltoDocument.AltoString ToAltoString(Word word, double height)
        {
            stringCount++;
            var glyphs = word.Letters.Select(l => ToAltoGlyph(l, height)).ToArray();

            return new AltoDocument.AltoString
            {
                VerticalPosition = (float)Math.Round((height - word.BoundingBox.Top) * scale),
                HorizontalPosition = (float)Math.Round(word.BoundingBox.Left * scale),
                Height = (float)Math.Round(word.BoundingBox.Height * scale),
                Width = (float)Math.Round(word.BoundingBox.Width * scale),
                Glyph = glyphs,
                Cc = string.Join("", glyphs.Select(g => 9f * (1f - g.Gc))), // from 0->1 to 9->0
                Content = word.Text,
                Language = null,
                StyleRefs = null,
                SubsContent = null,
                TagRefs = null,
                Wc = float.NaN,
                Id = "P" + pageCount + "_ST" + stringCount.ToString("#00000")
            };
        }

        private AltoDocument.AltoGlyph ToAltoGlyph(Letter letter, double height)
        {
            glyphCount++;
            return new AltoDocument.AltoGlyph
            {
                VerticalPosition = (float)Math.Round((height - letter.GlyphRectangle.Top) * scale),
                HorizontalPosition = (float)Math.Round(letter.GlyphRectangle.Left * scale),
                Height = (float)Math.Round(letter.GlyphRectangle.Height * scale),
                Width = (float)Math.Round(letter.GlyphRectangle.Width * scale),
                Gc = 1.0f,
                Content = letter.Value,
                Id = "P" + pageCount + "_ST" + stringCount.ToString("#00000") + "_G" + glyphCount.ToString("#00")
            };
        }

        private AltoDocument.AltoDescription GetAltoDescription(string fileName)
        {
            var processing = new AltoDocument.AltoDescriptionProcessing
            {
                ProcessingAgency = null,
                ProcessingCategory = AltoDocument.AltoProcessingCategory.Other,
                ProcessingDateTime = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture),
                ProcessingSoftware = new AltoDocument.AltoProcessingSoftware
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

            var documentIdentifier = new AltoDocument.AltoDocumentIdentifier
            {
                DocumentIdentifierLocation = null,
                Value = null
            };

            var fileIdentifier = new AltoDocument.AltoFileIdentifier
            {
                FileIdentifierLocation = null,
                Value = null
            };

            return new AltoDocument.AltoDescription
            {
                MeasurementUnit = AltoDocument.AltoMeasurementUnit.Pixel,
                Processings = new[] { processing },
                SourceImageInformation = new AltoDocument.AltoSourceImageInformation
                {
                    DocumentIdentifiers = new [] { documentIdentifier },
                    FileIdentifiers = new [] { fileIdentifier },
                    FileName = fileName
                }
            };
        }

        private string Serialize(AltoDocument altoDocument)
        {
            var serializer = new XmlSerializer(typeof(AltoDocument));
            var settings = new XmlWriterSettings
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

        /// <summary>
        /// Deserialize an <see cref="AltoDocument"/> from a given Alto format XML document.
        /// </summary>
        public static AltoDocument Deserialize(string xmlPath)
        {
            var serializer = new XmlSerializer(typeof(AltoDocument));

            using (var reader = XmlReader.Create(xmlPath))
            {
                return (AltoDocument)serializer.Deserialize(reader);
            }
        }
    }
}
