namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export
{
    using Content;
    using Core;
    using DocumentLayoutAnalysis;
    using Graphics;
    using Graphics.Colors;
    using PAGE;
    using PageSegmenter;
    using ReadingOrderDetector;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using System.Xml.Serialization;
    using Util;

    /// <summary>
    /// PAGE-XML 2019-07-15 (XML) text exporter.
    /// <para>See https://github.com/PRImA-Research-Lab/PAGE-XML </para>
    /// </summary>
    public sealed class PageXmlTextExporter : ITextExporter
    {
        private readonly IPageSegmenter pageSegmenter;
        private readonly IWordExtractor wordExtractor;
        private readonly IReadingOrderDetector readingOrderDetector;
        private readonly Func<string, string> invalidCharacterHandler;
        private readonly double scale;
        private readonly string indentChar;

        /// <inheritdoc/>
        public InvalidCharStrategy InvalidCharStrategy { get; }

        /// <summary>
        /// PAGE-XML 2019-07-15 (XML) text exporter.
        /// <para>See https://github.com/PRImA-Research-Lab/PAGE-XML </para>
        /// </summary>
        /// <param name="wordExtractor"></param>
        /// <param name="pageSegmenter"></param>
        /// <param name="readingOrderDetector"></param>
        /// <param name="scale"></param>
        /// <param name="indentChar">Indent character.</param>
        /// <param name="invalidCharacterHandler">How to handle invalid characters.</param>
        public PageXmlTextExporter(IWordExtractor wordExtractor, IPageSegmenter pageSegmenter,
                                   IReadingOrderDetector readingOrderDetector,
                                   double scale, string indentChar,
                                   Func<string, string> invalidCharacterHandler)
            : this(wordExtractor, pageSegmenter, readingOrderDetector, scale, indentChar,
                  InvalidCharStrategy.Custom, invalidCharacterHandler)
        { }

        /// <summary>
        /// PAGE-XML 2019-07-15 (XML) text exporter.
        /// <para>See https://github.com/PRImA-Research-Lab/PAGE-XML </para>
        /// </summary>
        /// <param name="wordExtractor"></param>
        /// <param name="pageSegmenter"></param>
        /// <param name="readingOrderDetector"></param>
        /// <param name="scale"></param>
        /// <param name="indent">Indent character.</param>
        /// <param name="invalidCharacterStrategy">How to handle invalid characters.</param>
        public PageXmlTextExporter(IWordExtractor wordExtractor, IPageSegmenter pageSegmenter,
                                   IReadingOrderDetector readingOrderDetector = null,
                                   double scale = 1.0, string indent = "\t",
                                   InvalidCharStrategy invalidCharacterStrategy = InvalidCharStrategy.DoNotCheck)
            : this(wordExtractor, pageSegmenter, readingOrderDetector, scale, indent,
                  invalidCharacterStrategy, null)
        { }

        private PageXmlTextExporter(IWordExtractor wordExtractor, IPageSegmenter pageSegmenter,
                           IReadingOrderDetector readingOrderDetector,
                           double scale, string indentChar,
                           InvalidCharStrategy invalidCharacterStrategy,
                           Func<string, string> invalidCharacterHandler)
        {
            this.wordExtractor = wordExtractor;
            this.pageSegmenter = pageSegmenter;
            this.readingOrderDetector = readingOrderDetector;
            this.scale = scale;
            this.indentChar = indentChar ?? string.Empty;
            InvalidCharStrategy = invalidCharacterStrategy;

            if (invalidCharacterHandler is null)
            {
                this.invalidCharacterHandler = TextExporterHelper.GetXmlInvalidCharHandler(InvalidCharStrategy);
            }
            else
            {
                this.invalidCharacterHandler = invalidCharacterHandler;
            }
        }

        /// <summary>
        /// Get the PAGE-XML (XML) string of the pages layout.
        /// <para>Not implemented, use <see cref="Get(Page)"/> instead.</para>
        /// </summary>
        /// <param name="document"></param>
        /// <param name="includePaths">Draw PdfPaths present in the page.</param>
        public string Get(PdfDocument document, bool includePaths = false)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the PAGE-XML (XML) string of the pages layout. Excludes PdfPaths.
        /// </summary>
        /// <param name="page"></param>
        public string Get(Page page)
        {
            return Get(page, false);
        }

        /// <summary>
        /// Get the PAGE-XML (XML) string of the pages layout.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="includePaths">Draw PdfPaths present in the page.</param>
        public string Get(Page page, bool includePaths)
        {
            PageXmlData data = new PageXmlData();

            DateTime utcNow = DateTime.UtcNow;

            PageXmlDocument pageXmlDocument = new PageXmlDocument()
            {
                Metadata = new PageXmlDocument.PageXmlMetadata()
                {
                    Created = utcNow,
                    LastChange = utcNow,
                    Creator = "PdfPig",
                    Comments = pageSegmenter.GetType().Name + "|" + wordExtractor.GetType().Name,
                },
                PcGtsId = "pc-" + page.GetHashCode()
            };

            pageXmlDocument.Page = ToPageXmlPage(page, data, includePaths);

            return Serialize(pageXmlDocument);
        }

        /// <summary>
        /// Converts a point to a string
        /// </summary>
        /// <param name="point"></param>
        /// <param name="pageWidth">The width of the page where the pdf point is located on</param>
        /// <param name="pageHeight">The height of the page where the pdf point is located on</param>
        /// <param name="scaleToApply"></param>
        /// <returns></returns>
        public static string PointToString(PdfPoint point, double pageWidth, double pageHeight, double scaleToApply = 1.0)
        {
            double x = Math.Round(point.X * scaleToApply);
            double y = Math.Round((pageHeight - point.Y) * scaleToApply);

            // move away from borders
            x = x > 1 ? x : 1;
            y = y > 1 ? y : 1;

            x = x < (pageWidth - 1) ? x : pageWidth - 1;
            y = y < (pageHeight - 1) ? y : pageHeight - 1;

            return x.ToString("0") + "," + y.ToString("0");
        }

        private string ToPoints(IEnumerable<PdfPoint> points, double pageWidth, double pageHeight)
        {
            return string.Join(" ", points.Select(p => PointToString(p, pageWidth, pageHeight, scale)));
        }

        private string ToPoints(PdfRectangle pdfRectangle, double pageWidth, double pageHeight)
        {
            return ToPoints(new[] { pdfRectangle.BottomLeft, pdfRectangle.TopLeft, pdfRectangle.TopRight, pdfRectangle.BottomRight }, pageWidth, pageHeight);
        }

        private PageXmlDocument.PageXmlCoords ToCoords(PdfRectangle pdfRectangle, double pageWidth, double pageHeight)
        {
            return new PageXmlDocument.PageXmlCoords()
            {
                Points = ToPoints(pdfRectangle, pageWidth, pageHeight)
            };
        }

        /// <summary>
        /// PageXml Text colour in RGB encoded format
        /// <para>(red value) + (256 x green value) + (65536 x blue value).</para>
        /// </summary>
        private string ToRgbEncoded(IColor color)
        {
            var (r, g, b) = color.ToRGBValues();
            int red = Convert.ToByte(255.0 * r);
            int green = 256 * Convert.ToByte(255.0 * g);
            int blue = 65536 * Convert.ToByte(255.0 * b);
            int sum = red + green + blue;

            // as per below, red and blue order might be inverted... var colorWin = System.Drawing.Color.FromArgb(sum);
            return sum.ToString();
        }

        private PageXmlDocument.PageXmlPage ToPageXmlPage(Page page, PageXmlData data, bool includePaths)
        {
            var pageXmlPage = new PageXmlDocument.PageXmlPage
            {
                ImageFilename = "unknown",
                ImageHeight = (int)Math.Round(page.Height * scale),
                ImageWidth = (int)Math.Round(page.Width * scale),
            };

            var regions = new List<PageXmlDocument.PageXmlRegion>();

            var words = page.GetWords(wordExtractor).ToList();
            if (words.Count > 0)
            {
                var blocks = pageSegmenter.GetBlocks(words);

                if (readingOrderDetector != null)
                {
                    blocks = readingOrderDetector.Get(blocks).ToList();
                }

                regions.AddRange(blocks.Select(b => ToPageXmlTextRegion(b, data, page.Width, page.Height)));

                if (data.OrderedRegions.Count > 0)
                {
                    data.GroupOrdersCount++;
                    pageXmlPage.ReadingOrder = new PageXmlDocument.PageXmlReadingOrder()
                    {
                        Item = new PageXmlDocument.PageXmlOrderedGroup()
                        {
                            Items = data.OrderedRegions.ToArray(),
                            Id = "g" + data.GroupOrdersCount
                        }
                    };
                }
            }

            var images = page.GetImages().ToList();
            if (images.Count > 0)
            {
                regions.AddRange(images.Select(i => ToPageXmlImageRegion(i, data, page.Width, page.Height)));
            }

            if (includePaths)
            {
                foreach (var path in page.Paths)
                {
                    var graphicalElement = ToPageXmlLineDrawingRegion(path, data, page.Width, page.Height);

                    if (graphicalElement != null)
                    {
                        regions.Add(graphicalElement);
                    }
                }
            }

            pageXmlPage.Items = regions.ToArray();
            return pageXmlPage;
        }

        private PageXmlDocument.PageXmlLineDrawingRegion ToPageXmlLineDrawingRegion(PdfPath pdfPath, PageXmlData data, double pageWidth, double pageHeight)
        {
            var bbox = pdfPath.GetBoundingRectangle();
            if (bbox.HasValue)
            {
                data.RegionsCount++;
                return new PageXmlDocument.PageXmlLineDrawingRegion()
                {
                    Coords = ToCoords(bbox.Value, pageWidth, pageHeight),
                    Id = "r" + data.RegionsCount
                };
            }
            return null;
        }

        private PageXmlDocument.PageXmlImageRegion ToPageXmlImageRegion(IPdfImage pdfImage, PageXmlData data, double pageWidth, double pageHeight)
        {
            data.RegionsCount++;
            var bbox = pdfImage.Bounds;
            return new PageXmlDocument.PageXmlImageRegion()
            {
                Coords = ToCoords(bbox, pageWidth, pageHeight),
                Id = "r" + data.RegionsCount
            };
        }

        private PageXmlDocument.PageXmlTextRegion ToPageXmlTextRegion(TextBlock textBlock, PageXmlData data, double pageWidth, double pageHeight)
        {
            data.RegionsCount++;
            string regionId = "r" + data.RegionsCount;

            if (readingOrderDetector != null && textBlock.ReadingOrder > -1)
            {
                data.OrderedRegions.Add(new PageXmlDocument.PageXmlRegionRefIndexed()
                {
                    RegionRef = regionId,
                    Index = textBlock.ReadingOrder
                });
            }

            return new PageXmlDocument.PageXmlTextRegion()
            {
                Coords = ToCoords(textBlock.BoundingBox, pageWidth, pageHeight),
                Type = PageXmlDocument.PageXmlTextSimpleType.Paragraph,
                TextLines = textBlock.TextLines.Select(l => ToPageXmlTextLine(l, data, pageWidth, pageHeight)).ToArray(),
                TextEquivs = new[]
                {
                    new PageXmlDocument.PageXmlTextEquiv()
                    {
                        Unicode = invalidCharacterHandler(textBlock.Text)
                    }
                },
                Id = regionId
            };
        }

        private PageXmlDocument.PageXmlTextLine ToPageXmlTextLine(TextLine textLine, PageXmlData data, double pageWidth, double pageHeight)
        {
            data.LinesCount++;
            return new PageXmlDocument.PageXmlTextLine()
            {
                Coords = ToCoords(textLine.BoundingBox, pageWidth, pageHeight),
                Production = PageXmlDocument.PageXmlProductionSimpleType.Printed,
                Words = textLine.Words.Select(w => ToPageXmlWord(w, data, pageWidth, pageHeight)).ToArray(),
                TextEquivs = new[]
                {
                    new PageXmlDocument.PageXmlTextEquiv()
                    {
                        Unicode = invalidCharacterHandler(textLine.Text)
                    }
                },
                Id = "l" + data.LinesCount
            };
        }

        private PageXmlDocument.PageXmlWord ToPageXmlWord(Word word, PageXmlData data, double pageWidth, double pageHeight)
        {
            data.WordsCount++;
            return new PageXmlDocument.PageXmlWord()
            {
                Coords = ToCoords(word.BoundingBox, pageWidth, pageHeight),
                Glyphs = word.Letters.Select(l => ToPageXmlGlyph(l, data, pageWidth, pageHeight)).ToArray(),
                TextEquivs = new[]
                {
                    new PageXmlDocument.PageXmlTextEquiv()
                    {
                        Unicode = invalidCharacterHandler(word.Text)
                    }
                },
                Id = "w" + data.WordsCount
            };
        }

        private PageXmlDocument.PageXmlGlyph ToPageXmlGlyph(Letter letter, PageXmlData data, double pageWidth, double pageHeight)
        {
            data.GlyphsCount++;
            return new PageXmlDocument.PageXmlGlyph()
            {
                Coords = ToCoords(letter.GlyphRectangle, pageWidth, pageHeight),
                Ligature = false,
                Production = PageXmlDocument.PageXmlProductionSimpleType.Printed,
                TextStyle = new PageXmlDocument.PageXmlTextStyle()
                {
                    FontSize = (float)letter.FontSize,
                    FontFamily = letter.FontName,
                    TextColourRgb = ToRgbEncoded(letter.Color),
                },
                TextEquivs = new[]
                {
                    new PageXmlDocument.PageXmlTextEquiv()
                    {
                        Unicode = invalidCharacterHandler(letter.Value)
                    }
                },
                Id = "c" + data.GlyphsCount
            };
        }

        private string Serialize(PageXmlDocument pageXmlDocument)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PageXmlDocument));
            var settings = new XmlWriterSettings()
            {
                Encoding = System.Text.Encoding.UTF8,
                Indent = true,
                IndentChars = indentChar,
                CheckCharacters = InvalidCharStrategy != InvalidCharStrategy.DoNotCheck,
            };

            using (var memoryStream = new System.IO.MemoryStream())
            using (var xmlWriter = XmlWriter.Create(memoryStream, settings))
            {
                serializer.Serialize(xmlWriter, pageXmlDocument);
                return System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }

        /// <summary>
        /// Deserialize an <see cref="PageXmlDocument"/> from a given PAGE format XML document.
        /// </summary>
        public static PageXmlDocument Deserialize(string xmlPath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PageXmlDocument));

            var settings = new XmlReaderSettings()
            {
                CheckCharacters = false
            };

            using (var reader = XmlReader.Create(xmlPath, settings))
            {
                return (PageXmlDocument)serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// Class to keep track of a page data.
        /// </summary>
        private sealed class PageXmlData
        {
            public PageXmlData()
            {
                OrderedRegions = new List<PageXmlDocument.PageXmlRegionRefIndexed>();
            }

            public int LinesCount { get; set; }
            public int WordsCount { get; set; }
            public int GlyphsCount { get; set; }
            public int RegionsCount { get; set; }
            public int GroupOrdersCount { get; set; }

            public List<PageXmlDocument.PageXmlRegionRefIndexed> OrderedRegions { get; }
        }
    }
}
