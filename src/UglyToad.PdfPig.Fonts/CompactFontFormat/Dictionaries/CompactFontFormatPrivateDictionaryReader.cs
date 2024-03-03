namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Dictionaries
{
    using System;
    using System.Collections.Generic;

    internal class CompactFontFormatPrivateDictionaryReader : CompactFontFormatDictionaryReader<CompactFontFormatPrivateDictionary, CompactFontFormatPrivateDictionary.Builder>
    {
        public override CompactFontFormatPrivateDictionary Read(CompactFontFormatData data, IReadOnlyList<string> stringIndex)
        {
            var builder = new CompactFontFormatPrivateDictionary.Builder();

            ReadDictionary(builder, data, stringIndex);

            return builder.Build();
        }

        protected override void ApplyOperation(CompactFontFormatPrivateDictionary.Builder dictionary, List<Operand> operands, OperandKey operandKey, IReadOnlyList<string> stringIndex)
        {
            switch (operandKey.Byte0)
            {
                case 6:
                    dictionary.BlueValues = ReadDeltaToIntArray(operands);
                    break;
                case 7:
                    dictionary.OtherBlues = ReadDeltaToIntArray(operands);
                    break;
                case 8:
                    dictionary.FamilyBlues = ReadDeltaToIntArray(operands);
                    break;
                case 9:
                    dictionary.FamilyOtherBlues = ReadDeltaToIntArray(operands);
                    break;
                case 10:
                    dictionary.StandardHorizontalWidth = operands[0].Double;
                    break;
                case 11:
                    dictionary.StandardVerticalWidth = operands[0].Double;
                    break;
                case 12:
                {
                    if (!operandKey.Byte1.HasValue)
                    {
                        throw new InvalidOperationException("In the CFF private dictionary, got the operation key 12 without a second byte.");
                    }

                    switch (operandKey.Byte1.Value)
                    {
                        case 9:
                            dictionary.BlueScale = operands[0].Double;
                            break;
                        case 10:
                            dictionary.BlueShift = operands[0].Int;
                            break;
                        case 11:
                            dictionary.BlueFuzz = operands[0].Int;
                            break;
                        case 12:
                            dictionary.StemSnapHorizontalWidths = ReadDeltaToArray(operands);
                            break;
                        case 13:
                            dictionary.StemSnapVerticalWidths = ReadDeltaToArray(operands);
                            break;
                        case 14:
                            dictionary.ForceBold = operands[0].Double == 1;
                            break;
                        case 17:
                            dictionary.LanguageGroup = operands[0].Int;
                            break;
                        case 18:
                            dictionary.ExpansionFactor = operands[0].Double;
                            break;
                        case 19:
                            dictionary.InitialRandomSeed = operands[0].Double;
                            break;
                    }
                }
                    break;
                case 19:
                    dictionary.LocalSubroutineOffset = GetIntOrDefault(operands, -1);
                    break;
                case 20:
                    dictionary.DefaultWidthX = operands[0].Double;
                    break;
                case 21:
                    dictionary.NominalWidthX = operands[0].Double;
                    break;
            }
        }
    }
}