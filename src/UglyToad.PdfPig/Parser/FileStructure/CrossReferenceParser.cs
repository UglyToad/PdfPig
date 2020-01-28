namespace UglyToad.PdfPig.Parser.FileStructure
{
    using System;
    using System.Collections.Generic;
    using Core;
    using CrossReference;
    using Exceptions;
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
        
        public CrossReferenceTable Parse(IInputBytes bytes, bool isLenientParsing, long crossReferenceLocation,
            long offsetCorrection,
            IPdfTokenScanner pdfScanner, 
            ISeekableTokenScanner tokenScanner)
        {
            long fixedOffset = offsetValidator.CheckXRefOffset(crossReferenceLocation, tokenScanner, bytes, isLenientParsing);
            if (fixedOffset > -1)
            {
                crossReferenceLocation = fixedOffset;

                log.Debug($"Found the first cross reference table or stream at {fixedOffset}.");
            }

            var table = new CrossReferenceTableBuilder();

            var prevSet = new HashSet<long>();
            long previousCrossReferenceLocation = crossReferenceLocation;

            var missedAttempts = 0;
            
            // Parse all cross reference tables and streams.
            while (previousCrossReferenceLocation > 0 && missedAttempts < 100)
            {
                log.Debug($"Reading cross reference table or stream at {previousCrossReferenceLocation}.");

                if (previousCrossReferenceLocation >= bytes.Length)
                {
                    break;
                }

                // seek to xref table
                tokenScanner.Seek(previousCrossReferenceLocation);

                tokenScanner.MoveNext();

                if (tokenScanner.CurrentToken is OperatorToken tableToken && tableToken.Data == "xref")
                {
                    missedAttempts = 0;
                    log.Debug("Element was cross reference table.");

                    CrossReferenceTablePart tablePart = crossReferenceTableParser.Parse(tokenScanner,
                        previousCrossReferenceLocation, isLenientParsing);

                    var nextOffset = tablePart.GetPreviousOffset();

                    if (nextOffset >= 0)
                    {
                        nextOffset += offsetCorrection;
                    }

                    previousCrossReferenceLocation = nextOffset;

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
                                TryParseCrossReferenceStream(streamOffset, pdfScanner, out streamPart);
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

                    missedAttempts = 0;

                    // Unread the numeric token.
                    tokenScanner.Seek(previousCrossReferenceLocation);

                    // parse xref stream
                    if (!TryParseCrossReferenceStream(previousCrossReferenceLocation, pdfScanner, out var tablePart))
                    {
                        if (!TryBruteForceXrefTableLocate(bytes, previousCrossReferenceLocation, out var actualOffset))
                        {
                            throw new PdfDocumentFormatException();
                        }

                        previousCrossReferenceLocation = actualOffset;
                        missedAttempts++;
                        continue;
                    }

                    table.Add(tablePart);

                    previousCrossReferenceLocation = tablePart.Previous;

                    if (previousCrossReferenceLocation >= 0)
                    {
                        previousCrossReferenceLocation += offsetCorrection;
                    }

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
                    log.Debug($"The cross reference found at this location ({previousCrossReferenceLocation}) was not a table or stream. " +
                              $"Found token ({tokenScanner.CurrentToken}) ending at {tokenScanner.CurrentPosition} instead. Seeking next token.");

                    previousCrossReferenceLocation = tokenScanner.CurrentPosition;

                    missedAttempts++;

                    continue;
                }

                if (prevSet.Contains(previousCrossReferenceLocation))
                {
                    throw new PdfDocumentFormatException("The cross references formed an infinite loop.");
                }

                prevSet.Add(previousCrossReferenceLocation);
            }

            if (missedAttempts == 100)
            {
                // TODO: scan the document to find the correct token.
                throw new PdfDocumentFormatException("The cross reference was not found.");
            }

            var resolved = table.Build(crossReferenceLocation, log);
            
            // check the offsets of all referenced objects
            xrefCosChecker.CheckCrossReferenceOffsets(bytes, resolved, isLenientParsing);
            
            return resolved;
        }

        private bool TryParseCrossReferenceStream(long objByteOffset, IPdfTokenScanner pdfScanner,
            out CrossReferenceTablePart xrefTablePart)
        {
            xrefTablePart = null;

            pdfScanner.Seek(objByteOffset);

            pdfScanner.MoveNext();

            var streamObjectToken = (ObjectToken)pdfScanner.CurrentToken;

            if (streamObjectToken == null || !(streamObjectToken.Data is StreamToken objectStream))
            {
                log.Error($"When reading a cross reference stream object found a non-stream object: {streamObjectToken?.Data}");

                return false;
            }
            
            xrefTablePart = crossReferenceStreamParser.Parse(objByteOffset, objectStream);

            return true;
        }

        private bool TryBruteForceXrefTableLocate(IInputBytes bytes, long expectedOffset, 
            out long actualOffset)
        {
            actualOffset = expectedOffset;

            bytes.Seek(expectedOffset - 1);
            var currentByte = bytes.CurrentByte;

            // Forward:
            while (bytes.MoveNext())
            {
                var previousByte = currentByte;
                currentByte = bytes.CurrentByte;

                if (currentByte != 'x' || !ReadHelper.IsWhitespace(previousByte))
                {
                    continue;
                }

                if (!ReadHelper.IsString(bytes, "xref"))
                {
                    continue;
                }

                actualOffset = bytes.CurrentOffset;
                return true;
            }

            var lastOffset = expectedOffset - 1;

            if (lastOffset < 0)
            {
                return false;
            }

            bytes.Seek(lastOffset);

            var buffer = new byte[5];

            while (bytes.Read(buffer) == buffer.Length)
            {
                for (var i = 1; i < buffer.Length; i++)
                {
                    var p = buffer[i - 1];
                    var b = buffer[i];

                    if (b != 'x' || !ReadHelper.IsWhitespace(p))
                    {
                        continue;
                    }

                    var xLocation = lastOffset + i + 1;

                    bytes.Seek(xLocation);

                    if (ReadHelper.IsString(bytes, "xref"))
                    {
                        actualOffset = xLocation;
                        return true;
                    }
                }

                lastOffset -= buffer.Length;
                if (lastOffset < 0)
                {
                    break;
                }

                bytes.Seek(lastOffset);
            }
            bytes.Read(buffer);



            return false;
        }
    }
}
