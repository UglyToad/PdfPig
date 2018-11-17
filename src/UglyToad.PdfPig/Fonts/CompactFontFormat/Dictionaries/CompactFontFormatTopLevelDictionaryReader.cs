namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Dictionaries
{
    using System;
    using System.Collections.Generic;
    using Core;

    internal class CompactFontFormatTopLevelDictionaryReader : CompactFontFormatDictionaryReader<CompactFontFormatTopLevelDictionary>
    {
        public override CompactFontFormatTopLevelDictionary Read(CompactFontFormatData data, IReadOnlyList<string> stringIndex)
        {
            var dictionary = new CompactFontFormatTopLevelDictionary();

            ReadDictionary(dictionary, data, stringIndex);

            return dictionary;
        }

        protected override void ApplyOperation(CompactFontFormatTopLevelDictionary dictionary, List<Operand> operands, OperandKey key, IReadOnlyList<string> stringIndex)
        {
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
                            case 0:
                                dictionary.Copyright = GetString(operands, stringIndex);
                                break;
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
                                dictionary.CharStringType = (CompactFontFormatCharStringType)GetIntOrDefault(operands, 2);
                                break;
                            case 7:
                                {
                                    var array = ToArray(operands);

                                    if (array.Length != 4)
                                    {
                                        throw new InvalidOperationException($"Expected four values for the font matrix, instead got: {array}.");
                                    }

                                    dictionary.FontMatrix = TransformationMatrix.FromArray(array);
                                }
                                break;
                            case 8:
                                dictionary.StrokeWidth = operands[0].Decimal;
                                break;
                            case 20:
                                dictionary.SyntheticBaseFontIndex = GetIntOrDefault(operands);
                                break;
                            case 21:
                                dictionary.PostScript = GetString(operands, stringIndex);
                                break;
                            case 22:
                                dictionary.BaseFontName = GetString(operands, stringIndex);
                                break;
                            case 23:
                                dictionary.BaseFontBlend = ReadDeltaToArray(operands);
                                break;
                            // TODO: CID Font Stuff
                            case 30:
                                break;
                            case 31:
                                break;
                            case 32:
                                break;
                            case 33:
                                break;
                            case 34:
                                break;
                            case 35:
                                break;
                            case 36:
                                break;
                            case 37:
                                break;
                            case 38:
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
                    dictionary.CharSetOffset = GetIntOrDefault(operands);
                    break;
                case 16:
                    dictionary.EncodingOffset = GetIntOrDefault(operands);
                    break;
                case 17:
                    dictionary.CharStringsOffset = GetIntOrDefault(operands);
                    break;
                case 18:
                    {
                        var size = GetIntOrDefault(operands);
                        operands.RemoveAt(0);
                        var offset = GetIntOrDefault(operands);
                        dictionary.PrivateDictionarySizeAndOffset = Tuple.Create(size, offset);
                    }
                    break;
            }
        }
    }
}
