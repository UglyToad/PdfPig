namespace UglyToad.Pdf.Fonts.Parser
{
    using System;
    using System.Collections.Generic;
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
                        case "usecmap":
                            throw new NotImplementedException("External CMap files not yet supported, please submit a pull request!");
                        case "begincodespacerange":
                            {
                                if (previousToken is NumericToken numeric)
                                {
                                    ParseCodespaceRange(numeric, scanner, builder);
                                }
                                else
                                {
                                    throw new InvalidOperationException("Unexpected token preceding start of codespace range: " + previousToken);
                                }

                            }
                            break;
                        case "beginbfchar":
                            {
                                if (previousToken is NumericToken numeric)
                                {
                                    ParseBaseFontCharacters(numeric, scanner, builder);
                                }
                                else
                                {
                                    throw new InvalidOperationException("Unexpected token preceding start of base font characters: " + previousToken);
                                }
                            }
                            break;
                        case "beginbfrange":
                            {
                                if (previousToken is NumericToken numeric)
                                {
                                    var parser = new BaseFontRangeParser();
                                    parser.Parse(numeric, scanner, builder);
                                }
                                else
                                {
                                    throw new InvalidOperationException("Unexpected token preceding start of base font character ranges: " + previousToken);
                                }
                            }
                            break;
                        case "begincidchar":
                            {
                                if (previousToken is NumericToken numeric)
                                {
                                    var characters = ParseCidCharacters(numeric, scanner);

                                    builder.CidCharacterMappings = characters;
                                }
                                else
                                {
                                    throw new InvalidOperationException("Unexpected token preceding start of Cid character mapping: " + previousToken);
                                }
                                break;
                            }
                        case "begincidrange":
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

        private static void ParseCodespaceRange(NumericToken count, ITokenScanner tokenScanner, CharacterMapBuilder builder)
        {
            /*
             * For example:
             3 begincodespacerange
                <00>    <80>
                <8140>  <9ffc>
                <a0>    <de>
             endcodespacerange
             */

            var ranges = new List<CodespaceRange>(count.Int);

            for (var i = 0; i < count.Int; i++)
            {
                if (!tokenScanner.MoveNext() || !(tokenScanner.CurrentToken is HexToken start))
                {
                    throw new InvalidOperationException("Codespace range contains an unexpected token: " + tokenScanner.CurrentToken);
                }

                if (!tokenScanner.MoveNext() || !(tokenScanner.CurrentToken is HexToken end))
                {
                    throw new InvalidOperationException("Codespace range contains an unexpected token: " + tokenScanner.CurrentToken);
                }

                ranges.Add(new CodespaceRange(start.Bytes, end.Bytes));
            }

            builder.CodespaceRanges = ranges;
        }

        private static void ParseBaseFontCharacters(NumericToken numeric, ITokenScanner tokenScanner, CharacterMapBuilder builder)
        {
            for (var i = 0; i < numeric.Int; i++)
            {
                if (!tokenScanner.MoveNext() || !(tokenScanner.CurrentToken is HexToken inputCode))
                {
                    throw new InvalidOperationException($"Base font characters definition contains invalid item at index {i}: {tokenScanner.CurrentToken}");
                }

                if (!tokenScanner.MoveNext())
                {
                    throw new InvalidOperationException($"Base font characters definition contains invalid item at index {i}: {tokenScanner.CurrentToken}");
                }

                if (tokenScanner.CurrentToken is NameToken characterName)
                {
                    builder.AddBaseFontCharacter(inputCode.Bytes, characterName.Data.Name);
                }
                else if (tokenScanner.CurrentToken is HexToken characterCode)
                {
                    builder.AddBaseFontCharacter(inputCode.Bytes, characterCode.Bytes);
                }
                else
                {
                    throw new InvalidOperationException($"Base font characters definition contains invalid item at index {i}: {tokenScanner.CurrentToken}");
                }
            }
        }

        private static IReadOnlyList<CidCharacterMapping> ParseCidCharacters(NumericToken numeric, ITokenScanner scanner)
        {
            var results = new List<CidCharacterMapping>();

            for (var i = 0; i < numeric.Int; i++)
            {
                if (!scanner.TryReadToken(out HexToken sourceCode))
                {
                    throw new InvalidOperationException("The first token in a line for Cid Characters should be a hex, instead it was: " + scanner.CurrentToken);
                }

                if (!scanner.TryReadToken(out NumericToken destinationCode))
                {
                    throw new InvalidOperationException("The destination token in a line for Cid Character should be an integer, instead it was: " + scanner.CurrentToken);
                }

                var sourceInteger = sourceCode.Bytes.ToInt(sourceCode.Bytes.Count);
                var mapping = new CidCharacterMapping(sourceInteger, destinationCode.Int);

                results.Add(mapping);
            }

            return results;
        }

        private static void ParseName(NameToken nameToken, ITokenScanner scanner, CharacterMapBuilder builder, bool isLenientParsing)
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
