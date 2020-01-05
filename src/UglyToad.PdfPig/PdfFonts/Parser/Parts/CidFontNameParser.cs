namespace UglyToad.PdfPig.PdfFonts.Parser.Parts
{
    using System;
    using System.Globalization;
    using Cmap;
    using Tokenization.Scanner;
    using Tokens;

    internal class CidFontNameParser : ICidFontPartParser<NameToken>
    {
        public void Parse(NameToken nameToken, ITokenScanner scanner, CharacterMapBuilder builder,
            bool isLenientParsing)
        {
            switch (nameToken.Data)
            {
                case "WMode":
                    {
                        if (scanner.TryReadToken(out NumericToken numeric))
                        {
                            builder.WMode = numeric.Int;
                        }
                        break;
                    }
                case "CMapName":
                    {
                        if (scanner.TryReadToken(out NameToken name))
                        {
                            builder.Name = name.Data;
                        }
                        break;
                    }
                case "CMapVersion":
                    {
                        if (!scanner.MoveNext())
                        {
                            break;
                        }

                        var next = scanner.CurrentToken;
                        if (next is NumericToken number)
                        {
                            builder.Version = number.Data.ToString(NumberFormatInfo.InvariantInfo);
                        }
                        else if (next is StringToken stringToken)
                        {
                            builder.Version = stringToken.Data;
                        }
                        break;
                    }
                case "CMapType":
                    {
                        if (scanner.TryReadToken(out NumericToken numeric))
                        {
                            builder.Type = numeric.Int;
                        }
                        break;
                    }
                case "Registry":
                    {
                        if (scanner.TryReadToken(out StringToken stringToken))
                        {
                            builder.SystemInfoBuilder.Registry = stringToken.Data;
                        }
                        break;
                    }
                case "Ordering":
                    {
                        if (scanner.TryReadToken(out StringToken stringToken))
                        {
                            builder.SystemInfoBuilder.Ordering = stringToken.Data;
                        }
                        break;
                    }
                case "Supplement":
                    {
                        if (scanner.TryReadToken(out NumericToken numericToken))
                        {
                            builder.SystemInfoBuilder.Supplement = numericToken.Int;
                        }
                        break;
                    }
                case "CIDSystemInfo":
                    {
                        if (scanner.TryReadToken(out DictionaryToken dictionary))
                        {
                            builder.CharacterIdentifierSystemInfo = GetCharacterIdentifier(dictionary, isLenientParsing);
                        }
                        break;
                    }
            }
        }

        private static CharacterIdentifierSystemInfo GetCharacterIdentifier(DictionaryToken dictionary, bool isLenientParsing)
        {
            string GetErrorMessage(string missingKey)
            {
                return $"No {missingKey} found in the CIDSystemInfo dictionary: " + dictionary;
            }

            if (!dictionary.TryGet(NameToken.Registry, out var registry) || !(registry is StringToken registryString))
            {
                if (isLenientParsing)
                {
                    registryString = new StringToken("Adobe");
                }
                else
                {
                    throw new InvalidOperationException(GetErrorMessage("registry"));
                }
            }

            if (!dictionary.TryGet(NameToken.Ordering, out var ordering) || !(ordering is StringToken orderingString))
            {
                if (isLenientParsing)
                {
                    orderingString = new StringToken("");
                }
                else
                {
                    throw new InvalidOperationException(GetErrorMessage("ordering"));
                }
            }

            if (!dictionary.TryGet(NameToken.Supplement, out var supplement) || !(supplement is NumericToken supplementNumeric))
            {
                if (isLenientParsing)
                {
                    supplementNumeric = new NumericToken(0);
                }
                else
                {
                    throw new InvalidOperationException(GetErrorMessage("supplement"));
                }
            }

            return new CharacterIdentifierSystemInfo(registryString.Data, orderingString.Data, supplementNumeric.Int);
        }
    }
}
