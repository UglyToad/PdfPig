namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Core;
    using Geometry;

    internal class CompactFontFormatIndividualFontParser
    {
        public void Parse(CompactFontFormatData data, string name, byte[] topDictionaryIndex, string[] stringIndex)
        {
            var individualData = new CompactFontFormatData(topDictionaryIndex);

            var dictionary = ReadTopLevelDictionary(individualData, stringIndex);
        }

        private static CompactFontFormatFontDictionary ReadTopLevelDictionary(CompactFontFormatData data, string[] stringIndex)
        {
            var dictionary = new CompactFontFormatFontDictionary();
            while (data.CanRead())
            {
                var numbers = new List<Operand>();

                var infiniteLoopProtection = 0;
                while (true)
                {
                    infiniteLoopProtection++;
                    // Avoid the library getting caught in an infinite loop, probably not possible.
                    // "An operator may be preceded by up to a maximum of 48 operands."
                    if (infiniteLoopProtection > 256)
                    {
                        throw new InvalidOperationException("Got caught in an infinite loop trying to read a CFF dictionary.");
                    }

                    var byte0 = data.ReadByte();

                    // Operands and operators are distinguished by the first byte, 0 - 21 specify operators
                    if (byte0 <= 21)
                    {
                        ApplyOperator(byte0, numbers, data, stringIndex, dictionary);
                        break;
                    }

                    /*
                     * b0 value     value range         calculation
                     *  32 - 246      -107 - +107       b0 - 139
                     * 247 - 250      +108 - +1131      (b0 - 247)*256 + b1 + 108
                     * 251 - 254     -1131 - -108       -(b0 - 251)*256 - b1 - 108
                     *  28          -32768 - +32767     b1 << 8 | b2
                     *  29          -(2^31)-+(2^31-1)   b1 << 24 | b2 << 16 | b3 << 8 | b4
                     *
                     * A byte value of 30 defines a real number operand
                     */
                    if (byte0 == 28)
                    {
                        var value = data.ReadByte() << 8 | data.ReadByte();
                        numbers.Add(new Operand(value));
                    }
                    else if (byte0 == 29)
                    {
                        var value = data.ReadByte() << 24 | data.ReadByte() << 16 |
                                    data.ReadByte() << 8 | data.ReadByte();
                        numbers.Add(new Operand(value));
                    }
                    else if (byte0 == 30)
                    {
                        var realNumber = ReadRealNumber(data);
                        numbers.Add(new Operand(realNumber));
                    }
                    else if (byte0 >= 32 && byte0 <= 246)
                    {
                        var value = byte0 - 139;
                        numbers.Add(new Operand(value));
                    }
                    else if (byte0 >= 247 && byte0 <= 250)
                    {
                        var value = (byte0 - 247) * 256 + data.ReadByte() + 108;
                        numbers.Add(new Operand(value));
                    }
                    else if (byte0 >= 251 && byte0 <= 254)
                    {
                        var value = -(byte0 - 251) * 256 - data.ReadByte() - 108;
                        numbers.Add(new Operand(value));
                    }
                    else
                    {
                        throw new InvalidOperationException($"The first dictionary byte was not in the range 29 - 254. Got {byte0}.");
                    }
                }
            }

            return dictionary;
        }

        private static decimal ReadRealNumber(CompactFontFormatData data)
        {
            var sb = new StringBuilder();
            var done = false;
            var exponentMissing = false;

            while (!done)
            {
                var b = data.ReadByte();
                var nibble1 = b / 16;
                var nibble2 = b % 16;

                for (var i = 0; i < 2; i++)
                {
                    var nibble = i == 0 ? nibble1 : nibble2;

                    switch (nibble)
                    {
                        case 0x0:
                        case 0x1:
                        case 0x2:
                        case 0x3:
                        case 0x4:
                        case 0x5:
                        case 0x6:
                        case 0x7:
                        case 0x8:
                        case 0x9:
                            sb.Append(nibble);
                            exponentMissing = false;
                            break;
                        case 0xa:
                            sb.Append(".");
                            break;
                        case 0xb:
                            sb.Append("E");
                            exponentMissing = true;
                            break;
                        case 0xc:
                            sb.Append("E-");
                            exponentMissing = true;
                            break;
                        case 0xd:
                            break;
                        case 0xe:
                            sb.Append("-");
                            break;
                        case 0xf:
                            done = true;
                            break;
                        default:
                            throw new InvalidOperationException($"Did not expect nibble value: {nibble}.");
                    }
                }
            }

            if (exponentMissing)
            {
                // the exponent is missing, just append "0" to avoid an exception
                // not sure if 0 is the correct value, but it seems to fit
                // see PDFBOX-1522
                sb.Append("0");
            }

            if (sb.Length == 0)
            {
                return 0m;
            }

            return decimal.Parse(sb.ToString());
        }

        private static void ApplyOperator(byte byte0, List<Operand> operands, CompactFontFormatData data,
            string[] stringIndex,
            CompactFontFormatFontDictionary dictionary)
        {

            OperandKey key;
            if (byte0 == 12)
            {
                var b1 = data.ReadByte();
                key = new OperandKey(byte0, b1);
            }
            else
            {
                key = new OperandKey(byte0);
            }

            switch (key.Byte0)
            {
                case 0:
                    dictionary.Version = GetString(operands, stringIndex);
                    break;
                case 1:
                    dictionary.Notice = GetString(operands, stringIndex);
                    break;
                case 2:
                    dictionary.FullName = GetString(operands, stringIndex);
                    break;
                case 3:
                    dictionary.FamilyName = GetString(operands, stringIndex);
                    break;
                case 4:
                    dictionary.Weight = GetString(operands, stringIndex);
                    break;
                case 5:
                    dictionary.FontBoundingBox = GetBoundingBox(operands);
                    break;
                case 12:
                    {
                        if (!key.Byte1.HasValue)
                        {
                            throw new InvalidOperationException("A single byte sequence beginning with 12 was found.");
                        }

                        switch (key.Byte1.Value)
                        {
                            case 1:
                                dictionary.IsFixedPitch = operands[0].Decimal == 1;
                                break;
                            case 2:
                                dictionary.ItalicAngle = operands[0].Decimal;
                                break;
                            case 3:
                                dictionary.UnderlinePosition = operands[0].Decimal;
                                break;
                            case 4:
                                dictionary.UnderlineThickness = operands[0].Decimal;
                                break;
                            case 5:
                                dictionary.PaintType = operands[0].Decimal;
                                break;
                            case 6:
                                dictionary.CharstringType = operands[0].Int.Value;
                                break;
                            case 7:
                                break;
                            case 8:
                                break;
                        }
                    }
                    break;
                case 13:
                    dictionary.UniqueId = operands.Count > 0 ? operands[0].Decimal : 0;
                    break;
                case 14:
                    dictionary.Xuid = ToArray(operands);
                    break;
                case 15:
                    break;
                case 16:
                    break;
                case 17:
                    break;
                case 18:
                    break;
            }
        }

        private static string GetString(List<Operand> operands, string[] stringIndex)
        {
            if (operands.Count == 0)
            {
                throw new InvalidOperationException("Cannot read a string from an empty operands array.");
            }

            if (!operands[0].Int.HasValue)
            {
                throw new InvalidOperationException($"The first operand for reading a string was not an integer. Got: {operands[0].Decimal}");
            }

            var index = operands[0].Int.Value;

            if (index >= 0 && index <= 390)
            {
                return CompactFontFormatStandardStrings.GetName(index);
            }

            var stringIndexIndex = index - 391;
            if (stringIndexIndex >= 0 && stringIndexIndex < stringIndex.Length)
            {
                return stringIndex[stringIndexIndex];
            }

            return $"SID{index}";
        }

        private static PdfRectangle GetBoundingBox(List<Operand> operands)
        {
            if (operands.Count != 4)
            {
                return new PdfRectangle();
            }

            return new PdfRectangle(operands[0].Decimal, operands[1].Decimal,
                operands[2].Decimal, operands[3].Decimal);
        }

        private static decimal[] ToArray(List<Operand> operands)
        {
            var result = new decimal[operands.Count];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = operands[i].Decimal;
            }

            return result;
        }

        private struct Operand
        {
            public int? Int { get; }

            public decimal Decimal { get; }

            public Operand(int integer)
            {
                Int = integer;
                Decimal = integer;
            }

            public Operand(decimal d)
            {
                Int = null;
                Decimal = d;
            }
        }

        private struct OperandKey
        {
            public byte Byte0 { get; }

            public byte? Byte1 { get; }

            public OperandKey(Byte byte0)
            {
                Byte0 = byte0;
                Byte1 = null;
            }

            public OperandKey(byte byte0, byte byte1)
            {
                Byte0 = byte0;
                Byte1 = byte1;
            }
        }
    }

    internal class CompactFontFormatFontDictionary
    {
        public string Version { get; set; }

        public string Notice { get; set; }

        public string Copyright { get; set; }

        public string FullName { get; set; }

        public string FamilyName { get; set; }

        public string Weight { get; set; }

        public bool IsFixedPitch { get; set; }

        public decimal ItalicAngle { get; set; }

        public decimal UnderlinePosition { get; set; } = -100;

        public decimal UnderlineThickness { get; set; } = 50;

        public decimal PaintType { get; set; }

        public int CharstringType { get; set; }

        public TransformationMatrix FontMatrix { get; set; } = TransformationMatrix.FromValues(0.001m, 0m, 0.001m, 0, 0, 0);

        public decimal UniqueId { get; set; }

        public PdfRectangle FontBoundingBox { get; set; } = new PdfRectangle(0, 0, 0, 0);

        public decimal[] Xuid { get; set; }


    }
}
