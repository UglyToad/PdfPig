namespace UglyToad.Pdf.Parser
{
    using System.Collections.Generic;
    using Graphics;
    using Graphics.Operations;
    using IO;
    using Tokenization.Scanner;
    using Tokenization.Tokens;

    internal class PageContentParser : IPageContentParser
    {
        public IReadOnlyList<IGraphicsStateOperation> Parse(IGraphicsStateOperationFactory operationFactory, IInputBytes inputBytes)
        {
            var scanner = new CoreTokenScanner(inputBytes);

            var precedingTokens = new List<IToken>();
            var graphicsStateOperations = new List<IGraphicsStateOperation>();

            while (scanner.MoveNext())
            {
                var token = scanner.CurrentToken;

                if (token is OperatorToken op)
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
