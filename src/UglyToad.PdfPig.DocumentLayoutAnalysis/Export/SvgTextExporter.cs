namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using Content;
    using Graphics;
    using Graphics.Colors;
    using Graphics.Core;

    /// <summary>
    /// Exports a page as an SVG.
    /// </summary>
    public sealed class SvgTextExporter : ITextExporter
    {
        private readonly Func<string, string> invalidCharacterHandler;

        private static readonly Dictionary<string, string> Fonts = new Dictionary<string, string>()
        {
            { "ArialMT", "Arial Rounded MT Bold" }
        };

        /// <summary>
        /// Used to round numbers.
        /// </summary>
        public int Rounding { get; } = 4;

        /// <summary>
        /// <inheritdoc/>
        /// Not in use.
        /// </summary>
        public InvalidCharStrategy InvalidCharStrategy { get; }

        /// <summary>
        /// Svg text exporter.
        /// </summary>
        /// <param name="invalidCharacterHandler">How to handle invalid characters.</param>
        public SvgTextExporter(Func<string, string> invalidCharacterHandler)
            : this(InvalidCharStrategy.Custom, invalidCharacterHandler)
        { }

        /// <summary>
        /// Svg text exporter.
        /// </summary>
        /// <param name="invalidCharacterStrategy">How to handle invalid characters.</param>
        public SvgTextExporter(InvalidCharStrategy invalidCharacterStrategy = InvalidCharStrategy.DoNotCheck)
            : this(invalidCharacterStrategy, null)
        { }

        private SvgTextExporter(InvalidCharStrategy invalidCharacterStrategy, Func<string, string> invalidCharacterHandler)
        {
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
        /// Get the page contents as an SVG.
        /// </summary>
        public string Get(Page page)
        {
            var builder = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width='{Math.Round(page.Width, Rounding)}' height='{Math.Round(page.Height, Rounding)}'>\n<g transform=\"scale(1, 1) translate(0, 0)\">\n");

            foreach (var path in page.Paths)
            {
                if (!path.IsClipping)
                {
                    builder.AppendLine(PathToSvg(path, page.Height));
                }
            }

            var doc = new XmlDocument();

            foreach (var letter in page.Letters)
            {
                builder.Append(LetterToSvg(letter, page.Height, doc));
            }

            builder.Append("</g></svg>");
            return builder.ToString();
        }

        private string LetterToSvg(Letter l, double height, XmlDocument doc)
        {
            string fontFamily = GetFontFamily(l.FontName, out string style, out string weight);
            string rotation = "";
            if (l.GlyphRectangle.Rotation != 0)
            {
                rotation = $" transform='rotate({Math.Round(-l.GlyphRectangle.Rotation, Rounding)} {Math.Round(l.GlyphRectangle.BottomLeft.X, Rounding)},{Math.Round(height - l.GlyphRectangle.TopLeft.Y, Rounding)})'";
            }

            string fontSize = l.FontSize != 1 ? $"font-size='{l.FontSize:0}'" : $"style='font-size:{Math.Round(l.GlyphRectangle.Height, 2)}px'";

            var safeValue = XmlEscape(l, doc);
            var x = Math.Round(l.StartBaseLine.X, Rounding);
            var y = Math.Round(height - l.StartBaseLine.Y, Rounding);

            return $"<text x='{x}' y='{y}'{rotation} font-family='{fontFamily}' font-style='{style}' font-weight='{weight}' {fontSize} fill='{ColorToSvg(l.Color)}'>{safeValue}</text>"
                   + Environment.NewLine;
        }

        private static string GetFontFamily(string fontName, out string style, out string weight)
        {
            style = "normal";   // normal | italic | oblique
            weight = "normal";  // normal | bold | bolder | lighter

            // remove subset prefix
            if (fontName.Contains('+'))
            {
                if (fontName.Length > 7 && fontName[6] == '+')
                {
                    var split = fontName.Split('+');
                    if (split[0].All(char.IsUpper))
                    {
                        fontName = split[1];
                    }
                }
            }

            if (fontName.Contains('-'))
            {
                var infos = fontName.Split('-');
                fontName = infos[0];

                for (int i = 1; i < infos.Length; i++)
                {
                    string infoLower = infos[i].ToLowerInvariant();
                    if (infoLower.Contains("light"))
                    {
                        weight = "lighter";
                    }
                    else if (infoLower.Contains("bolder"))
                    {
                        weight = "bolder";
                    }
                    else if (infoLower.Contains("bold"))
                    {
                        weight = "bold";
                    }

                    if (infoLower.Contains("italic"))
                    {
                        style = "italic";
                    }
                    else if (infoLower.Contains("oblique"))
                    {
                        style = "oblique";
                    }
                }
            }

            if (Fonts.ContainsKey(fontName))
            {
                fontName = Fonts[fontName];
            }

            return fontName;
        }

        private string XmlEscape(Letter letter, XmlDocument doc)
        {
            XmlNode node = doc.CreateElement("root");
            node.InnerText = invalidCharacterHandler(letter.Value);
            return node.InnerXml;
        }

        private static string ColorToSvg(IColor color)
        {
            if (color == null)
            {
                return string.Empty;
            }

            var (r, g, b) = color.ToRGBValues();
            return $"rgb({Convert.ToByte(r * 255)},{Convert.ToByte(g * 255)},{Convert.ToByte(b * 255)})";
        }

        private static string PathToSvg(PdfPath p, double height)
        {
            var builder = new StringBuilder();
            foreach (var subpath in p)
            {
                foreach (var command in subpath.Commands)
                {
                    command.WriteSvg(builder, height);
                }
            }

            if (builder.Length == 0)
            {
                return string.Empty;
            }

            if (builder[builder.Length - 1] == ' ')
            {
                builder.Remove(builder.Length - 1, 1);
            }

            var glyph = builder.ToString();

            string dashArray = "";
            string capStyle = "";
            string jointStyle = "";
            string strokeColor = " stroke='none'";
            string strokeWidth = "";

            if (p.IsStroked)
            {
                strokeColor = $" stroke='{ColorToSvg(p.StrokeColor)}'";
                strokeWidth = $" stroke-width='{p.LineWidth}'";

                if (p.LineDashPattern.HasValue && p.LineDashPattern.Value.Array.Count > 0)
                {
                    dashArray = $" stroke-dasharray='{string.Join(" ", p.LineDashPattern.Value.Array)}'";
                }

                if (p.LineCapStyle != LineCapStyle.Butt)
                {
                    if (p.LineCapStyle == LineCapStyle.Round)
                    {
                        capStyle = " stroke-linecap='round'";
                    }
                    else
                    {
                        capStyle = " stroke-linecap='square'";
                    }
                }

                if (p.LineJoinStyle != LineJoinStyle.Miter)
                {
                    if (p.LineJoinStyle == LineJoinStyle.Round)
                    {
                        jointStyle = " stroke-linejoin='round'";
                    }
                    else
                    {
                        jointStyle = " stroke-linejoin='bevel'";
                    }
                }
            }

            string fillColor = " fill='none'";
            const string fillRule = ""; // For further dev

            if (p.IsFilled)
            {
                fillColor = $" fill='{ColorToSvg(p.FillColor)}'";
            }

            var path = $"<path d='{glyph}'{fillColor}{fillRule}{strokeColor}{strokeWidth}{dashArray}{capStyle}{jointStyle}></path>";
            return path;
        }
    }
}
