using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.Export.PAGE;
using UglyToad.PdfPig.Geometry;
using UglyToad.PdfPig.Graphics.Colors;
using UglyToad.PdfPig.Util;

namespace UglyToad.PdfPig.Export
{
    /// <summary>
    /// PAGE-XML 2019-07-15 (XML) text exporter.
    /// <para>See https://github.com/PRImA-Research-Lab/PAGE-XML </para>
    /// </summary>
    public class PageXmlTextExporter : ITextExporter
    {
        private IPageSegmenter pageSegmenter;
        private IWordExtractor wordExtractor;

        private double scale;
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
            this.scale = scale;
            indentChar = indent;
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

        private string PointToString(PdfPoint point, double height)
        {
            double x = Math.Round(point.X * scale);
            double y = Math.Round((height - point.Y) * scale);
            return (x > 0 ? x : 0).ToString("0") + "," + (y > 0 ? y : 0).ToString("0");
        }

        private string ToPoints(IEnumerable<PdfPoint> points, double height)
        {
            return string.Join(" ", points.Select(p => PointToString(p, height)));
        }

        private string ToPoints(PdfRectangle pdfRectangle, double height)
        {
            return ToPoints(new[] { pdfRectangle.BottomLeft, pdfRectangle.TopLeft, pdfRectangle.TopRight, pdfRectangle.BottomRight }, height);
        }

        private PageXmlDocument.PageXmlCoords ToCoords(PdfRectangle pdfRectangle, double height)
        {
            return new PageXmlDocument.PageXmlCoords()
            {
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

            // as per below, red and blue order might be inverted... var colorWin = System.Drawing.Color.FromArgb(sum);
            return sum.ToString();
        }

        private PageXmlDocument.PageXmlPage ToPageXmlPage(Page page, bool includePaths)
        {
            var pageXmlPage = new PageXmlDocument.PageXmlPage()
            {
                ImageFilename = "unknown",
                ImageHeight = (int)Math.Round(page.Height * scale),
                ImageWidth = (int)Math.Round(page.Width * scale),
            };

            var regions = new List<PageXmlDocument.PageXmlRegion>();

            var words = page.GetWords(wordExtractor);
            if (words.Count() > 0)
            {
                var blocks = pageSegmenter.GetBlocks(words);
                regions.AddRange(blocks.Select(b => ToPageXmlTextRegion(b, page.Height)));
            }

            var images = page.GetImages();
            if (images.Count() > 0)
            {
                regions.AddRange(images.Select(i => ToPageXmlImageRegion(i, page.Height)));
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

        private PageXmlDocument.PageXmlLineDrawingRegion ToPageXmlLineDrawingRegion(PdfPath pdfPath, double height)
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

        private PageXmlDocument.PageXmlImageRegion ToPageXmlImageRegion(IPdfImage pdfImage, double height)
        {
            regionCount++;
            var bbox = pdfImage.Bounds;
            return new PageXmlDocument.PageXmlImageRegion()
            {
                Coords = ToCoords(bbox, height),
                Id = "r" + regionCount
            };
        }

        private PageXmlDocument.PageXmlTextRegion ToPageXmlTextRegion(TextBlock textBlock, double height)
        {
            regionCount++;
            return new PageXmlDocument.PageXmlTextRegion()
            {
                Coords = ToCoords(textBlock.BoundingBox, height),
                Type = PageXmlDocument.PageXmlTextSimpleType.Paragraph,
                TextLines = textBlock.TextLines.Select(l => ToPageXmlTextLine(l, height)).ToArray(),
                TextEquivs = new[] { new PageXmlDocument.PageXmlTextEquiv() { Unicode = textBlock.Text } },
                Id = "r" + regionCount
            };
        }

        private PageXmlDocument.PageXmlTextLine ToPageXmlTextLine(TextLine textLine, double height)
        {
            lineCount++;
            return new PageXmlDocument.PageXmlTextLine()
            {
                Coords = ToCoords(textLine.BoundingBox, height),
                Production = PageXmlDocument.PageXmlProductionSimpleType.Printed,
                Words = textLine.Words.Select(w => ToPageXmlWord(w, height)).ToArray(),
                TextEquivs = new[] { new PageXmlDocument.PageXmlTextEquiv() { Unicode = textLine.Text } },
                Id = "l" + lineCount
            };
        }

        private PageXmlDocument.PageXmlWord ToPageXmlWord(Word word, double height)
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

        private PageXmlDocument.PageXmlGlyph ToPageXmlGlyph(Letter letter, double height)
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

            using (var reader = XmlReader.Create(xmlPath))
            {
                return (PageXmlDocument)serializer.Deserialize(reader);
            }
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
    }
}
