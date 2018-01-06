namespace UglyToad.Pdf.Fonts.Parser
{
    using System;
    using System.Text;
    using Exceptions;
    using IO;
    using Pdf.Parser.Parts;

    internal class AdobeFontMetricsParser : IAdobeFontMetricsParser
    {
        /// <summary>
        /// This is a comment in a AFM file.
        /// </summary>
        public const string Comment = "Comment";

        /// <summary>
        /// This is the constant used in the AFM file to start a font metrics item.
        /// </summary>
        public const string StartFontMetrics = "StartFontMetrics";

        /// <summary>
        /// This is the constant used in the AFM file to end a font metrics item.
        /// </summary>
        public const string EndFontMetrics = "EndFontMetrics";

        /// <summary>
        /// The font name.
        /// </summary>
        public const string FontName = "FontName";

        /// <summary>
        /// The full name.
        /// </summary>
        public const string FullName = "FullName";

        /// <summary>
        /// The family name.
        /// </summary>
        public const string FamilyName = "FamilyName";

        /// <summary>
        /// The weight.
        /// </summary>
        public const string Weight = "Weight";

        /// <summary>
        /// The bounding box.
        /// </summary>
        public const string FontBbox = "FontBBox";

        /// <summary>
        /// The version of the font.
        /// </summary>
        public const string Version = "Version";

        /// <summary>
        /// The notice.
        /// </summary>
        public const string Notice = "Notice";

        /// <summary>
        /// The encoding scheme.
        /// </summary>
        public const string EncodingScheme = "EncodingScheme";

        /// <summary>
        /// The mapping scheme.
        /// </summary>
        public const string MappingScheme = "MappingScheme";

        /// <summary>
        /// The escape character.
        /// </summary>
        public const string EscChar = "EscChar";

        /// <summary>
        /// The character set.
        /// </summary>
        public const string CharacterSet = "CharacterSet";

        /// <summary>
        /// The characters attribute.
        /// </summary>
        public const string Characters = "Characters";

        /// <summary>
        /// Whether this is a base font.
        /// </summary>
        public const string IsBaseFont = "IsBaseFont";

        /// <summary>
        /// The V Vector attribute.
        /// </summary>
        public const string VVector = "VVector";

        /// <summary>
        /// Whether V is fixed.
        /// </summary>
        public const string IsFixedV = "IsFixedV";

        /// <summary>
        /// The cap height.
        /// </summary>
        public const string CapHeight = "CapHeight";

        /// <summary>
        /// The X height.
        /// </summary>
        public const string XHeight = "XHeight";

        /// <summary>
        /// The ascender attribute.
        /// </summary>
        public const string Ascender = "Ascender";

        /// <summary>
        /// The descender attribute.
        /// </summary>
        public const string Descender = "Descender";

        /// <summary>
        /// The underline position.
        /// </summary>
        public const string UnderlinePosition = "UnderlinePosition";

        /// <summary>
        /// The underline thickness.
        /// </summary>
        public const string UnderlineThickness = "UnderlineThickness";

        /// <summary>
        /// The italic angle.
        /// </summary>
        public const string ItalicAngle = "ItalicAngle";

        /// <summary>
        /// The character width.
        /// </summary>
        public const string CharWidth = "CharWidth";

        /// <summary>
        /// Determines if fixed pitch.
        /// </summary>
        public const string IsFixedPitch = "IsFixedPitch";

        /// <summary>
        /// The start of the character metrics.
        /// </summary>
        public const string StartCharMetrics = "StartCharMetrics";

        /// <summary>
        /// The end of the character metrics.
        /// </summary>
        public const string EndCharMetrics = "EndCharMetrics";

        /// <summary>
        /// The character metrics c value.
        /// </summary>
        public const string CharmetricsC = "C";

        /// <summary>
        /// The character metrics value.
        /// </summary>
        public const string CharmetricsCh = "CH";

        /// <summary>
        /// The character metrics value.
        /// </summary>
        public const string CharmetricsWx = "WX";

        /// <summary>
        /// The character metrics value.
        /// </summary>
        public const string CharmetricsW0X = "W0X";

        /// <summary>
        /// The character metrics value.
        /// </summary>
        public const string CharmetricsW1X = "W1X";

        /// <summary>
        /// The character metrics value.
        /// </summary>
        public const string CharmetricsWy = "WY";

        /// <summary>
        /// The character metrics value.
        /// </summary>
        public const string CharmetricsW0Y = "W0Y";

        /// <summary>
        /// The character metrics value.
        /// </summary>
        public const string CharmetricsW1Y = "W1Y";

        /// <summary>
        /// The character metrics value.
        /// </summary>
        public const string CharmetricsW = "W";

        /// <summary>
        /// The character metrics value.
        /// </summary>
        public const string CharmetricsW0 = "W0";

        /// <summary>
        /// The character metrics value.
        /// </summary>
        public const string CharmetricsW1 = "W1";

        /// <summary>
        /// The character metrics value.
        /// </summary>
        public const string CharmetricsVv = "VV";

        /// <summary>
        /// The character metrics value.
        /// </summary>
        public const string CharmetricsN = "N";

        /// <summary>
        /// The character metrics value.
        /// </summary>
        public const string CharmetricsB = "B";

        /// <summary>
        /// The character metrics value.
        /// </summary>
        public const string CharmetricsL = "L";

        /// <summary>
        /// The character metrics value.
        /// </summary>
        public const string StdHw = "StdHW";

        /// <summary>
        /// The character metrics value.
        /// </summary>
        public const string StdVw = "StdVW";

        /// <summary>
        /// This is the start of the track kern data.
        /// </summary>
        public const string StartTrackKern = "StartTrackKern";

        /// <summary>
        /// This is the end of the track kern data.
        /// </summary>
        public const string EndTrackKern = "EndTrackKern";

        /// <summary>
        /// This is the start of the kern data.
        /// </summary>
        public const string StartKernData = "StartKernData";

        /// <summary>
        /// This is the end of the kern data.
        /// </summary>
        public const string EndKernData = "EndKernData";

        /// <summary>
        /// This is the start of the kern pairs data.
        /// </summary>
        public const string StartKernPairs = "StartKernPairs";

        /// <summary>
        /// This is the end of the kern pairs data.
        /// </summary>
        public const string EndKernPairs = "EndKernPairs";

        /// <summary>
        /// This is the start of the kern pairs data.
        /// </summary>
        public const string StartKernPairs0 = "StartKernPairs0";

        /// <summary>
        /// This is the start of the kern pairs data.
        /// </summary>
        public const string StartKernPairs1 = "StartKernPairs1";

        /// <summary>
        /// This is the start of the composite data section.
        /// </summary>
        public const string StartComposites = "StartComposites";

        /// <summary>
        /// This is the end of the composite data section.
        /// </summary>
        public const string EndComposites = "EndComposites";

        /// <summary>
        /// This is a composite character.
        /// </summary>
        public const string Cc = "CC";

        /// <summary>
        /// This is a composite character part.
        /// </summary>
        public const string Pcc = "PCC";

        /// <summary>
        /// This is a kern pair.
        /// </summary>
        public const string KernPairKp = "KP";

        /// <summary>
        /// This is a kern pair.
        /// </summary>
        public const string KernPairKph = "KPH";

        /// <summary>
        /// This is a kern pair.
        /// </summary>
        public const string KernPairKpx = "KPX";

        public const string KernPairKpy = "KPY";

        public FontMetrics Parse(IInputBytes bytes, bool useReducedDataSet)
        {
            var token = ReadString(bytes);

            if (!string.Equals(StartFontMetrics, token, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidFontFormatException($"The AFM file was not valid, it did not start with {StartFontMetrics}.");
            }

            var version = ReadDecimal(bytes);

            var builder = new FontMetricsBuilder(version);

            while ((token = ReadString(bytes)) != EndFontMetrics)
            {
                switch (token)
                {
                    case Comment:
                        builder.Comments.Add(ReadLine(bytes));
                        break;
                    case FontName:
                        builder.FontName = ReadLine(bytes);
                        break;
                    case FullName:
                        builder.FullName = ReadLine(bytes);
                        break;
                    case FamilyName:
                        builder.FamilyName = ReadLine(bytes);
                        break;
                }
            }

            return new FontMetrics();
        }

        private static decimal ReadDecimal(IInputBytes input)
        {
            var str = ReadString(input);

            return decimal.Parse(str);
        }

        private static bool ReadBool(IInputBytes input)
        {
            var boolean = ReadString(input);

            switch (boolean)
            {
                case "true":
                    return true;
                case "false":
                    return false;
                default:
                    throw new InvalidFontFormatException($"The AFM should have contained a boolean but instead contained: {boolean}.");
            }
        }

        private static readonly StringBuilder Builder = new StringBuilder();

        private static string ReadString(IInputBytes input)
        {
            Builder.Clear();

            if (input.IsAtEnd())
            {
                return EndFontMetrics;
            }

            while (ReadHelper.IsWhitespace(input.CurrentByte) && input.MoveNext())
            {
            }

            Builder.Append((char)input.CurrentByte);

            while (input.MoveNext() && !ReadHelper.IsWhitespace(input.CurrentByte))
            {
                Builder.Append((char)input.CurrentByte);
            }

            return Builder.ToString();
        }

        private static string ReadLine(IInputBytes input)
        {
            Builder.Clear();

            while (ReadHelper.IsWhitespace(input.CurrentByte) && input.MoveNext())
            {
            }

            Builder.Append((char)input.CurrentByte);

            while (input.MoveNext() && !ReadHelper.IsEndOfLine(input.CurrentByte))
            {
                Builder.Append((char)input.CurrentByte);
            }

            return Builder.ToString();
        }
    }

    internal interface IAdobeFontMetricsParser
    {
        FontMetrics Parse(IInputBytes bytes, bool useReducedDataSet);
    }
}
