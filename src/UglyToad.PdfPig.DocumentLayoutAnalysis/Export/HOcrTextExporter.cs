﻿namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export
{
    using Content;
    using Core;
    using DocumentLayoutAnalysis;
    using System;
    using System.Linq;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
    using UglyToad.PdfPig.Graphics;
    using Util;

    /// <summary>
    /// hOCR v1.2 (HTML) text exporter.
    /// <para>See http://kba.cloud/hocr-spec/1.2/ </para>
    /// </summary>
    public sealed class HOcrTextExporter : ITextExporter
    {
        private const string XmlHeader = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\"\n\t\"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">\n";
        private const string Hocrjs = "<script src='https://unpkg.com/hocrjs'></script>\n";

        private readonly IPageSegmenter pageSegmenter;
        private readonly IWordExtractor wordExtractor;
        private readonly Func<string, string> invalidCharacterHandler;
        private readonly double scale;
        private readonly string indentChar;

        private int pageCount;
        private int areaCount;
        private int lineCount;
        private int wordCount;
        private int pathCount;
        private int paraCount;
        private int imageCount;

        /// <inheritdoc/>
        public InvalidCharStrategy InvalidCharStrategy { get; }

        /// <summary>
        /// hOCR v1.2 (HTML)
        /// <para>See http://kba.cloud/hocr-spec/1.2/ </para>
        /// </summary>
        /// <param name="wordExtractor">Extractor used to identify words in the document.</param>
        /// <param name="pageSegmenter">Segmenter used to split page into blocks.</param>
        /// <param name="scale">Scale multiplier to apply to output document, defaults to 1.</param>
        /// <param name="indentChar">Character to use for indentation, defaults to tab.</param>
        /// <param name="invalidCharacterHandler">How to handle invalid characters.</param>
        public HOcrTextExporter(IWordExtractor wordExtractor, IPageSegmenter pageSegmenter,
                                   double scale, string indentChar,
                                   Func<string, string> invalidCharacterHandler)
            : this(wordExtractor, pageSegmenter, scale, indentChar,
                  InvalidCharStrategy.Custom, invalidCharacterHandler)
        { }

        /// <summary>
        /// hOCR v1.2 (HTML)
        /// <para>See http://kba.cloud/hocr-spec/1.2/ </para>
        /// </summary>
        /// <param name="wordExtractor">Extractor used to identify words in the document.</param>
        /// <param name="pageSegmenter">Segmenter used to split page into blocks.</param>
        /// <param name="scale">Scale multiplier to apply to output document, defaults to 1.</param>
        /// <param name="indentChar">Character to use for indentation, defaults to tab.</param>
        /// <param name="invalidCharacterStrategy">How to handle invalid characters.</param>
        public HOcrTextExporter(IWordExtractor wordExtractor, IPageSegmenter pageSegmenter,
                                   double scale = 1, string indentChar = "\t",
                                   InvalidCharStrategy invalidCharacterStrategy = InvalidCharStrategy.DoNotCheck)
             : this(wordExtractor, pageSegmenter, scale, indentChar,
                  invalidCharacterStrategy, null)
        { }

        private HOcrTextExporter(IWordExtractor wordExtractor, IPageSegmenter pageSegmenter,
                           double scale, string indentChar,
                           InvalidCharStrategy invalidCharacterStrategy,
                           Func<string, string> invalidCharacterHandler)
        {
            this.wordExtractor = wordExtractor;
            this.pageSegmenter = pageSegmenter;
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
        /// Get the hOCR (HTML) string of the page layout.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="includePaths">Draw PdfPaths present in the page.</param>
        /// <param name="useHocrjs">Will add a reference to the 'hocrjs' script just before the closing 'body' tag, adding the
        /// interface to a plain hOCR file.<para>See https://github.com/kba/hocrjs for more information.</para></param>
        public string Get(PdfDocument document, bool includePaths = false, bool useHocrjs = false)
        {
            string hocr = GetHead() + indentChar + "<body>\n";

            for (var i = 0; i < document.NumberOfPages; i++)
            {
                var page = document.GetPage(i + 1);
                hocr += GetCode(page, includePaths) + "\n";
            }

            if (useHocrjs)
            {
                hocr += indentChar + indentChar + Hocrjs;
            }

            hocr += indentChar + "</body>";
            return XmlHeader + AddHtmlHeader(hocr);
        }

        /// <summary>
        /// Get the hOCR (HTML) string of the page layout. Excludes PdfPaths.
        /// </summary>
        /// <param name="page">The page.</param>
        public string Get(Page page)
        {
            return Get(page, false);
        }

        /// <summary>
        /// Get the hOCR (HTML) string of the page layout.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="includePaths">Draw PdfPaths present in the page.</param>
        /// <param name="imageName">The image name, if any.</param>
        /// <param name="useHocrjs">Will add a reference to the 'hocrjs' script just before the closing 'body' tag, adding the interface to a plain hOCR file.<para>See https://github.com/kba/hocrjs for more information.</para></param>
        public string Get(Page page, bool includePaths = false, string imageName = "unknown", bool useHocrjs = false)
        {
            string hocr = GetHead() + indentChar + "<body>\n";

            hocr += GetCode(page, includePaths, imageName) + "\n";

            if (useHocrjs)
            {
                hocr += indentChar + indentChar + Hocrjs;
            }

            hocr += indentChar + "</body>";
            return XmlHeader + AddHtmlHeader(hocr);
        }

        private string GetHead()
        {
            return indentChar + "<head>" +
                "\n" + indentChar + indentChar + "<title></title>" +
                "\n" + indentChar + indentChar + "<meta http-equiv='Content-Type' content='text/html;charset=utf-8' />" +
                "\n" + indentChar + indentChar + "<meta name='ocr-system' content='" + pageSegmenter.GetType().Name + "|" + wordExtractor.GetType().Name + "' />" +
                "\n" + indentChar + indentChar + "<meta name='ocr-capabilities' content='ocr_page ocr_carea ocr_par ocr_line ocrx_word ocr_linedrawing' />" +
                "\n" + indentChar + "</head>\n";
        }

        private string AddHtmlHeader(string content)
        {
            return "<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\" lang=\"en\">\n" + content + "\n</html>";
        }

        /// <summary>
        /// Get indent string from level.
        /// </summary>
        /// <param name="level">The indent level.</param>
        private string GetIndent(int level)
        {
            string indent = "";
            for (int i = 0; i < level; i++)
            {
                indent += indentChar;
            }
            return indent;
        }

        /// <summary>
        /// Get the hOCR string for the page.
        /// <para>http://kba.cloud/hocr-spec/1.2/#elementdef-ocr_page</para>
        /// </summary>
        /// <param name="page"></param>
        /// <param name="includePaths">Draw PdfPaths present in the page.</param>
        /// <param name="imageName"></param>
        private string GetCode(Page page, bool includePaths, string imageName = "unknown")
        {
            pageCount++;
            int level = 2;

            string hocr = GetIndent(level) + "<div class='ocr_page' id='page_" + page.Number.ToString() +
                "' title='image \"" + imageName + "\"; bbox 0 0 " +
                (int)Math.Round(page.Width * scale) + " " + (int)Math.Round(page.Height * scale) +
                "; ppageno " + (page.Number - 1) + "\'>";

            if (includePaths)
            {
                foreach (var path in page.ExperimentalAccess.Paths)
                {
                    hocr += "\n" + GetCode(path, page.Height, true, level + 1);
                }
            }

            foreach (var image in page.GetImages())
            {
                hocr += "\n" + GetCode(image, page.Height, level + 1);
            }

            var words = page.GetWords(wordExtractor);

            if (words.Any())
            {
                foreach (var block in pageSegmenter.GetBlocks(words))
                {
                    hocr += "\n" + GetCodeArea(block, page.Height, level + 1);
                }
            }

            hocr += "\n" + GetIndent(level) + "</div>";
            return hocr;
        }

        /// <summary>
        /// Get the hOCR string for the path.
        /// <para>http://kba.cloud/hocr-spec/1.2/#elementdef-ocr_linedrawing</para>
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pageHeight"></param>
        /// <param name="subPaths"></param>
        /// <param name="level">The indent level.</param>
        private string GetCode(PdfPath path, double pageHeight, bool subPaths, int level)
        {
            if (path == null)
            {
                return string.Empty;
            }

            string hocr = string.Empty;

            if (subPaths)
            {
                var bbox = path.GetBoundingRectangle();
                if (bbox.HasValue)
                {
                    areaCount++;
                    hocr += GetIndent(level) + "<div class='ocr_carea' id='block_" + pageCount + "_"
                        + areaCount + "' title='" + GetCode(bbox.Value, pageHeight) + "'>\n";
                    foreach (var subPath in path)
                    {
                        var subBbox = subPath.GetBoundingRectangle();
                        if (subBbox.HasValue)
                        {
                            pathCount++;
                            hocr += GetIndent(level + 1) + "<span class='ocr_linedrawing' id='drawing_" + pageCount + "_"
                                + pathCount + "' title='" + GetCode(subBbox.Value, pageHeight) + "' />\n";
                        }
                    }
                    hocr += GetIndent(level) + "</div>";
                }
            }
            else
            {
                var bbox = path.GetBoundingRectangle();
                if (bbox.HasValue)
                {
                    pathCount++;
                    hocr += GetIndent(level) + "<span class='ocr_linedrawing' id='drawing_" + pageCount + "_"
                            + pathCount + "' title='" + GetCode(bbox.Value, pageHeight) + "' />";
                }
            }

            return hocr;
        }

        private string GetCode(IPdfImage pdfImage, double pageHeight, int level)
        {
            imageCount++;
            var bbox = pdfImage.Bounds;
            return GetIndent(level) + "<span class='ocr_image' id='image_" + pageCount + "_"
                            + imageCount + "' title='" + GetCode(bbox, pageHeight) + "' />";
        }

        /// <summary>
        /// Get the hOCR string for the area.
        /// <para>http://kba.cloud/hocr-spec/1.2/#elementdef-ocr_carea</para>
        /// </summary>
        /// <param name="block">The text area.</param>
        /// <param name="pageHeight"></param>
        /// <param name="level">The indent level.</param>
        private string GetCodeArea(TextBlock block, double pageHeight, int level)
        {
            areaCount++;

            string hocr = GetIndent(level) + "<div class='ocr_carea' id='block_" + pageCount + "_"
                + areaCount + "' title='" + GetCode(block.BoundingBox, pageHeight) + "'>";

            hocr += GetCodeParagraph(block, pageHeight, level + 1); // we concider 1 area = 1 block. should change in the future
            hocr += "\n" + GetIndent(level) + "</div>";
            return hocr;
        }

        /// <summary>
        /// Get the hOCR string for the paragraph.
        /// <para>See http://kba.cloud/hocr-spec/1.2/#elementdef-ocr_par</para>
        /// </summary>
        /// <param name="block">The paragraph.</param>
        /// <param name="pageHeight"></param>
        /// <param name="level">The indent level.</param>
        private string GetCodeParagraph(TextBlock block, double pageHeight, int level)
        {
            paraCount++;
            string hocr = "\n" + GetIndent(level) + "<p class='ocr_par' id='par_" + pageCount + "_"
                + paraCount + "' title='" + GetCode(block.BoundingBox, pageHeight) + "'>"; // lang='eng'

            foreach (var line in block.TextLines)
            {
                hocr += "\n" + GetCode(line, pageHeight, level + 1);
            }
            hocr += "\n" + GetIndent(level) + "</p>";

            return hocr;
        }

        /// <summary>
        /// Get the hOCR string for the text line.
        /// <para>See http://kba.cloud/hocr-spec/1.2/#elementdef-ocr_line</para>
        /// </summary>
        /// <param name="line"></param>
        /// <param name="pageHeight"></param>
        /// <param name="level">The indent level.</param>
        private string GetCode(TextLine line, double pageHeight, int level)
        {
            lineCount++;
            double angle = 0;

            // http://kba.cloud/hocr-spec/1.2/#propdef-baseline
            // below will be 0 as long as the word's bounding box bottom is the BaseLine and not 'Bottom'
            double baseLine = (double)line.Words[0].Letters[0].StartBaseLine.Y;
            baseLine = (double)line.BoundingBox.Bottom - baseLine;

            string hocr = GetIndent(level) + "<span class='ocr_line' id='line_" + pageCount + "_" + lineCount + "' title='" +
                GetCode(line.BoundingBox, pageHeight) + "; baseline " + angle + " 0'>"; //"; x_size 42; x_descenders 5; x_ascenders 12' >";

            foreach (var word in line.Words)
            {
                hocr += "\n" + GetCode(word, pageHeight, level + 1);
            }
            hocr += "\n" + GetIndent(level) + "</span>";
            return hocr;
        }

        /// <summary>
        /// Get the hOCR string for the word.
        /// <para>See http://kba.cloud/hocr-spec/1.2/#elementdef-ocrx_word</para>
        /// </summary>
        /// <param name="word"></param>
        /// <param name="pageHeight"></param>
        /// <param name="level">The indent level.</param>
        private string GetCode(Word word, double pageHeight, int level)
        {
            wordCount++;
            string hocr = GetIndent(level) +
                "<span class='ocrx_word' id='word_" + pageCount + "_" + wordCount +
                "' title='" + GetCode(word.BoundingBox, pageHeight) + "; x_wconf " + GetConfidence(word);

            hocr += "; x_font " + word.FontName;

            if (word.Letters.Count > 0 && word.Letters[0].FontSize != 1)
            {
                hocr += "; x_fsize " + word.Letters[0].FontSize;
            }
            hocr += "'";

            hocr += ">" + invalidCharacterHandler(word.Text) + "</span> ";
            return hocr;
        }

        private int GetConfidence(Word word)
        {
            return 100;
        }

        /// <summary>
        /// Get the hOCR string for the bounding box.
        /// <para>See http://kba.cloud/hocr-spec/1.2/#propdef-bbox</para>
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="pageHeight"></param>
        private string GetCode(PdfRectangle rectangle, double pageHeight)
        {
            // the values are with reference to the the top-left 
            // corner of the document image and measured in pixels

            var left = (int)Math.Round(rectangle.Left * scale);
            var top = (int)Math.Round((pageHeight - rectangle.Top) * scale);
            var right = (int)Math.Round(rectangle.Right * scale);
            var bottom = (int)Math.Round((pageHeight - rectangle.Bottom) * scale);

            return "bbox " + (left > 0 ? left : 0) + " "
                            + (top > 0 ? top : 0) + " "
                            + (right > 0 ? right : 0) + " "
                            + (bottom > 0 ? bottom : 0);
        }
    }
}
