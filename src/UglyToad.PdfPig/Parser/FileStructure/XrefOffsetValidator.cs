namespace UglyToad.PdfPig.Parser.FileStructure
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Logging;
    using Tokenization.Scanner;
    using Tokens;

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

        public long CheckXRefOffset(long startXRefOffset, ISeekableTokenScanner scanner, IInputBytes inputBytes, bool isLenientParsing)
        {
            // repair mode isn't available in non-lenient mode
            if (!isLenientParsing)
            {
                return startXRefOffset;
            }

            if (startXRefOffset >= inputBytes.Length)
            {
                return CalculateXRefFixedOffset(startXRefOffset, scanner, inputBytes);
            }

            scanner.Seek(startXRefOffset);

            scanner.MoveNext();

            if (ReferenceEquals(scanner.CurrentToken, OperatorToken.Xref))
            {
                return startXRefOffset;
            }
            
            if (startXRefOffset > 0)
            {
                if (CheckXRefStreamOffset(startXRefOffset, scanner, true))
                {
                    return startXRefOffset;
                }

                return CalculateXRefFixedOffset(startXRefOffset, scanner, inputBytes);
            }

            // can't find a valid offset
            return -1;
        }

        private long CalculateXRefFixedOffset(long objectOffset, ISeekableTokenScanner scanner, IInputBytes inputBytes)
        {
            if (objectOffset < 0)
            {
                log.Error($"Invalid object offset {objectOffset} when searching for a xref table/stream");
                return 0;
            }

            // start a brute force search for all xref tables and try to find the offset we are looking for
            var newOffset = BruteForceSearchForXref(objectOffset, scanner, inputBytes);

            if (newOffset > -1)
            {
                log.Debug($"Fixed reference for xref table/stream {objectOffset} -> {newOffset}");

                return newOffset;
            }

            log.Error($"Can\'t find the object xref table/stream at offset {objectOffset}");

            return 0;
        }

        private long BruteForceSearchForXref(long xrefOffset, ISeekableTokenScanner scanner, IInputBytes reader)
        {
            long newOffset = -1;
            long newOffsetTable = -1;
            long newOffsetStream = -1;

            BruteForceSearchForTables(reader);

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

        private void BruteForceSearchForTables(IInputBytes bytes)
        {
            if (bfSearchXRefTablesOffsets != null)
            {
                return;
            }

            // a pdf may contain more than one xref entry
            bfSearchXRefTablesOffsets = new List<long>();

            var startOffset = bytes.CurrentOffset;

            bytes.Seek(MinimumSearchOffset);

            // search for xref tables
            while (bytes.MoveNext() && !bytes.IsAtEnd())
            {
                if (ReadHelper.IsString(bytes, "xref"))
                {
                    var newOffset = bytes.CurrentOffset;

                    bytes.Seek(newOffset - 1);

                    // ensure that we don't read "startxref" instead of "xref"
                    if (ReadHelper.IsWhitespace(bytes.CurrentByte))
                    {
                        bfSearchXRefTablesOffsets.Add(newOffset);
                    }

                    bytes.Seek(newOffset + 4);
                }
            }

            bytes.Seek(startOffset);
        }

        private void BfSearchForXRefStreams(IInputBytes bytes)
        {
            if (bfSearchXRefStreamsOffsets != null)
            {
                return;
            }

            // a pdf may contain more than one /XRef entry
            bfSearchXRefStreamsOffsets = new List<long>();

            var startOffset = bytes.CurrentOffset;

            bytes.Seek(MinimumSearchOffset);

            // search for XRef streams
            var objString = " obj";

            while (bytes.MoveNext() && !bytes.IsAtEnd())
            {
                if (!ReadHelper.IsString(bytes, "xref"))
                {
                    continue;
                }

                // search backwards for the beginning of the stream
                long newOffset = -1;
                long xrefOffset = bytes.CurrentOffset;

                bool objFound = false;
                for (var i = 1; i < 40; i++)
                {
                    if (objFound)
                    {
                        break;
                    }

                    long currentOffset = xrefOffset - (i * 10);

                    if (currentOffset > 0)
                    {
                        bytes.Seek(currentOffset);

                        for (int j = 0; j < 10; j++)
                        {
                            if (ReadHelper.IsString(bytes, objString))
                            {
                                long tempOffset = currentOffset - 1;

                                bytes.Seek(tempOffset);

                                var generationNumber = bytes.Peek();

                                // is the next char a digit?
                                if (generationNumber.HasValue && ReadHelper.IsDigit(generationNumber.Value))
                                {
                                    tempOffset--;
                                    bytes.Seek(tempOffset);

                                    // is the digit preceded by a space?
                                    if (ReadHelper.IsSpace(bytes.CurrentByte))
                                    {
                                        int length = 0;
                                        bytes.Seek(--tempOffset);

                                        while (tempOffset > MinimumSearchOffset && ReadHelper.IsDigit(bytes.CurrentByte))
                                        {
                                            bytes.Seek(--tempOffset);
                                            length++;
                                        }

                                        if (length > 0)
                                        {
                                            bytes.MoveNext();
                                            newOffset = bytes.CurrentOffset;
                                        }
                                    }
                                }

                                objFound = true;

                                break;
                            }

                            currentOffset++;
                            bytes.MoveNext();
                        }
                    }
                }

                if (newOffset > -1)
                {
                    bfSearchXRefStreamsOffsets.Add(newOffset);
                }

                bytes.Seek(xrefOffset + 5);
            }

            bytes.Seek(startOffset);
        }

        private static long SearchNearestValue(List<long> values, long offset)
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

            scanner.Seek(startXRefOffset);

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
            else
            {
                log.Error($"When looking for the cross reference stream object we sought a number but found: {scanner.CurrentToken}.");
            }

            return false;
        }
    }
}
