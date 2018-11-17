namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Dictionaries
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Geometry;

    internal abstract class CompactFontFormatDictionaryReader<TResult, TBuilder>
    {
        private readonly List<Operand> operands = new List<Operand>();

        public abstract TResult Read(CompactFontFormatData data, IReadOnlyList<string> stringIndex);

        protected TBuilder ReadDictionary(TBuilder builder, CompactFontFormatData data, IReadOnlyList<string> stringIndex)
        {
            while (data.CanRead())
            {
                operands.Clear();
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
                        var key = byte0 == 12 ? new OperandKey(byte0, data.ReadByte()) : new OperandKey(byte0); 

                        ApplyOperation(builder, operands, key, stringIndex);
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
                        operands.Add(new Operand(value));
                    }
                    else if (byte0 == 29)
                    {
                        var value = data.ReadByte() << 24 | data.ReadByte() << 16 |
                                    data.ReadByte() << 8 | data.ReadByte();
                        operands.Add(new Operand(value));
                    }
                    else if (byte0 == 30)
                    {
                        var realNumber = ReadRealNumber(data);
                        operands.Add(new Operand(realNumber));
                    }
                    else if (byte0 >= 32 && byte0 <= 246)
                    {
                        var value = byte0 - 139;
                        operands.Add(new Operand(value));
                    }
                    else if (byte0 >= 247 && byte0 <= 250)
                    {
                        var value = (byte0 - 247) * 256 + data.ReadByte() + 108;
                        operands.Add(new Operand(value));
                    }
                    else if (byte0 >= 251 && byte0 <= 254)
                    {
                        var value = -(byte0 - 251) * 256 - data.ReadByte() - 108;
                        operands.Add(new Operand(value));
                    }
                    else
                    {
                        throw new InvalidOperationException($"The first dictionary byte was not in the range 29 - 254. Got {byte0}.");
                    }
                }
            }

            return builder;
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

        protected abstract void ApplyOperation(TBuilder builder, List<Operand> operands, OperandKey operandKey, IReadOnlyList<string> stringIndex);

        protected static string GetString(List<Operand> operands, IReadOnlyList<string> stringIndex)
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
            if (stringIndexIndex >= 0 && stringIndexIndex < stringIndex.Count)
            {
                return stringIndex[stringIndexIndex];
            }

            return $"SID{index}";
        }

        protected static PdfRectangle GetBoundingBox(List<Operand> operands)
        {
            if (operands.Count != 4)
            {
                return new PdfRectangle();
            }

            return new PdfRectangle(operands[0].Decimal, operands[1].Decimal,
                operands[2].Decimal, operands[3].Decimal);
        }

        protected static decimal[] ToArray(List<Operand> operands)
        {
            var result = new decimal[operands.Count];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = operands[i].Decimal;
            }

            return result;
        }

        protected static int GetIntOrDefault(List<Operand> operands, int defaultValue = 0)
        {
            if (operands.Count == 0)
            {
                return defaultValue;
            }

            var first = operands[0];

            if (first.Int.HasValue)
            {
                return first.Int.Value;
            }

            return defaultValue;
        }

        protected static int[] ReadDeltaToIntArray(List<Operand> operands)
        {
            var results = new int[operands.Count];

            if (operands.Count == 0)
            {
                return results;
            }

            results[0] = (int)operands[0].Decimal;

            for (var i = 1; i < operands.Count; i++)
            {
                var previous = results[i - 1];
                var current = operands[i].Decimal;

                results[i] = (int)(previous + current);
            }

            return results;
        }

        protected static decimal[] ReadDeltaToArray(List<Operand> operands)
        {
            var results = new decimal[operands.Count];

            if (operands.Count == 0)
            {
                return results;
            }

            results[0] = operands[0].Decimal;

            for (var i = 1; i < operands.Count; i++)
            {
                var previous = results[i - 1];
                var current = operands[i].Decimal;

                results[i] = previous + current;
            }

            return results;
        }

        protected struct Operand
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

        protected struct OperandKey
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
}
