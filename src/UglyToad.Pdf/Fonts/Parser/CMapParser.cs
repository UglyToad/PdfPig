namespace UglyToad.Pdf.Fonts.Parser
{
    using System;
    using System.Globalization;
    using Cmap;
    using Cos;
    using IO;
    using Tokenization.Scanner;
    using Tokenization.Tokens;
    using Util.JetBrains.Annotations;

    public class CMapParser
    {
        public CMap Parse(IInputBytes inputBytes, bool isLenientParsing)
        {
            var scanner = new CoreTokenScanner(inputBytes);

            var builder = new CharacterMapBuilder();
            var result = new CMap();

            IToken previousToken = null;
            while (scanner.MoveNext())
            {
                var token = scanner.CurrentToken;

                if (token is OperatorToken operatorToken)
                {
                    switch (operatorToken.Data)
                    {
                        default:
                            break;
                    }
                }
                else if (token is NameToken name)
                {
                    ParseName(name, scanner, builder, isLenientParsing);
                }

                previousToken = token;
            }

            return null;
        }

        private static void ParseName(NameToken nameToken, CoreTokenScanner scanner, CharacterMapBuilder builder, bool isLenientParsing)
        {
            switch (nameToken.Data.Name)
            {
                case "WMode":
                    {
                        var next = TryMoveNext(scanner);
                        if (next is NumericToken numeric)
                        {
                            builder.WMode = numeric.Int;
                        }
                        break;
                    }
                case "CMapName":
                    {
                        var next = TryMoveNext(scanner);
                        if (next is NameToken name)
                        {
                            builder.Name = name.Data.Name;
                        }
                        break;
                    }
                case "CMapVersion":
                    {
                        var next = TryMoveNext(scanner);
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
                        var next = TryMoveNext(scanner);
                        if (next is NumericToken numeric)
                        {
                            builder.Type = numeric.Int;
                        }
                        break;
                    }
                case "Registry":
                    {
                        throw new NotImplementedException("Registry should be in a dictionary");
                    }
                case "Ordering":
                    {
                        throw new NotImplementedException("Ordering should be in a dictionary");
                    }
                case "Supplement":
                    {
                        throw new NotImplementedException("Supplement should be in a dictionary");
                    }
                case "CIDSystemInfo":
                    {
                        var next = TryMoveNext(scanner);

                        if (next is DictionaryToken dictionary)
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

            if (!dictionary.TryGetByName(CosName.REGISTRY, out var registry) || !(registry is StringToken registryString))
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

            if (!dictionary.TryGetByName(CosName.ORDERING, out var ordering) || !(ordering is StringToken orderingString))
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

            if (!dictionary.TryGetByName(CosName.SUPPLEMENT, out var supplement) || !(supplement is NumericToken supplementNumeric))
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

        [CanBeNull]
        private static IToken TryMoveNext(ITokenScanner scanner)
        {
            if (!scanner.MoveNext())
            {
                return null;
            }

            return scanner.CurrentToken;
        }
    }
}
