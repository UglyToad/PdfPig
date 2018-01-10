namespace UglyToad.PdfPig.Parser.FileStructure
{
    using System;
    using System.Collections.Generic;
    using ContentStream;
    using Cos;
    using IO;
    using Logging;
    using Parts;

    internal class XrefOffsetValidator
    {
        private static readonly long MinimumSearchOffset = 6;

        private readonly ILog log;
        private readonly IRandomAccessRead source;
        private readonly CosDictionaryParser dictionaryParser;
        private readonly CosBaseParser baseParser;
        private readonly CosObjectPool pool;

        private List<long> bfSearchXRefTablesOffsets = null;
        private List<long> bfSearchXRefStreamsOffsets = null;

        public XrefOffsetValidator(ILog log, IRandomAccessRead source, CosDictionaryParser dictionaryParser, 
            CosBaseParser baseParser,
            CosObjectPool pool)
        {
            this.log = log;
            this.source = source;
            this.dictionaryParser = dictionaryParser;
            this.baseParser = baseParser;
            this.pool = pool;
        }

        public long CheckXRefOffset(long startXRefOffset, bool isLenientParsing)
        {
            // repair mode isn't available in non-lenient mode
            if (!isLenientParsing)
            {
                return startXRefOffset;
            }

            source.Seek(startXRefOffset);

            ReadHelper.SkipSpaces(source);

            if (source.Peek() == 'x' && ReadHelper.IsString(source, "xref"))
            {
                return startXRefOffset;
            }
            if (startXRefOffset > 0)
            {
                if (CheckXRefStreamOffset(source, startXRefOffset, true, pool))
                {
                    return startXRefOffset;
                }

                return CalculateXRefFixedOffset(startXRefOffset);
            }
            // can't find a valid offset
            return -1;
        }
        
        private long CalculateXRefFixedOffset(long objectOffset)
        {
            if (objectOffset < 0)
            {
                // LOG.error("Invalid object offset " + objectOffset + " when searching for a xref table/stream");
                return 0;
            }
            // start a brute force search for all xref tables and try to find the offset we are looking for
            long newOffset = BfSearchForXRef(objectOffset);
            if (newOffset > -1)
            {
                // LOG.debug("Fixed reference for xref table/stream " + objectOffset + " -> " + newOffset);
                return newOffset;
            }
            // LOG.error("Can't find the object xref table/stream at offset " + objectOffset);
            return 0;
        }

        private void BfSearchForXRefStreams()
        {
            if (bfSearchXRefStreamsOffsets == null)
            {
                // a pdf may contain more than one /XRef entry
                bfSearchXRefStreamsOffsets = new List<long>();
                long originOffset = source.GetPosition();
                source.Seek(MinimumSearchOffset);
                // search for XRef streams
                var objString = " obj";
                while (!source.IsEof())
                {
                    if (ReadHelper.IsString(source, "xref"))
                    {
                        // search backwards for the beginning of the stream
                        long newOffset = -1;
                        long xrefOffset = source.GetPosition();
                        bool objFound = false;
                        for (int i = 1; i < 40 && !objFound; i++)
                        {
                            long currentOffset = xrefOffset - (i * 10);
                            if (currentOffset > 0)
                            {
                                source.Seek(currentOffset);
                                for (int j = 0; j < 10; j++)
                                {
                                    if (ReadHelper.IsString(source, objString))
                                    {
                                        long tempOffset = currentOffset - 1;
                                        source.Seek(tempOffset);
                                        int genId = source.Peek();
                                        // is the next char a digit?
                                        if (ReadHelper.IsDigit(genId))
                                        {
                                            tempOffset--;
                                            source.Seek(tempOffset);
                                            if (ReadHelper.IsSpace(source))
                                            {
                                                int length = 0;
                                                source.Seek(--tempOffset);
                                                while (tempOffset > MinimumSearchOffset && ReadHelper.IsDigit(source))
                                                {
                                                    source.Seek(--tempOffset);
                                                    length++;
                                                }
                                                if (length > 0)
                                                {
                                                    source.Read();
                                                    newOffset = source.GetPosition();
                                                }
                                            }
                                        }
                                        objFound = true;
                                        break;
                                    }
                                    else
                                    {
                                        currentOffset++;
                                        source.Read();
                                    }
                                }
                            }
                        }
                        if (newOffset > -1)
                        {
                            bfSearchXRefStreamsOffsets.Add(newOffset);
                        }
                        source.Seek(xrefOffset + 5);
                    }
                    source.Read();
                }
                source.Seek(originOffset);
            }
        }

        private long BfSearchForXRef(long xrefOffset)
        {
            long newOffset = -1;
            long newOffsetTable = -1;
            long newOffsetStream = -1;
            BfSearchForXRefTables();
            BfSearchForXRefStreams();
            if (bfSearchXRefTablesOffsets != null)
            {
                // TODO to be optimized, this won't work in every case
                newOffsetTable = SearchNearestValue(bfSearchXRefTablesOffsets, xrefOffset);
            }
            if (bfSearchXRefStreamsOffsets != null)
            {
                // TODO to be optimized, this won't work in every case
                newOffsetStream = SearchNearestValue(bfSearchXRefStreamsOffsets, xrefOffset);
            }
            // choose the nearest value
            if (newOffsetTable > -1 && newOffsetStream > -1)
            {
                long differenceTable = xrefOffset - newOffsetTable;
                long differenceStream = xrefOffset - newOffsetStream;
                if (Math.Abs(differenceTable) > Math.Abs(differenceStream))
                {
                    newOffset = newOffsetStream;
                    bfSearchXRefStreamsOffsets.Remove(newOffsetStream);
                }
                else
                {
                    newOffset = newOffsetTable;
                    bfSearchXRefTablesOffsets.Remove(newOffsetTable);
                }
            }
            else if (newOffsetTable > -1)
            {
                newOffset = newOffsetTable;
                bfSearchXRefTablesOffsets.Remove(newOffsetTable);
            }
            else if (newOffsetStream > -1)
            {
                newOffset = newOffsetStream;
                bfSearchXRefStreamsOffsets.Remove(newOffsetStream);
            }
            return newOffset;
        }

        private void BfSearchForXRefTables()
        {
            if (bfSearchXRefTablesOffsets == null)
            {
                // a pdf may contain more than one xref entry
                bfSearchXRefTablesOffsets = new List<long>();
                long originOffset = source.GetPosition();
                source.Seek(MinimumSearchOffset);
                // search for xref tables
                while (!source.IsEof())
                {
                    if (ReadHelper.IsString(source, "xref"))
                    {
                        long newOffset = source.GetPosition();
                        source.Seek(newOffset - 1);
                        // ensure that we don't read "startxref" instead of "xref"
                        if (ReadHelper.IsWhitespace(source))
                        {
                            bfSearchXRefTablesOffsets.Add(newOffset);
                        }
                        source.Seek(newOffset + 4);
                    }
                    source.Read();
                }
                source.Seek(originOffset);
            }
        }

        private long SearchNearestValue(List<long> values, long offset)
        {
            long newValue = -1;
            long? currentDifference = null;
            int currentOffsetIndex = -1;
            int numberOfOffsets = values.Count;
            // find the nearest value
            for (int i = 0; i < numberOfOffsets; i++)
            {
                long newDifference = offset - values[i];
                // find the nearest offset
                if (!currentDifference.HasValue || (Math.Abs(currentDifference.Value) > Math.Abs(newDifference)))
                {
                    currentDifference = newDifference;
                    currentOffsetIndex = i;
                }
            }
            if (currentOffsetIndex > -1)
            {
                newValue = values[currentOffsetIndex];
            }
            return newValue;
        }

        private bool CheckXRefStreamOffset(IRandomAccessRead source, long startXRefOffset, bool isLenient, CosObjectPool pool)
        {
            // repair mode isn't available in non-lenient mode
            if (!isLenient || startXRefOffset == 0)
            {
                return true;
            }
            // seek to offset-1 
            source.Seek(startXRefOffset - 1);
            int nextValue = source.Read();
            // the first character has to be a whitespace, and then a digit
            if (ReadHelper.IsWhitespace(nextValue))
            {
                ReadHelper.SkipSpaces(source);
                if (ReadHelper.IsDigit(source))
                {
                    try
                    {
                        // it's a XRef stream
                        ObjectHelper.ReadObjectNumber(source);
                        ObjectHelper.ReadGenerationNumber(source);

                        ReadHelper.ReadExpectedString(source, "obj", true);

                        // check the dictionary to avoid false positives
                        PdfDictionary dict = dictionaryParser.Parse(source, baseParser, pool);
                        source.Seek(startXRefOffset);
                        
                        if (dict.IsType(CosName.XREF))
                        {
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error("Couldn't read the xref stream object.", ex);
                        // there wasn't an object of a xref stream
                        source.Seek(startXRefOffset);
                    }
                }
            }
            return false;
        }
    }
}
