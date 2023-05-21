namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export
{
    using Content;
    using Core;
    using DocumentLayoutAnalysis;
    using Graphics.Colors;
    using PAGE;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using System.Xml.Serialization;
    using PageSegmenter;
    using ReadingOrderDetector;
    using Graphics;
    using Util;

    /// <summary>
    /// PAGE-XML 2019-07-15 (XML) text exporter.
    /// <para>See https://github.com/PRImA-Research-Lab/PAGE-XML </para>
    /// </summary>
    public class PageXmlTextExporter : ITextExporter
    {
        private readonly IPageSegmenter pageSegmenter;
        private readonly IWordExtractor wordExtractor;
        private readonly IReadingOrderDetector readingOrderDetector;

        private readonly double scale;
        private readonly string indentChar;

        private int lineCount;
        private int wordCount;
        private int glyphCount;
        private int regionCount;
        private int groupOrderCount;

        private List<PageXmlDocument.PageXmlRegionRefIndexed> orderedRegions;

        /// <summary>
        /// PAGE-XML 2019-07-15 (XML) text exporter.
        /// <para>See https://github.com/PRImA-Research-Lab/PAGE-XML </para>
        /// </summary>
        /// <param name="wordExtractor"></param>
        /// <param name="pageSegmenter"></param>
		/// <param name="readingOrderDetector"></param>
        /// <param name="scale"></param>
        /// <param name="indent">Indent character.</param>
        public PageXmlTextExporter(IWordExtractor wordExtractor, IPageSegmenter pageSegmenter, IReadingOrderDetector readingOrderDetector = null, double scale = 1.0, string indent = "\t")
        {
            this.wordExtractor = wordExtractor;
            this.pageSegmenter = pageSegmenter;
            this.readingOrderDetector = readingOrderDetector;
            this.scale = scale;
            indentChar = indent;
        }

        /// <summary>
        /// Get the PAGE-XML (XML) string of the pages layout.
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
            lineCount = 0;
            wordCount = 0;
            glyphCount = 0;
            regionCount = 0;
            groupOrderCount = 0;
            orderedRegions = new List<PageXmlDocument.PageXmlRegionRefIndexed>();

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
            var rgb = color.ToRGBValues();
            int red = (int)Math.Round(255f * (float)rgb.r);
            int green = 256 * (int)Math.Round(255f * (float)rgb.g);
            int blue = 65536 * (int)Math.Round(255f * (float)rgb.b);
            int sum = red + green + blue;

            // as per below, red and blue order might be inverted... var colorWin = System.Drawing.Color.FromArgb(sum);
            return sum.ToString();
        }

        private PageXmlDocument.PageXmlPage ToPageXmlPage(Page page, bool includePaths)
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

                regions.AddRange(blocks.Select(b => ToPageXmlTextRegion(b, page.Width, page.Height)));

                if (orderedRegions.Count > 0)
                {
                    pageXmlPage.ReadingOrder = new PageXmlDocument.PageXmlReadingOrder()
                    {
                        Item = new PageXmlDocument.PageXmlOrderedGroup()
                        {
                            Items = orderedRegions.ToArray(),
                            Id = "g" + groupOrderCount++
                        }
                    };
                }
            }

            var images = page.GetImages().ToList();
            if (images.Count > 0)
            {
                regions.AddRange(images.Select(i => ToPageXmlImageRegion(i, page.Width, page.Height)));
            }

            if (includePaths)
            {
                foreach (var path in page.ExperimentalAccess.Paths)
                {
                    var graphicalElement = ToPageXmlLineDrawingRegion(path, page.Width, page.Height);

                    if (graphicalElement != null)
                    {
                        regions.Add(graphicalElement);
                    }
                }
            }

            pageXmlPage.Items = regions.ToArray();
            return pageXmlPage;
        }

        private PageXmlDocument.PageXmlLineDrawingRegion ToPageXmlLineDrawingRegion(PdfPath pdfPath, double pageWidth, double pageHeight)
        {
            var bbox = pdfPath.GetBoundingRectangle();
            if (bbox.HasValue)
            {
                regionCount++;
                return new PageXmlDocument.PageXmlLineDrawingRegion()
                {
                    Coords = ToCoords(bbox.Value, pageWidth, pageHeight),
                    Id = "r" + regionCount
                };
            }
            return null;
        }

        private PageXmlDocument.PageXmlImageRegion ToPageXmlImageRegion(IPdfImage pdfImage, double pageWidth, double pageHeight)
        {
            regionCount++;
            var bbox = pdfImage.Bounds;
            return new PageXmlDocument.PageXmlImageRegion()
            {
                Coords = ToCoords(bbox, pageWidth, pageHeight),
                Id = "r" + regionCount
            };
        }

        private PageXmlDocument.PageXmlTextRegion ToPageXmlTextRegion(TextBlock textBlock, double pageWidth, double pageHeight)
        {
            regionCount++;
            string regionId = "r" + regionCount;

            if (readingOrderDetector != null && textBlock.ReadingOrder > -1)
            {
                orderedRegions.Add(new PageXmlDocument.PageXmlRegionRefIndexed()
                {
                    RegionRef = regionId,
                    Index = textBlock.ReadingOrder
                });
            }

            return new PageXmlDocument.PageXmlTextRegion()
            {
                Coords = ToCoords(textBlock.BoundingBox, pageWidth, pageHeight),
                Type = PageXmlDocument.PageXmlTextSimpleType.Paragraph,
                TextLines = textBlock.TextLines.Select(l => ToPageXmlTextLine(l, pageWidth, pageHeight)).ToArray(),
                TextEquivs = new[] { new PageXmlDocument.PageXmlTextEquiv() { Unicode = textBlock.Text } },
                Id = regionId
            };
        }

        private PageXmlDocument.PageXmlTextLine ToPageXmlTextLine(TextLine textLine, double pageWidth, double pageHeight)
        {
            lineCount++;
            return new PageXmlDocument.PageXmlTextLine()
            {
                Coords = ToCoords(textLine.BoundingBox, pageWidth, pageHeight),
                Production = PageXmlDocument.PageXmlProductionSimpleType.Printed,
                Words = textLine.Words.Select(w => ToPageXmlWord(w, pageWidth, pageHeight)).ToArray(),
                TextEquivs = new[] { new PageXmlDocument.PageXmlTextEquiv() { Unicode = textLine.Text } },
                Id = "l" + lineCount
            };
        }

        private PageXmlDocument.PageXmlWord ToPageXmlWord(Word word, double pageWidth, double pageHeight)
        {
            wordCount++;
            return new PageXmlDocument.PageXmlWord()
            {
                Coords = ToCoords(word.BoundingBox, pageWidth, pageHeight),
                Glyphs = word.Letters.Select(l => ToPageXmlGlyph(l, pageWidth, pageHeight)).ToArray(),
                TextEquivs = new[] { new PageXmlDocument.PageXmlTextEquiv() { Unicode = word.Text } },
                Id = "w" + wordCount
            };
        }

        private PageXmlDocument.PageXmlGlyph ToPageXmlGlyph(Letter letter, double pageWidth, double pageHeight)
        {
            glyphCount++;
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
                TextEquivs = new[] { new PageXmlDocument.PageXmlTextEquiv() { Unicode = letter.Value } },
                Id = "c" + glyphCount
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

            using (var reader = XmlReader.Create(xmlPath))
            {
                return (PageXmlDocument)serializer.Deserialize(reader);
            }
        }
    }
}
