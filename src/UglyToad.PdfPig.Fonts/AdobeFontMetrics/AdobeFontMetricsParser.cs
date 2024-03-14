using System.Collections.Generic;

namespace UglyToad.PdfPig.Fonts.AdobeFontMetrics
{
    using System;
    using System.Globalization;
    using System.Text;
    using Core;
    using Fonts;

    /// <summary>
    /// Parses files in the Adobe Font Metrics (AFM) format.
    /// </summary>
    public static class AdobeFontMetricsParser
    {
        private static readonly object Locker = new object();
        private static readonly Dictionary<string, string> CharacterNames = new Dictionary<string, string>();

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

        /// <summary>
        /// This is a kern pair.
        /// </summary>
        public const string KernPairKpy = "KPY";

        private static readonly char[] IndividualCharmetricsSplit = [';'];

        private static readonly char[] CharmetricsKeySplit = [' '];
        
        /// <summary>
        /// Parse the font metrics from the input bytes.
        /// </summary>
        public static AdobeFontMetrics Parse(IInputBytes bytes, bool useReducedDataSet)
        {
            var stringBuilder = new StringBuilder();

            var token = ReadString(bytes, stringBuilder);

            if (!string.Equals(StartFontMetrics, token, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidFontFormatException($"The AFM file was not valid, it did not start with {StartFontMetrics}.");
            }

            var version = ReadDouble(bytes, stringBuilder);

            var builder = new AdobeFontMetricsBuilder(version);

            while ((token = ReadString(bytes, stringBuilder)) != EndFontMetrics)
            {
                switch (token)
                {
                    case Comment:
                        builder.Comments.Add(ReadLine(bytes, stringBuilder));
                        break;
                    case FontName:
                        builder.FontName = ReadLine(bytes, stringBuilder);
                        break;
                    case FullName:
                        builder.FullName = ReadLine(bytes, stringBuilder);
                        break;
                    case FamilyName:
                        builder.FamilyName = ReadLine(bytes, stringBuilder);
                        break;
                    case Weight:
                        builder.Weight = ReadLine(bytes, stringBuilder);
                        break;
                    case ItalicAngle:
                        builder.ItalicAngle = ReadDouble(bytes, stringBuilder);
                        break;
                    case IsFixedPitch:
                        builder.IsFixedPitch = ReadBool(bytes, stringBuilder);
                        break;
                    case FontBbox:
                        builder.SetBoundingBox(ReadDouble(bytes, stringBuilder), ReadDouble(bytes, stringBuilder),
                            ReadDouble(bytes, stringBuilder), ReadDouble(bytes, stringBuilder));
                        break;
                    case UnderlinePosition:
                        builder.UnderlinePosition = ReadDouble(bytes, stringBuilder);
                        break;
                    case UnderlineThickness:
                        builder.UnderlineThickness = ReadDouble(bytes, stringBuilder);
                        break;
                    case Version:
                        builder.Version = ReadLine(bytes, stringBuilder);
                        break;
                    case Notice:
                        builder.Notice = ReadLine(bytes, stringBuilder);
                        break;
                    case EncodingScheme:
                        builder.EncodingScheme = ReadLine(bytes, stringBuilder);
                        break;
                    case MappingScheme:
                        builder.MappingScheme = (int)ReadDouble(bytes, stringBuilder);
                        break;
                    case CharacterSet:
                        builder.CharacterSet = ReadLine(bytes, stringBuilder);
                        break;
                    case EscChar:
                        builder.EscapeCharacter = (int)ReadDouble(bytes, stringBuilder);
                        break;
                    case Characters:
                        builder.Characters = (int)ReadDouble(bytes, stringBuilder);
                        break;
                    case IsBaseFont:
                        builder.IsBaseFont = ReadBool(bytes, stringBuilder);
                        break;
                    case CapHeight:
                        builder.CapHeight = ReadDouble(bytes, stringBuilder);
                        break;
                    case XHeight:
                        builder.XHeight = ReadDouble(bytes, stringBuilder);
                        break;
                    case Ascender:
                        builder.Ascender = ReadDouble(bytes, stringBuilder);
                        break;
                    case Descender:
                        builder.Descender = ReadDouble(bytes, stringBuilder);
                        break;
                    case StdHw:
                        builder.StdHw = ReadDouble(bytes, stringBuilder);
                        break;
                    case StdVw:
                        builder.StdVw = ReadDouble(bytes, stringBuilder);
                        break;
                    case CharWidth:
                        builder.SetCharacterWidth(ReadDouble(bytes, stringBuilder), ReadDouble(bytes, stringBuilder));
                        break;
                    case VVector:
                        builder.SetVVector(ReadDouble(bytes, stringBuilder), ReadDouble(bytes, stringBuilder));
                        break;
                    case IsFixedV:
                        builder.IsFixedV = ReadBool(bytes, stringBuilder);
                        break;
                    case StartCharMetrics:
                        var count = (int)ReadDouble(bytes, stringBuilder);
                        for (var i = 0; i < count; i++)
                        {
                            var metric = ReadCharacterMetric(bytes, stringBuilder);
                            builder.CharacterMetrics.Add(metric);
                        }

                        var end = ReadString(bytes, stringBuilder);
                        if (end != EndCharMetrics)
                        {
                            throw new InvalidFontFormatException($"The character metrics section did not end with {EndCharMetrics} instead it was {end}.");
                        }

                        break;
                    case StartKernData:
                        break;
                }
            }

            return builder.Build();
        }

        private static double ReadDouble(IInputBytes input, StringBuilder stringBuilder)
        {
            return double.Parse(ReadString(input, stringBuilder), CultureInfo.InvariantCulture);
        }

        private static bool ReadBool(IInputBytes input, StringBuilder stringBuilder)
        {
            var boolean = ReadString(input, stringBuilder);

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

        private static string ReadString(IInputBytes input, StringBuilder stringBuilder)
        {
            stringBuilder.Clear();

            if (input.IsAtEnd())
            {
                return EndFontMetrics;
            }

            while (ReadHelper.IsWhitespace(input.CurrentByte) && input.MoveNext())
            {
            }

            stringBuilder.Append((char)input.CurrentByte);

            while (input.MoveNext() && !ReadHelper.IsWhitespace(input.CurrentByte))
            {
                stringBuilder.Append((char)input.CurrentByte);
            }

            return stringBuilder.ToString();
        }

        private static string ReadLine(IInputBytes input, StringBuilder stringBuilder)
        {
            stringBuilder.Clear();

            while (ReadHelper.IsWhitespace(input.CurrentByte) && input.MoveNext())
            {
            }

            stringBuilder.Append((char)input.CurrentByte);

            while (input.MoveNext() && !ReadHelper.IsEndOfLine(input.CurrentByte))
            {
                stringBuilder.Append((char)input.CurrentByte);
            }

            return stringBuilder.ToString();
        }

        private static AdobeFontMetricsIndividualCharacterMetric ReadCharacterMetric(IInputBytes bytes, StringBuilder stringBuilder)
        {
            var line = ReadLine(bytes, stringBuilder);

            var split = line.Split(IndividualCharmetricsSplit, StringSplitOptions.RemoveEmptyEntries);

            var metric = new AdobeFontMetricsIndividualCharacterMetricBuilder();

            foreach (var s in split)
            {
                var parts = s.Split(CharmetricsKeySplit, StringSplitOptions.RemoveEmptyEntries);

                switch (parts[0])
                {
                    case CharmetricsC:
                        {
                            var code = int.Parse(parts[1], CultureInfo.InvariantCulture);
                            metric.CharacterCode = code;
                            break;
                        }
                    case CharmetricsCh:
                        {
                            var code = int.Parse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                            metric.CharacterCode = code;
                            break;
                        }
                    case CharmetricsWx:
                        {
                            metric.WidthX = double.Parse(parts[1], CultureInfo.InvariantCulture);
                            break;
                        }
                    case CharmetricsW0X:
                        {
                            metric.WidthXDirection0 = double.Parse(parts[1], CultureInfo.InvariantCulture);
                            break;
                        }
                    case CharmetricsW1X:
                        {
                            metric.WidthXDirection1 = double.Parse(parts[1], CultureInfo.InvariantCulture);
                            break;
                        }
                    case CharmetricsWy:
                        {
                            metric.WidthY = double.Parse(parts[1], CultureInfo.InvariantCulture);
                            break;
                        }
                    case CharmetricsW0Y:
                        {
                            metric.WidthYDirection0 = double.Parse(parts[1], CultureInfo.InvariantCulture);
                            break;
                        }
                    case CharmetricsW1Y:
                        {
                            metric.WidthYDirection1 = double.Parse(parts[1], CultureInfo.InvariantCulture);
                            break;
                        }
                    case CharmetricsW:
                        {
                            metric.WidthX = double.Parse(parts[1], CultureInfo.InvariantCulture);
                            metric.WidthY = double.Parse(parts[2], CultureInfo.InvariantCulture);
                            break;
                        }
                    case CharmetricsW0:
                        {
                            metric.WidthXDirection0 = double.Parse(parts[1], CultureInfo.InvariantCulture);
                            metric.WidthYDirection0 = double.Parse(parts[2], CultureInfo.InvariantCulture);
                            break;
                        }
                    case CharmetricsW1:
                        {
                            metric.WidthXDirection1 = double.Parse(parts[1], CultureInfo.InvariantCulture);
                            metric.WidthYDirection1 = double.Parse(parts[2], CultureInfo.InvariantCulture);
                            break;
                        }
                    case CharmetricsVv:
                        {
                            metric.VVector = new AdobeFontMetricsVector(double.Parse(parts[1], CultureInfo.InvariantCulture), 
                                double.Parse(parts[2], CultureInfo.InvariantCulture));
                            break;
                        }
                    case CharmetricsN:
                        {
                            lock (Locker)
                            {
                                var name = parts[1];

                                if (!CharacterNames.TryGetValue(name, out var cached))
                                {
                                    cached = name;
                                    CharacterNames[name] = cached;
                                }

                                metric.Name = cached;
                            }
                            break;
                        }
                    case CharmetricsB:
                        {
                            metric.BoundingBox = new PdfRectangle(double.Parse(parts[1], CultureInfo.InvariantCulture),
                                double.Parse(parts[2], CultureInfo.InvariantCulture),
                                double.Parse(parts[3], CultureInfo.InvariantCulture),
                                double.Parse(parts[4], CultureInfo.InvariantCulture));
                            break;
                        }
                    case CharmetricsL:
                        {
                            metric.Ligature = new AdobeFontMetricsLigature(parts[1], parts[2]);
                            break;
                        }
                    default:
                        throw new InvalidFontFormatException($"Unknown CharMetrics command '{parts[0]}'.");
                }
            }

            return metric.Build();
        }
    }
}
