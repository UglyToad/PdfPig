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
                var objStreams = new List<IndirectReference>();

                // find all object streams
                foreach (var entry in crossReferenceTable.ObjectOffsets)
                {
                    var offset = entry.Value;
                    if (offset < 0)
                    {
                        var objStream = new IndirectReference(-offset, 0);
                        if (!objStreams.Contains(objStream))
                        {
                            objStreams.Add(new IndirectReference(-offset, 0));
                        }
                    }

                    // remove all found object streams
                    if (objStreams.Count > 0)
                    {
                        foreach (var key in objStreams)
                        {
                            if (bruteForceOffsets.ContainsKey(key))
                            {
                                // remove all parsed objects which are part of an object stream
                                //ISet<long> objects = xrefTrailerResolver
                                //    .getContainedObjectNumbers((int)(key.Number));
                                //foreach (long objNr in objects)
                                //{
                                //    CosObjectKey streamObjectKey = new CosObjectKey(objNr, 0);

                                //    if (bfCOSObjectKeyOffsets.TryGetValue(streamObjectKey, out long streamObjectOffset) && streamObjectOffset > 0)
                                //    {
                                //        bfCOSObjectKeyOffsets.Remove(streamObjectKey);
                                //    }
                                //}
                            }
                            else
                            {
                                // remove all objects which are part of an object stream which wasn't found
                                //ISet<long> objects = xrefTrailerResolver
                                //    .getContainedObjectNumbers((int)(key.Number));
                                //foreach (long objNr in objects)
                                //{
                                //    xrefOffset.Remove(new CosObjectKey(objNr, 0));
                                //}
                            }
                        }
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
            if (objectOffsets == null)
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
