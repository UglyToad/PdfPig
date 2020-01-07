namespace UglyToad.PdfPig.Parser
{
    using System.Collections.Generic;
    using Core;
    using Graphics;
    using Graphics.Operations;
    using Graphics.Operations.InlineImages;
    using Tokenization.Scanner;
    using Tokens;

    internal class PageContentParser : IPageContentParser
    {
        private readonly IGraphicsStateOperationFactory operationFactory;

        public PageContentParser(IGraphicsStateOperationFactory operationFactory)
        {
            this.operationFactory = operationFactory;
        }

        public IReadOnlyList<IGraphicsStateOperation> Parse(IInputBytes inputBytes)
        {
            var scanner = new CoreTokenScanner(inputBytes);

            var precedingTokens = new List<IToken>();
            var graphicsStateOperations = new List<IGraphicsStateOperation>();

            while (scanner.MoveNext())
            {
                var token = scanner.CurrentToken;

                if (token is InlineImageDataToken inlineImageData)
                {
                    var dictionary = new Dictionary<NameToken, IToken>();

                    for (var i = 0; i < precedingTokens.Count - 1; i++)
                    {
                        var t = precedingTokens[i];
                        if (!(t is NameToken n))
                        {
                            continue;
                        }

                        i++;

                        dictionary[n] = precedingTokens[i];
                    }

                    graphicsStateOperations.Add(new BeginInlineImageData(dictionary));
                    graphicsStateOperations.Add(new EndInlineImage(inlineImageData.Data));
                    precedingTokens.Clear();
                }
                else if (token is OperatorToken op)
                {
                    var operation = operationFactory.Create(op, precedingTokens);

                    if (operation != null)
                    {
                        graphicsStateOperations.Add(operation);
                    }

                    precedingTokens.Clear();
                }
                else if (token is CommentToken)
                {
                }
                else
                {
                    precedingTokens.Add(token);
                }
            }

            return graphicsStateOperations;
        }
    }
}
