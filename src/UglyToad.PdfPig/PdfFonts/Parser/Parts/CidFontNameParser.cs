namespace UglyToad.PdfPig.PdfFonts.Parser.Parts
{
    using System.Globalization;
    using Cmap;
    using Tokenization.Scanner;
    using Tokens;

    internal class CidFontNameParser : ICidFontPartParser<NameToken>
    {
        public void Parse(NameToken nameToken, ITokenScanner scanner, CharacterMapBuilder builder)
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
                            builder.CharacterIdentifierSystemInfo = GetCharacterIdentifier(dictionary);
                        }
                        break;
                    }
            }
        }

        private static CharacterIdentifierSystemInfo GetCharacterIdentifier(DictionaryToken dictionary)
        {
            if (!dictionary.TryGet(NameToken.Registry, out var registry) || !(registry is StringToken registryString))
            {
                registryString = new StringToken("Adobe");
            }

            if (!dictionary.TryGet(NameToken.Ordering, out var ordering) || !(ordering is StringToken orderingString))
            {
                orderingString = new StringToken(string.Empty);
            }

            if (!dictionary.TryGet(NameToken.Supplement, out var supplement) || !(supplement is NumericToken supplementNumeric))
            {
                supplementNumeric = new NumericToken(0);
            }

            return new CharacterIdentifierSystemInfo(registryString.Data, orderingString.Data, supplementNumeric.Int);
        }
    }
}
