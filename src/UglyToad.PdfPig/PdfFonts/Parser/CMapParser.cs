namespace UglyToad.PdfPig.PdfFonts.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using Cmap;
    using Core;
    using Parts;
    using Tokenization.Scanner;
    using Tokens;

    internal class CMapParser
    {
        private static readonly BaseFontRangeParser BaseFontRangeParser = new BaseFontRangeParser();
        private static readonly BaseFontCharacterParser BaseFontCharacterParser = new BaseFontCharacterParser();
        private static readonly CidRangeParser CidRangeParser = new CidRangeParser();
        private static readonly CidFontNameParser CidFontNameParser = new CidFontNameParser();
        private static readonly CodespaceRangeParser CodespaceRangeParser = new CodespaceRangeParser();
        private static readonly CidCharacterParser CidCharacterParser = new CidCharacterParser();

        public CMap Parse(IInputBytes inputBytes)
        {
            var scanner = new CoreTokenScanner(inputBytes,
                false,
                namedDictionaryRequiredKeys: new Dictionary<NameToken, IReadOnlyList<NameToken>>
                {
                    { NameToken.CidSystemInfo, new[] { NameToken.Registry, NameToken.Ordering, NameToken.Supplement } }
                });

            var builder = new CharacterMapBuilder();

            IToken? previousToken = null;
            while (scanner.MoveNext())
            {
                var token = scanner.CurrentToken;

                if (token is OperatorToken operatorToken)
                {
                    switch (operatorToken.Data)
                    {
                        case "usecmap":
                            {
                                if (previousToken is NameToken name && TryParseExternal(name.Data, out var external))
                                {
                                    builder.UseCMap(external);
                                }
                                else
                                {
                                    throw new InvalidOperationException("Unexpected token preceding external cmap call: " + previousToken);
                                }
                                break;
                            }
                        case "begincodespacerange":
                            {
                                if (previousToken is NumericToken numeric)
                                {
                                    CodespaceRangeParser.Parse(numeric, scanner, builder);
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
                                    BaseFontCharacterParser.Parse(numeric, scanner, builder);
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
                                    BaseFontRangeParser.Parse(numeric, scanner, builder);
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
                                    CidCharacterParser.Parse(numeric, scanner, builder);
                                }
                                else
                                {
                                    throw new InvalidOperationException("Unexpected token preceding start of Cid character mapping: " + previousToken);
                                }
                                break;
                            }
                        case "begincidrange":
                            {
                                if (previousToken is NumericToken numeric)
                                {
                                    CidRangeParser.Parse(numeric, scanner, builder);
                                }
                                else
                                {
                                    throw new InvalidOperationException("Unexpected token preceding start of Cid ranges: " + previousToken);
                                }
                            }
                            break;
                    }
                }
                else if (token is NameToken name)
                {
                    CidFontNameParser.Parse(name, scanner, builder);
                }

                previousToken = token;
            }

            return builder.Build();
        }

        public bool TryParseExternal(string name, [NotNullWhen(true)] out CMap? result)
        {
            result = null;

            var resources = typeof(CMapParser).Assembly.GetManifestResourceNames();

            var resource = resources.FirstOrDefault(x =>
                x.EndsWith("CMap." + name, StringComparison.InvariantCultureIgnoreCase));

            if (resource is null)
            {
                return false;
            }

            byte[] bytes;
            using (var stream = typeof(CMapParser).Assembly.GetManifestResourceStream(resource))
            {
                if (stream is null)
                {
                    return false;
                }

                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);

                    bytes = memoryStream.ToArray();
                }
            }

            result = Parse(new ByteArrayInputBytes(bytes));

            return true;
        }
    }
}
