namespace UglyToad.PdfPig.Parser.FileStructure
{
    using System;
    using System.Collections.Generic;
    using Core;
    using CrossReference;
    using Logging;
    using Parts;

    internal static class CrossReferenceObjectOffsetValidator
    {
        private const long MinimumSearchOffset = 6;
        
        /// <summary>
        /// Check that the offsets in the cross reference are correct.
        /// </summary>
        public static bool ValidateCrossReferenceOffsets(IInputBytes bytes, CrossReferenceTable crossReferenceTable, ILog log,
            out IReadOnlyDictionary<IndirectReference, long> actualOffsets)
        {
            actualOffsets = crossReferenceTable.ObjectOffsets;

            if (ValidateXrefOffsets(bytes, crossReferenceTable.ObjectOffsets, log))
            {
                return true;
            }

            var builderOffsets = new Dictionary<IndirectReference, long>();
            
            var bruteForceOffsets = BruteForceSearcher.GetObjectLocations(bytes);
            if (bruteForceOffsets.Count > 0)
            {
                // find all object streams
                foreach (var entry in crossReferenceTable.ObjectOffsets)
                {
                    var offset = entry.Value;
                    if (offset < 0)
                    {
                        // Trust stream offsets for now.
                        // TODO: more validation of streams.
                        builderOffsets[entry.Key] = entry.Value;
                    }

                    foreach (var item in bruteForceOffsets)
                    {
                        builderOffsets[item.Key] = item.Value;
                    }
                }

                actualOffsets = builderOffsets;
            }

            return false;
        }
        
        private static bool ValidateXrefOffsets(IInputBytes bytes, IReadOnlyDictionary<IndirectReference, long> objectOffsets, ILog log)
        {
            if (objectOffsets is null)
            {
                return true;
            }

            foreach (var objectEntry in objectOffsets)
            {
                var objectKey = objectEntry.Key;
                var objectOffset = objectEntry.Value;

                if (objectOffset < 0)
                {
                    continue;
                }

                if (!CheckObjectKeys(bytes, objectKey, objectOffset))
                {
                    log.Error($"At least one cross-reference offset was incorrect. {objectKey} could not be found at {objectOffset}. " +
                              "Using brute-force search to repair object offsets.");

                    return false;
                }
            }

            return true;
        }

        private static bool CheckObjectKeys(IInputBytes bytes, IndirectReference objectKey, long offset)
        {
            // there can't be any object at the very beginning of a pdf
            if (offset < MinimumSearchOffset)
            {
                return false;
            }

            var objectNr = objectKey.ObjectNumber;
            long objectGen = objectKey.Generation;
            var originOffset = bytes.CurrentOffset;

            var objectString = ObjectHelper.CreateObjectString(objectNr, objectGen);

            try
            {
                if (offset >= bytes.Length)
                {
                    bytes.Seek(originOffset);
                    return false;
                }

                bytes.Seek(offset);

                if (ReadHelper.IsWhitespace(bytes.CurrentByte))
                {
                    bytes.MoveNext();
                }

                if (ReadHelper.IsString(bytes, objectString))
                {
                    // everything is ok, return origin object key
                    bytes.Seek(originOffset);
                    return true;
                }
            }
            catch (Exception)
            {
                // Swallow the exception, obviously there isn't any valid object number
            }
            finally
            {
                bytes.Seek(originOffset);
            }

            // no valid object number found
            return false;
        }
    }
}
