namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Dictionaries
{
    using System;
    using System.Collections.Generic;
    using Core;

    internal class CompactFontFormatTopLevelDictionaryReader : CompactFontFormatDictionaryReader<CompactFontFormatTopLevelDictionary, CompactFontFormatTopLevelDictionary>
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
                                var registry = GetString(operands, stringIndex);
                                operands.RemoveAt(0);
                                var ordering = GetString(operands, stringIndex);
                                operands.RemoveAt(0);
                                var supplement = GetIntOrDefault(operands);
                                dictionary.CidFontOperators.Ros = new RegistryOrderingSupplement
                                {
                                    Registry = registry,
                                    Ordering = ordering,
                                    Supplement = supplement
                                };
                                dictionary.IsCidFont = true;
                                break;
                            case 31:
                                dictionary.CidFontOperators.Version = GetIntOrDefault(operands);
                                dictionary.IsCidFont = true;
                                break;
                            case 32:
                                dictionary.CidFontOperators.Revision = GetIntOrDefault(operands);
                                dictionary.IsCidFont = true;
                                break;
                            case 33:
                                dictionary.CidFontOperators.Type = GetIntOrDefault(operands);
                                dictionary.IsCidFont = true;
                                break;
                            case 34:
                                dictionary.CidFontOperators.Count = GetIntOrDefault(operands);
                                dictionary.IsCidFont = true;
                                break;
                            case 35:
                                dictionary.CidFontOperators.UidBase = GetIntOrDefault(operands);
                                dictionary.IsCidFont = true;
                                break;
                            case 36:
                                dictionary.CidFontOperators.FontDictionaryArray = GetIntOrDefault(operands);
                                dictionary.IsCidFont = true;
                                break;
                            case 37:
                                dictionary.CidFontOperators.FontDictionarySelect = operands[0].Decimal;
                                dictionary.IsCidFont = true;
                                break;
                            case 38:
                                dictionary.CidFontOperators.FontName = GetString(operands, stringIndex);
                                dictionary.IsCidFont = true;
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
                        dictionary.PrivateDictionaryLocation = new CompactFontFormatTopLevelDictionary.SizeAndOffset(size, offset);
                    }
                    break;
            }
        }
    }
}
