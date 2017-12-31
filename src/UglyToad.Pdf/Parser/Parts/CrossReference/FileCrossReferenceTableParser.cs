namespace UglyToad.Pdf.Parser.Parts.CrossReference
{
    using System;
    using System.Collections.Generic;
    using ContentStream;
    using ContentStream.TypedAccessors;
    using Cos;
    using IO;
    using Logging;

    internal class FileCrossReferenceTableParser
    {
        private const int X = 'x';

        private readonly ILog log;
        private readonly CosDictionaryParser dictionaryParser;
        private readonly CosBaseParser baseParser;
        private readonly CosStreamParser streamParser;
        private readonly CrossReferenceStreamParser crossReferenceStreamParser;
        private readonly CrossReferenceTableParser crossReferenceTableParser;

        public FileCrossReferenceTableParser(ILog log, CosDictionaryParser dictionaryParser, CosBaseParser baseParser, 
            CosStreamParser streamParser,
            CrossReferenceStreamParser crossReferenceStreamParser,
            CrossReferenceTableParser crossReferenceTableParser)
        {
            this.log = log;
            this.dictionaryParser = dictionaryParser;
            this.baseParser = baseParser;
            this.streamParser = streamParser;
            this.crossReferenceStreamParser = crossReferenceStreamParser;
            this.crossReferenceTableParser = crossReferenceTableParser;
        }

        public CrossReferenceTable Parse(IRandomAccessRead reader, bool isLenientParsing, long xrefLocation,
            CosObjectPool pool)
        {
            var xrefOffsetValidator = new XrefOffsetValidator(log, reader, dictionaryParser, baseParser, pool);
            var xrefCosChecker = new XrefCosOffsetChecker();
            long fixedOffset = xrefOffsetValidator.CheckXRefOffset(xrefLocation, isLenientParsing);
            if (fixedOffset > -1)
            {
                xrefLocation = fixedOffset;
            }

            var table = new CrossReferenceTableBuilder();

            long previousCrossReferenceLocation = xrefLocation;
            // ---- parse whole chain of xref tables/object streams using PREV reference
            HashSet<long> prevSet = new HashSet<long>();
            while (previousCrossReferenceLocation > 0)
            {
                // seek to xref table
                reader.Seek(previousCrossReferenceLocation);
                
                ReadHelper.SkipSpaces(reader);

                var isTable = reader.Peek() == X;

                // -- parse xref
                if (isTable)
                {
                    // xref table and trailer
                    // use existing parser to parse xref table
                    if (!crossReferenceTableParser.TryParse(reader, previousCrossReferenceLocation, isLenientParsing, pool, out var tableBuilder))
                    {
                        throw new InvalidOperationException($"Expected trailer object at position: {reader.GetPosition()}");
                    }

                    PdfDictionary trailer = tableBuilder.Dictionary;
                    CrossReferenceTablePart streamPart = null;
                    // check for a XRef stream, it may contain some object ids of compressed objects 
                    if (trailer.ContainsKey(CosName.XREF_STM))
                    {
                        int streamOffset = trailer.GetIntOrDefault(CosName.XREF_STM);
                        // check the xref stream reference
                        fixedOffset = xrefOffsetValidator.CheckXRefOffset(streamOffset, isLenientParsing);
                        if (fixedOffset > -1 && fixedOffset != streamOffset)
                        {
                            log.Warn("/XRefStm offset " + streamOffset + " is incorrect, corrected to " + fixedOffset);
                            streamOffset = (int)fixedOffset;
                            trailer.SetInt(CosName.XREF_STM, streamOffset);
                            tableBuilder.Offset = streamOffset;
                        }
                        if (streamOffset > 0)
                        {
                            reader.Seek(streamOffset);
                            ReadHelper.SkipSpaces(reader);
                            try
                            {
                                streamPart = ParseCrossReferenceStream(reader, previousCrossReferenceLocation, pool, isLenientParsing);
                            }
                            catch (InvalidOperationException ex)
                            {
                                if (isLenientParsing)
                                {
                                    log.Error("Failed to parse /XRefStm at offset " + streamOffset, ex);
                                }
                                else
                                {
                                    throw ex;
                                }
                            }
                        }
                        else
                        {
                            if (isLenientParsing)
                            {
                                log.Error("Skipped XRef stream due to a corrupt offset:"+streamOffset);
                            }
                            else
                            {
                                throw new InvalidOperationException("Skipped XRef stream due to a corrupt offset:" + streamOffset);
                            }
                        }
                    }
                    previousCrossReferenceLocation = trailer.GetLongOrDefault(CosName.PREV);
                    if (previousCrossReferenceLocation > 0)
                    {
                        // check the xref table reference
                        fixedOffset = xrefOffsetValidator.CheckXRefOffset(previousCrossReferenceLocation, isLenientParsing);
                        if (fixedOffset > -1 && fixedOffset != previousCrossReferenceLocation)
                        {
                            previousCrossReferenceLocation = fixedOffset;
                            trailer.SetLong(CosName.PREV, previousCrossReferenceLocation);
                        }
                    }

                    tableBuilder.Previous = tableBuilder.Dictionary.GetLongOrDefault(CosName.PREV);

                    table.Add(tableBuilder.AsCrossReferenceTablePart());

                    if (streamPart != null)
                    {
                        table.Add(streamPart);
                    }
                }
                else
                {
                    // parse xref stream
                    var tablePart = ParseCrossReferenceStream(reader, previousCrossReferenceLocation, pool, isLenientParsing);
                    table.Add(tablePart);

                    previousCrossReferenceLocation = tablePart.Previous;
                    if (previousCrossReferenceLocation > 0)
                    {
                        // check the xref table reference
                        fixedOffset = xrefOffsetValidator.CheckXRefOffset(previousCrossReferenceLocation, isLenientParsing);
                        if (fixedOffset > -1 && fixedOffset != previousCrossReferenceLocation)
                        {
                            previousCrossReferenceLocation = fixedOffset;
                            tablePart.FixOffset(previousCrossReferenceLocation);
                        }
                    }
                }
                if (prevSet.Contains(previousCrossReferenceLocation))
                {
                    throw new InvalidOperationException("/Prev loop at offset " + previousCrossReferenceLocation);
                }
                prevSet.Add(previousCrossReferenceLocation);
            }

            var resolved = table.Build(xrefLocation, log);
            
            // check the offsets of all referenced objects
            xrefCosChecker.checkXrefOffsets(reader, resolved, isLenientParsing);
            
            return resolved;
        }
        
        private CrossReferenceTablePart ParseCrossReferenceStream(IRandomAccessRead reader, long objByteOffset, CosObjectPool pool, 
            bool isLenientParsing)
        {
            // ---- parse indirect object head
            ObjectHelper.ReadObjectNumber(reader);
            ObjectHelper.ReadGenerationNumber(reader);

            ReadHelper.ReadExpectedString(reader, "obj", true);

            PdfDictionary dict = dictionaryParser.Parse(reader, baseParser, pool);

            PdfRawStream xrefStream = streamParser.Parse(reader, dict, isLenientParsing, null);
            CrossReferenceTablePart xrefTablePart = crossReferenceStreamParser.Parse(objByteOffset, xrefStream);

            return xrefTablePart;
        }
    }
}
