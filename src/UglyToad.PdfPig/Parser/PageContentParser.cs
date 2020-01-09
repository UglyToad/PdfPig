namespace UglyToad.PdfPig.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

        public IReadOnlyList<IGraphicsStateOperation> Parse(int pageNumber, IInputBytes inputBytes)
        {
            var scanner = new CoreTokenScanner(inputBytes);

            var precedingTokens = new List<IToken>();
            var graphicsStateOperations = new List<IGraphicsStateOperation>();

            var lastEndImageOffset = new long?();

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

                    lastEndImageOffset = scanner.CurrentPosition - 2;

                    precedingTokens.Clear();
                }
                else if (token is OperatorToken op)
                {
                    // Handle an end image where the stream of image data contained EI but was not actually a real end image operator.
                    if (op.Data == "EI")
                    {
                        // Check an end image operation was the last thing that happened.
                        IGraphicsStateOperation lastOperation = graphicsStateOperations.Count > 0
                            ? graphicsStateOperations[graphicsStateOperations.Count - 1]
                            : null;

                        if (lastEndImageOffset == null || lastOperation == null || !(lastOperation is EndInlineImage lastEndImage))
                        {
                            throw new PdfDocumentFormatException("Encountered End Image token outside an inline image on " +
                                                                 $"page {pageNumber} at offset in content: {scanner.CurrentPosition}.");
                        }

                        // Work out how much data we missed between the false EI operator and the actual one.
                        var actualEndImageOffset = scanner.CurrentPosition - 3;

                        var gap = (int)(actualEndImageOffset - lastEndImageOffset);

                        var from = inputBytes.CurrentOffset;
                        inputBytes.Seek(lastEndImageOffset.Value);

                        // Recover the full image data.
                        {
                            var missingData = new byte[gap];
                            var read = inputBytes.Read(missingData);
                            if (read != gap)
                            {
                                throw new InvalidOperationException($"Failed to read expected buffer length {gap} on page {pageNumber} " +
                                                                    $"when reading inline image at offset in content: {lastEndImageOffset.Value}.");
                            }
                            
                            // Replace the last end image operator with one containing the full set of data.
                            graphicsStateOperations.Remove(lastEndImage);
                            graphicsStateOperations.Add(new EndInlineImage(lastEndImage.ImageData.Concat(missingData).ToArray()));
                        }

                        lastEndImageOffset = actualEndImageOffset;

                        inputBytes.Seek(from);
                    }
                    else
                    {
                        var operation = operationFactory.Create(op, precedingTokens);

                        if (operation != null)
                        {
                            graphicsStateOperations.Add(operation);
                        }
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
