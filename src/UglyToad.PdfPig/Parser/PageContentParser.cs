namespace UglyToad.PdfPig.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Graphics;
    using Graphics.Operations;
    using Graphics.Operations.InlineImages;
    using Logging;
    using Tokenization.Scanner;
    using Tokens;

    internal class PageContentParser : IPageContentParser
    {
        private readonly IGraphicsStateOperationFactory operationFactory;

        public PageContentParser(IGraphicsStateOperationFactory operationFactory)
        {
            this.operationFactory = operationFactory;
        }

        public IReadOnlyList<IGraphicsStateOperation> Parse(int pageNumber, IInputBytes inputBytes,
            ILog log)
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

                        log.Warn($"End inline image (EI) encountered after previous EI, attempting recovery at {actualEndImageOffset}.");

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
                        IGraphicsStateOperation operation;
                        try
                        {
                            operation = operationFactory.Create(op, precedingTokens);
                        }
                        catch (Exception ex)
                        {
                            var lastWasEndImage = graphicsStateOperations.Count > 0
                                                  && graphicsStateOperations[graphicsStateOperations.Count - 1] is EndInlineImage;

                            // End images can cause weird state if the "EI" appears inside the inline data stream.
                            if (lastWasEndImage)
                            {
                                log.Error($"Failed reading an operation at offset {inputBytes.CurrentOffset} for page {pageNumber}.", ex);
                                operation = null;
                            }
                            else
                            {
                                throw;
                            }
                        }

                        if (operation != null)
                        {
                            graphicsStateOperations.Add(operation);
                        }
                        else if (graphicsStateOperations.Count > 0)
                        {
                            var lastToken = graphicsStateOperations[graphicsStateOperations.Count - 1];

                            if (lastToken is EndInlineImage prevEndInlineImage && lastEndImageOffset.HasValue)
                            {
                                log.Warn($"Operator {op.Data} was not understood following end of inline image data at {lastEndImageOffset}, " +
                                         "attempting recovery.");

                                var nextByteSet = scanner.RecoverFromIncorrectEndImage(lastEndImageOffset.Value);
                                graphicsStateOperations.RemoveAt(graphicsStateOperations.Count - 1);
                                var newEndInlineImage = new EndInlineImage(prevEndInlineImage.ImageData.Concat(nextByteSet).ToList());
                                graphicsStateOperations.Add(newEndInlineImage);
                                lastEndImageOffset = scanner.CurrentPosition - 2;
                            }
                            else
                            {
                                log.Warn($"Operator which was not understood encountered. Values was {op.Data}. Ignoring.");
                            }
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
