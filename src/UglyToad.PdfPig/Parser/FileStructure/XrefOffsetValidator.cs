namespace UglyToad.PdfPig.Parser.FileStructure
{
    using System;
    using System.Collections.Generic;
    using IO;
    using Logging;
    using Parts;
    using Tokenization.Scanner;
    using Tokenization.Tokens;

    internal class XrefOffsetValidator
    {
        private static readonly long MinimumSearchOffset = 6;

        private readonly ILog log;

        private List<long> bfSearchXRefTablesOffsets;
        private List<long> bfSearchXRefStreamsOffsets;

        public XrefOffsetValidator(ILog log)
        {
            this.log = log;
        }

        public long CheckXRefOffset(long startXRefOffset, ISeekableTokenScanner scanner, IRandomAccessRead reader, bool isLenientParsing)
        {
            // repair mode isn't available in non-lenient mode
            if (!isLenientParsing)
            {
                return startXRefOffset;
            }

            reader.Seek(startXRefOffset);

            ReadHelper.SkipSpaces(reader);

            if (reader.Peek() == 'x' && ReadHelper.IsString(reader, "xref"))
            {
                return startXRefOffset;
            }
            if (startXRefOffset > 0)
            {
                if (CheckXRefStreamOffset(startXRefOffset, scanner, true))
                {
                    return startXRefOffset;
                }

                return CalculateXRefFixedOffset(startXRefOffset, scanner, reader);
            }

            // can't find a valid offset
            return -1;
        }

        private long CalculateXRefFixedOffset(long objectOffset, ISeekableTokenScanner scanner, IRandomAccessRead reader)
        {
            if (objectOffset < 0)
            {
                log.Error($"Invalid object offset {objectOffset} when searching for a xref table/stream");
                return 0;
            }

            // start a brute force search for all xref tables and try to find the offset we are looking for
            long newOffset = BfSearchForXRef(objectOffset, scanner, reader);
            if (newOffset > -1)
            {
                log.Debug($"Fixed reference for xref table/stream {objectOffset} -> {newOffset}");
                return newOffset;
            }

            log.Error($"Can\'t find the object xref table/stream at offset {objectOffset}");

            return 0;
        }

        private void BfSearchForXRefStreams(IRandomAccessRead reader)
        {
            if (bfSearchXRefStreamsOffsets != null)
            {
                return;
            }

            // a pdf may contain more than one /XRef entry
            bfSearchXRefStreamsOffsets = new List<long>();
            long originOffset = reader.GetPosition();
            reader.Seek(MinimumSearchOffset);
            // search for XRef streams
            var objString = " obj";
            while (!reader.IsEof())
            {
                if (ReadHelper.IsString(reader, "xref"))
                {
                    // search backwards for the beginning of the stream
                    long newOffset = -1;
                    long xrefOffset = reader.GetPosition();
                    bool objFound = false;
                    for (int i = 1; i < 40 && !objFound; i++)
                    {
                        long currentOffset = xrefOffset - (i * 10);
                        if (currentOffset > 0)
                        {
                            reader.Seek(currentOffset);
                            for (int j = 0; j < 10; j++)
                            {
                                if (ReadHelper.IsString(reader, objString))
                                {
                                    long tempOffset = currentOffset - 1;
                                    reader.Seek(tempOffset);
                                    int genId = reader.Peek();
                                    // is the next char a digit?
                                    if (ReadHelper.IsDigit(genId))
                                    {
                                        tempOffset--;
                                        reader.Seek(tempOffset);
                                        if (ReadHelper.IsSpace(reader))
                                        {
                                            int length = 0;
                                            reader.Seek(--tempOffset);
                                            while (tempOffset > MinimumSearchOffset && ReadHelper.IsDigit(reader))
                                            {
                                                reader.Seek(--tempOffset);
                                                length++;
                                            }
                                            if (length > 0)
                                            {
                                                reader.Read();
                                                newOffset = reader.GetPosition();
                                            }
                                        }
                                    }
                                    objFound = true;
                                    break;
                                }
                                else
                                {
                                    currentOffset++;
                                    reader.Read();
                                }
                            }
                        }
                    }
                    if (newOffset > -1)
                    {
                        bfSearchXRefStreamsOffsets.Add(newOffset);
                    }
                    reader.Seek(xrefOffset + 5);
                }
                reader.Read();
            }
            reader.Seek(originOffset);
        }

        private long BfSearchForXRef(long xrefOffset, ISeekableTokenScanner scanner, IRandomAccessRead reader)
        {
            long newOffset = -1;
            long newOffsetTable = -1;
            long newOffsetStream = -1;
            BfSearchForXRefTables(reader);
            BfSearchForXRefStreams(reader);
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

        private void BfSearchForXRefTables(IRandomAccessRead reader)
        {
            if (bfSearchXRefTablesOffsets == null)
            {
                // a pdf may contain more than one xref entry
                bfSearchXRefTablesOffsets = new List<long>();
                long originOffset = reader.GetPosition();
                reader.Seek(MinimumSearchOffset);
                // search for xref tables
                while (!reader.IsEof())
                {
                    if (ReadHelper.IsString(reader, "xref"))
                    {
                        long newOffset = reader.GetPosition();
                        reader.Seek(newOffset - 1);
                        // ensure that we don't read "startxref" instead of "xref"
                        if (ReadHelper.IsWhitespace(reader))
                        {
                            bfSearchXRefTablesOffsets.Add(newOffset);
                        }
                        reader.Seek(newOffset + 4);
                    }
                    reader.Read();
                }
                reader.Seek(originOffset);
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

        private bool CheckXRefStreamOffset(long startXRefOffset, ISeekableTokenScanner scanner, bool isLenient)
        {
            // repair mode isn't available in non-lenient mode
            if (!isLenient || startXRefOffset == 0)
            {
                return true;
            }
            // seek to offset-1 
            scanner.Seek(startXRefOffset - 1);
            if (scanner.TryReadToken(out NumericToken objectNumber))
            {
                try
                {
                    if (!scanner.TryReadToken(out NumericToken generation))
                    {
                        log.Debug($"When checking offset at {startXRefOffset} did not find the generation number. Got: {objectNumber} {generation}.");
                    }
                    
                    scanner.MoveNext();

                    var obj = scanner.CurrentToken;

                    if (!ReferenceEquals(obj, OperatorToken.StartObject))
                    {
                        scanner.Seek(startXRefOffset);
                        return false;
                    }

                    // check the dictionary to avoid false positives
                    if (!scanner.TryReadToken(out DictionaryToken dictionary))
                    {
                        scanner.Seek(startXRefOffset);

                    }

                    if (dictionary.TryGet(NameToken.Type, out var type) && NameToken.Xref.Equals(type))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Couldn't read the xref stream object.", ex);
                }
            }
            return false;
        }
    }
}
