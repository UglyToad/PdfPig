namespace UglyToad.PdfPig.Parser.FileStructure
{
    using System;
    using System.Collections.Generic;
    using Cos;
    using Exceptions;
    using IO;
    using Logging;
    using Parts.CrossReference;
    using Tokenization.Scanner;
    using Tokens;

    internal class CrossReferenceParser
    {
        private readonly ILog log;
        private readonly XrefOffsetValidator offsetValidator;
        private readonly CrossReferenceStreamParser crossReferenceStreamParser;
        private readonly CrossReferenceTableParser crossReferenceTableParser;
        private readonly XrefCosOffsetChecker xrefCosChecker;

        public CrossReferenceParser(ILog log, XrefOffsetValidator offsetValidator,
            XrefCosOffsetChecker xrefCosChecker,
            CrossReferenceStreamParser crossReferenceStreamParser,
            CrossReferenceTableParser crossReferenceTableParser)
        {
            this.log = log;
            this.offsetValidator = offsetValidator;
            this.crossReferenceStreamParser = crossReferenceStreamParser;
            this.crossReferenceTableParser = crossReferenceTableParser;
            this.xrefCosChecker = xrefCosChecker;
        }
        
        public CrossReferenceTable Parse(IInputBytes bytes, bool isLenientParsing, long xrefLocation, IPdfTokenScanner pdfScanner, ISeekableTokenScanner tokenScanner)
        {
            long fixedOffset = offsetValidator.CheckXRefOffset(xrefLocation, tokenScanner, bytes, isLenientParsing);
            if (fixedOffset > -1)
            {
                xrefLocation = fixedOffset;

                log.Debug($"Found the first cross reference table or stream at {fixedOffset}.");
            }

            var table = new CrossReferenceTableBuilder();

            var prevSet = new HashSet<long>();
            long previousCrossReferenceLocation = xrefLocation;

            // Parse all cross reference tables and streams.
            while (previousCrossReferenceLocation > 0)
            {
                log.Debug($"Reading cross reference table or stream at {previousCrossReferenceLocation}.");

                // seek to xref table
                tokenScanner.Seek(previousCrossReferenceLocation);

                tokenScanner.MoveNext();

                if (tokenScanner.CurrentToken is OperatorToken tableToken && tableToken.Data == "xref")
                {
                    log.Debug("Element was cross reference table.");

                    CrossReferenceTablePart tablePart = crossReferenceTableParser.Parse(tokenScanner,
                        previousCrossReferenceLocation, isLenientParsing);

                    previousCrossReferenceLocation = tablePart.GetPreviousOffset();

                    DictionaryToken tableDictionary = tablePart.Dictionary;

                    CrossReferenceTablePart streamPart = null;

                    // check for a XRef stream, it may contain some object ids of compressed objects 
                    if (tableDictionary.ContainsKey(NameToken.XrefStm))
                    {
                        log.Debug("Cross reference table contained referenced to stream. Reading the stream.");

                        int streamOffset = ((NumericToken)tableDictionary.Data[NameToken.XrefStm]).Int;

                        // check the xref stream reference
                        fixedOffset = offsetValidator.CheckXRefOffset(streamOffset, tokenScanner, bytes, isLenientParsing);
                        if (fixedOffset > -1 && fixedOffset != streamOffset)
                        {
                            log.Warn($"/XRefStm offset {streamOffset} is incorrect, corrected to {fixedOffset}");

                            streamOffset = (int)fixedOffset;

                            // Update the cross reference table to be a stream instead.
                            tableDictionary = tableDictionary.With(NameToken.XrefStm, new NumericToken(streamOffset));
                            tablePart = new CrossReferenceTablePart(tablePart.ObjectOffsets, streamOffset,
                                tablePart.Previous, tableDictionary, tablePart.Type);
                        }

                        // Read the stream from the table.
                        if (streamOffset > 0)
                        {
                            try
                            {
                                streamPart = ParseCrossReferenceStream(streamOffset, pdfScanner);
                            }
                            catch (InvalidOperationException ex)
                            {
                                if (isLenientParsing)
                                {
                                    log.Error("Failed to parse /XRefStm at offset " + streamOffset, ex);
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                        else
                        {
                            if (isLenientParsing)
                            {
                                log.Error("Skipped XRef stream due to a corrupt offset:" + streamOffset);
                            }
                            else
                            {
                                throw new PdfDocumentFormatException("Skipped XRef stream due to a corrupt offset:" + streamOffset);
                            }
                        }
                    }
                    
                    table.Add(tablePart);

                    if (streamPart != null)
                    {
                        table.Add(streamPart);
                    }
                }
                else if (tokenScanner.CurrentToken is NumericToken)
                {
                    log.Debug("Element was cross reference stream.");

                    // Unread the numeric token.
                    tokenScanner.Seek(previousCrossReferenceLocation);

                    // parse xref stream
                    var tablePart = ParseCrossReferenceStream(previousCrossReferenceLocation, pdfScanner);
                    table.Add(tablePart);

                    previousCrossReferenceLocation = tablePart.Previous;
                    if (previousCrossReferenceLocation > 0)
                    {
                        // check the xref table reference
                        fixedOffset = offsetValidator.CheckXRefOffset(previousCrossReferenceLocation, tokenScanner, bytes, isLenientParsing);
                        if (fixedOffset > -1 && fixedOffset != previousCrossReferenceLocation)
                        {
                            previousCrossReferenceLocation = fixedOffset;
                            tablePart.FixOffset(previousCrossReferenceLocation);
                        }
                    }
                }
                else
                {
                    log.Debug("Element was invalid.");

                    throw new PdfDocumentFormatException("The cross reference found at this location was not a " +
                                                         $"table or a stream: Location - {previousCrossReferenceLocation}, {tokenScanner.CurrentPosition}.");
                }

                if (prevSet.Contains(previousCrossReferenceLocation))
                {
                    throw new PdfDocumentFormatException("The cross references formed an infinite loop.");
                }

                prevSet.Add(previousCrossReferenceLocation);
            }

            var resolved = table.Build(xrefLocation, log);
            
            // check the offsets of all referenced objects
            xrefCosChecker.CheckCrossReferenceOffsets(bytes, resolved, isLenientParsing);
            
            return resolved;
        }

        private CrossReferenceTablePart ParseCrossReferenceStream(long objByteOffset, IPdfTokenScanner pdfScanner)
        {
            pdfScanner.Seek(objByteOffset);

            pdfScanner.MoveNext();

            var streamObjectToken = (ObjectToken)pdfScanner.CurrentToken;

            if (streamObjectToken == null || !(streamObjectToken.Data is StreamToken objectStream))
            {
                throw new PdfDocumentFormatException($"When reading a cross reference stream object found a non-stream object: {streamObjectToken?.Data}");
            }
            
            CrossReferenceTablePart xrefTablePart = crossReferenceStreamParser.Parse(objByteOffset, objectStream);

            return xrefTablePart;
        }
    }
}
