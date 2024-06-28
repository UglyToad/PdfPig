namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export
{
    using Content;
    using Core;
    using DocumentLayoutAnalysis;
    using Graphics.Colors;
    using PAGE;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// PAGE-XML 2019-07-15 (XML) exporter for general case
    /// This is a rewrite of <see cref="PageXmlTextExporter"/> to be simple and handle a general case of text, image
    /// and custom implementer defined blocks
    /// <para>See https://github.com/PRImA-Research-Lab/PAGE-XML </para>
    /// </summary>
    public class PageXmlGeneralExporter
    {
        private readonly double scale;
        private string indentChar;
        private int nextId;

        /// <summary>
        /// PAGE-XML 2019-07-15 (XML) exporter for general case
        /// <para>See https://github.com/PRImA-Research-Lab/PAGE-XML </para>
        /// </summary>
        /// <param name="scale"></param>
        /// <param name="indent"></param>
        public PageXmlGeneralExporter(double scale = 1.0, string indent = "\t")
        {
            this.scale = scale;
            indentChar = indent;
        }

        /// <summary>
        /// Get the PAGE-XML (XML) string of the pages layout using the <see cref="IBoundingBox"></see>'s as the page layout
        /// </summary>
        /// <param name="page">The Page</param>
        /// <param name="blocks">Blocks to be exported</param>
        /// <returns></returns>
        public string Get(Page page, IEnumerable<IBoundingBox> blocks)
        {
            PageXmlDocument pageXmlDocument = new PageXmlDocument()
            {
                Metadata = new PageXmlDocument.PageXmlMetadata()
                {
                    Created = DateTime.UtcNow,
                    LastChange = DateTime.UtcNow,
                    Creator = "PdfPig",
                    Comments = "",
                },
                PcGtsId = "pc-" + page.GetHashCode()
            };

            var xmlPage = CreatePage(page.Height, page.Width, blocks);

            pageXmlDocument.Page = xmlPage;

            return Serialize(pageXmlDocument);
        }

        private PageXmlDocument.PageXmlPage CreatePage(double pageHeight, double pageWidth, IEnumerable<IBoundingBox> blocks)
        {
            var pageXmlPage = new PageXmlDocument.PageXmlPage()
            {
                ImageFilename = "unknown",
                ImageHeight = (int)Math.Round(pageHeight * scale),
                ImageWidth = (int)Math.Round(pageWidth * scale),
            };

            var regions = blocks
                .Select(b => ToRegion(b, pageWidth, pageHeight))
                .Where(x => x != null).ToList();
            pageXmlPage.Items = regions.ToArray();

            var regionsOrder = regions.Select(x => x.Id);

            var orderedRegions = GetOrderRegions(regionsOrder).ToArray();
            pageXmlPage.ReadingOrder = new PageXmlDocument.PageXmlReadingOrder()
            {
                Item = new PageXmlDocument.PageXmlOrderedGroup()
                {
                    Items = orderedRegions,
                    Id = "g" + NextId()
                }
            };

            return pageXmlPage;
        }

        private IEnumerable<PageXmlDocument.PageXmlRegionRefIndexed> GetOrderRegions(IEnumerable<string> idOrder)
        {
            var index = 1;
            foreach (var item in idOrder)
            {
                yield return new PageXmlDocument.PageXmlRegionRefIndexed()
                {
                    RegionRef = item,
                    Index = index++
                };
            }
        }

        private PageXmlDocument.PageXmlRegion ToRegion(IBoundingBox block, double pageWidth, double pageHeight)
        {
            if (block is TextBlock textblock)
            {
                return ToPageXmlTextRegion(textblock, pageWidth, pageHeight);
            }

            if (block is ILettersBlock blockOfLetters)
            {
                return ToPageXmlSimpleTextRegion(blockOfLetters.BoundingBox, blockOfLetters.Text, pageWidth, pageHeight);
            }

            if (block is IPdfImage imageBlock)
            {
                return ToImageRegion(imageBlock.BoundingBox, pageWidth, pageHeight);
            }

            // Default case
            return ToPageXmlSimpleTextRegion(block.BoundingBox, block.ToString(), pageWidth, pageHeight);
        }

        private PageXmlDocument.PageXmlImageRegion ToImageRegion(PdfRectangle box, double pageWidth, double pageHeight)
        {
            return new PageXmlDocument.PageXmlImageRegion()
            {
                Coords = ToCoords(box, pageWidth, pageHeight),
                Id = "r" + NextId(),
            };
        }

        private PageXmlDocument.PageXmlTableRegion ToTableRegion(PdfRectangle box, double pageWidth, double pageHeight)
        {
            return new PageXmlDocument.PageXmlTableRegion()
            {
                Coords = ToCoords(box, pageWidth, pageHeight),
                Id = "r" + NextId(),
            };
        }

        private PageXmlDocument.PageXmlCustomRegion ToCustomRegion(PdfRectangle box, string text, double pageWidth, double pageHeight)
        {
            if (box.TopLeft.Equals(box.BottomRight))
            {
                return null;
            }

            return new PageXmlDocument.PageXmlCustomRegion()
            {
                Coords = ToCoords(box, pageWidth, pageHeight),
                Id = "r" + NextId(),
                Type = text
            };
        }

        private PageXmlDocument.PageXmlTextRegion ToPageXmlSimpleTextRegion(PdfRectangle box, string text, double pageWidth, double pageHeight)
        {
            string regionId = "r" + NextId();

            return new PageXmlDocument.PageXmlTextRegion()
            {
                Coords = ToCoords(box, pageWidth, pageHeight),
                Type = PageXmlDocument.PageXmlTextSimpleType.Paragraph,
                TextLines = new PageXmlDocument.PageXmlTextLine[0],
                TextEquivs = new[] { new PageXmlDocument.PageXmlTextEquiv() { Unicode = text } },
                Id = regionId
            };
        }

        private PageXmlDocument.PageXmlTextRegion ToPageXmlTextRegion(TextBlock textBlock, double pageWidth, double pageHeight)
        {
            string regionId = "r" + NextId();


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
            return new PageXmlDocument.PageXmlTextLine()
            {
                Coords = ToCoords(textLine.BoundingBox, pageWidth, pageHeight),
                Production = PageXmlDocument.PageXmlProductionSimpleType.Printed,
                Words = textLine.Words.Select(w => ToPageXmlWord(w, pageWidth, pageHeight)).ToArray(),
                TextEquivs = new[] { new PageXmlDocument.PageXmlTextEquiv() { Unicode = textLine.Text } },
                Id = "l" + NextId()
            };
        }

        private PageXmlDocument.PageXmlWord ToPageXmlWord(Word word, double pageWidth, double pageHeight)
        {
            return new PageXmlDocument.PageXmlWord()
            {
                Coords = ToCoords(word.BoundingBox, pageWidth, pageHeight),
                Glyphs = word.Letters.Select(l => ToPageXmlGlyph(l, pageWidth, pageHeight)).ToArray(),
                TextEquivs = new[] { new PageXmlDocument.PageXmlTextEquiv() { Unicode = word.Text } },
                Id = "w" + NextId()
            };
        }

        private PageXmlDocument.PageXmlGlyph ToPageXmlGlyph(Letter letter, double pageWidth, double pageHeight)
        {
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
                Id = "c" + NextId()
            };
        }

        private string PointToString(PdfPoint point, double pageWidth, double pageHeight)
        {
            double x = Math.Round(point.X * scale);
            double y = Math.Round((pageHeight - point.Y) * scale);

            // move away from borders
            x = x > 1 ? x : 1;
            y = y > 1 ? y : 1;

            x = x < pageWidth - 1 ? x : pageWidth - 1;
            y = y < pageHeight - 1 ? y : pageHeight - 1;

            return x.ToString("0") + "," + y.ToString("0");
        }

        private string ToPoints(IEnumerable<PdfPoint> points, double pageWidth, double pageHeight)
        {
            return string.Join(" ", points.Select(p => PointToString(p, pageWidth, pageHeight)));
        }

        private string ToPoints(PdfRectangle pdfRectangle, double pageWidth, double pageHeight)
        {
            return ToPoints(
                new[] { pdfRectangle.BottomLeft, pdfRectangle.TopLeft, pdfRectangle.TopRight, pdfRectangle.BottomRight },
                pageWidth, pageHeight);
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

        private string Serialize(PageXmlDocument pageXmlDocument)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PageXmlDocument));
            var settings = new XmlWriterSettings()
            {
                Encoding = System.Text.Encoding.UTF8,
                Indent = true,
                IndentChars = indentChar,
            };

            using (var memoryStream = new MemoryStream())
            using (var xmlWriter = XmlWriter.Create(memoryStream, settings))
            {
                serializer.Serialize(xmlWriter, pageXmlDocument);
                return System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }

        private int NextId()
        {
            return nextId++;
        }
    }
}
