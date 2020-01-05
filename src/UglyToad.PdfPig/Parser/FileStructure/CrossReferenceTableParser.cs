namespace UglyToad.PdfPig.Parser.FileStructure
{
    using System.Collections.Generic;
    using System.Linq;
    using CrossReference;
    using Core;
    using Parts.CrossReference;
    using Tokenization;
    using Tokenization.Scanner;
    using Tokens;

    internal class CrossReferenceTableParser
    {
        private const string InUseEntry = "n";
        private const string FreeEntry = "f";
        
        public CrossReferenceTablePart Parse(ISeekableTokenScanner scanner, long offset, bool isLenientParsing)
        {
            var builder = new CrossReferenceTablePartBuilder
            {
                Offset = offset,
                XRefType = CrossReferenceType.Table
            };

            if (scanner.CurrentPosition != offset)
            {
                scanner.Seek(offset);
            }

            scanner.MoveNext();

            if (scanner.CurrentToken is OperatorToken operatorToken)
            {
                if (operatorToken.Data == "xref")
                {
                    scanner.MoveNext();
                }
                else
                {
                    throw new PdfDocumentFormatException($"Unexpected operator in xref position: {operatorToken}.");
                }
            }

            if (scanner.CurrentToken is NumericToken firstObjectNumber)
            {
                if (!scanner.TryReadToken(out NumericToken objectCount))
                {
                    throw new PdfDocumentFormatException($"Unexpected token following xref and {firstObjectNumber}. We found: {scanner.CurrentToken}.");
                }

                var definition = new TableSubsectionDefinition(firstObjectNumber.Long, objectCount.Int);

                var tokenizer = new EndOfLineTokenizer();

                scanner.RegisterCustomTokenizer((byte)'\r', tokenizer);
                scanner.RegisterCustomTokenizer((byte)'\n', tokenizer);

                var readingLine = false;
                var tokens = new List<IToken>();
                var count = 0;
                while (scanner.MoveNext())
                {
                    if (scanner.CurrentToken is EndOfLineToken)
                    {
                        if (!readingLine)
                        {
                            continue;
                        }

                        readingLine = false;

                        count = ProcessTokens(tokens, scanner, builder, isLenientParsing, count, ref definition);
                        
                        tokens.Clear();

                        continue;
                    }

                    if (scanner.CurrentToken is CommentToken)
                    {
                        continue;
                    }

                    var isLineOperator = scanner.CurrentToken is OperatorToken op && (op.Data == FreeEntry || op.Data == InUseEntry);

                    if (!(scanner.CurrentToken is NumericToken) && !isLineOperator)
                    {
                        break;
                    }

                    readingLine = true;
                    tokens.Add(scanner.CurrentToken);
                }

                if (tokens.Count > 0)
                {
                    ProcessTokens(tokens, scanner, builder, isLenientParsing, count, ref definition);
                }

                scanner.DeregisterCustomTokenizer(tokenizer);
            }

            builder.Dictionary = ParseTrailer(scanner, isLenientParsing);

            return builder.Build();
        }

        private static int ProcessTokens(List<IToken> tokens, ISeekableTokenScanner scanner, CrossReferenceTablePartBuilder builder, bool isLenientParsing,
            int objectCount, ref TableSubsectionDefinition definition)
        {
            string GetErrorMessage()
            {
                var representation = "Invalid line format in xref table: [" + string.Join(", ", tokens.Select(x => x.ToString())) + "]";

                return representation;
            }

            if (objectCount == definition.Count)
            {
                if (tokens.Count == 2)
                {
                    if (tokens[0] is NumericToken newFirstObjectToken && tokens[1] is NumericToken newObjectCountToken)
                    {
                        definition = new TableSubsectionDefinition(newFirstObjectToken.Long, newObjectCountToken.Int);

                        return 0;
                    }
                }

                throw new PdfDocumentFormatException($"Found a line with 2 unexpected entries in the cross reference table: {tokens[0]}, {tokens[1]}.");
            }
            
            if (tokens.Count <= 2)
            {
                if (!isLenientParsing)
                {
                    throw new PdfDocumentFormatException(GetErrorMessage());
                }

                return objectCount;
            }

            var lastToken = tokens[tokens.Count - 1];

            if (lastToken is OperatorToken operatorToken)
            {
                if (operatorToken.Data == FreeEntry)
                {
                    return objectCount + 1;
                }

                if (operatorToken.Data != InUseEntry)
                {
                    if (!isLenientParsing)
                    {
                        throw new PdfDocumentFormatException(GetErrorMessage());
                    }

                    return objectCount;
                }

                if (tokens[0] is NumericToken offset && tokens[1] is NumericToken generationNumber)
                {
                    if (offset.Long >= builder.Offset && offset.Long <= scanner.CurrentPosition)
                    {
                        throw new PdfDocumentFormatException($"Object offset {offset} is within its own cross-reference table for object {definition.FirstNumber + objectCount}");
                    }

                    builder.Add(definition.FirstNumber + objectCount, generationNumber.Int, offset.Long);

                    return objectCount + 1;
                }
            }
            else
            {
                if (!isLenientParsing)
                {
                    throw new PdfDocumentFormatException(GetErrorMessage());
                }
            }

            return objectCount;
        }

        private static DictionaryToken ParseTrailer(ISeekableTokenScanner scanner, bool isLenientParsing)
        {
            if (scanner.CurrentToken is OperatorToken trailerToken && trailerToken.Data == "trailer")
            {
                if (!scanner.TryReadToken(out DictionaryToken trailerDictionary))
                {
                    throw new PdfDocumentFormatException($"Expected to find a dictionary in the trailer but instead found: {scanner.CurrentToken}.");
                }

                return trailerDictionary;
            }

            if (isLenientParsing)
            {
                var foundTrailer = false;
                while (scanner.MoveNext())
                {
                    if (scanner.CurrentToken is OperatorToken op && op.Data == "trailer")
                    {
                        foundTrailer = true;

                        break;
                    }
                }

                if (foundTrailer && scanner.TryReadToken(out DictionaryToken trailerDictionary))
                {
                    return trailerDictionary;
                }
            }

            throw new PdfDocumentFormatException("No trailer dictionary was present.");
        }
    }
}
