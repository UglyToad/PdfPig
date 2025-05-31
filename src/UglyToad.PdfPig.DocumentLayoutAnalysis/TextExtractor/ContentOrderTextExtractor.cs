namespace UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor
{
    using System;
    using System.Text;
    using Content;
    using System.Collections.Generic;
    using Util;

    /// <summary>
    /// Extracts text from a document based on the content order in the file.
    /// </summary>
    public static class ContentOrderTextExtractor
    {
        private static readonly HashSet<string> ReplaceableWhitespace = new HashSet<string>
        {
            "\t",
            "\v",
            "\f"
        };

        /// <summary>
        /// Gets a human readable representation of the text from the page based on
        /// the letter order of the original PDF document.
        /// </summary>
        /// <param name="page">A page from the document.</param>
        /// <param name="addDoubleNewline">Whether to include a double new-line when the text is likely to be a new paragraph.</param>
        public static string GetText(Page page, bool addDoubleNewline = false)
            => GetText(
                page,
                new Options
                {
                    SeparateParagraphsWithDoubleNewline = addDoubleNewline
                });

        /// <summary>
        /// Gets a human readable representation of the text from the page based on
        /// the letter order of the original PDF document.
        /// </summary>
        /// <param name="page">A page from the document.</param>
        /// <param name="options">Control various aspects of the generated text.</param>
        public static string GetText(Page page, Options options)
        {
            options ??= new Options();

            var sb = new StringBuilder();

            var previous = default(Letter);
            var hasJustAddedWhitespace = false;
            for (var i = 0; i < page.Letters.Count; i++)
            {
                var letter = page.Letters[i];

                if (string.IsNullOrEmpty(letter.Value))
                {
                    continue;
                }

                if (options.ReplaceWhitespaceWithSpace && ReplaceableWhitespace.Contains(letter.Value))
                {
                    letter = new Letter(
                        " ",
                        letter.GlyphRectangle,
                        letter.StartBaseLine,
                        letter.EndBaseLine,
                        letter.Width,
                        letter.FontSize,
                        letter.Font,
                        letter.RenderingMode,
                        letter.StrokeColor,
                        letter.FillColor,
                        letter.PointSize,
                        letter.TextSequence);
                }

                if (letter.Value == " " && !hasJustAddedWhitespace)
                {
                    if (previous != null && IsNewline(previous, letter, page, out _))
                    {
                        continue;
                    }

                    sb.Append(" ");
                    previous = letter;
                    hasJustAddedWhitespace = true;
                    continue;
                }

                hasJustAddedWhitespace = false;

                if (previous != null && letter.Value != " ")
                {
                    var nwPrevious = GetNonWhitespacePrevious(page, i);

                    if (IsNewline(nwPrevious, letter, page, out var isDoubleNewline))
                    {
                        if (previous.Value == " ")
                        {
                            sb.Remove(sb.Length - 1, 1);
                        }

                        sb.AppendLine();
                        if (options.SeparateParagraphsWithDoubleNewline && isDoubleNewline)
                        {
                            sb.AppendLine();
                        }

                        hasJustAddedWhitespace = true;
                    }
                    else if (previous.Value != " ")
                    {
                        var gap = letter.StartBaseLine.X - previous.EndBaseLine.X;

                        if (options.NegativeGapAsWhitespace)
                        {
                            gap = Math.Abs(gap);
                        }

                        if (WhitespaceSizeStatistics.IsProbablyWhitespace(gap, previous))
                        {
                            sb.Append(" ");
                            hasJustAddedWhitespace = true;
                        }
                    }
                }

                sb.Append(letter.Value);
                previous = letter;
            }

            return sb.ToString();
        }

        private static Letter GetNonWhitespacePrevious(Page page, int index)
        {
            for (var i = index - 1; i >= 0; i--)
            {
                var letter = page.Letters[i];
                if (!string.IsNullOrWhiteSpace(letter.Value))
                {
                    return letter;
                }
            }

            return null;
        }

        private static bool IsNewline(Letter previous, Letter letter, Page page, out bool isDoubleNewline)
        {
            isDoubleNewline = false;

            if (previous == null)
            {
                return false;
            }

            var ptSizePrevious = (int)Math.Round(previous.PointSize);
            var ptSize = (int)Math.Round(letter.PointSize);
            var minPtSize = ptSize < ptSizePrevious ? ptSize : ptSizePrevious;

            var gap = Math.Abs(previous.StartBaseLine.Y - letter.StartBaseLine.Y);

            if (gap > minPtSize * 1.7 && previous.StartBaseLine.Y > letter.StartBaseLine.Y)
            {
                isDoubleNewline = true;
            }

            return gap > minPtSize * 0.9;
        }

        /// <summary>
        /// Options controlling the text generation algorithm.
        /// </summary>
        public class Options
        {
            /// <summary>
            /// Whether to include a double new-line when the text is likely to be a new paragraph.
            /// Default <see langword="false"/>.
            /// </summary>
            public bool SeparateParagraphsWithDoubleNewline { get; set; }

            /// <summary>
            /// Whether to replace all whitespace characters (except line breaks) with single space ' '
            /// character. Default <see langword="false"/>.
            /// </summary>
            public bool ReplaceWhitespaceWithSpace { get; set; }

            /// <summary>
            /// When parsing PDF files with tables containing multiple lines in a cell or "merged" cells,
            /// the separate words can appear out of horizontal order. This option can better predict the
            ///  spaces between the words. Default <see langword="false"/>.
            /// </summary>
            public bool NegativeGapAsWhitespace { get; set; }
        }
    }
}
