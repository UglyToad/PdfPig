namespace UglyToad.Pdf.Parser.Parts.CrossReference
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cos;
    using IO;
    using Util;

    internal class XrefCosOffsetChecker
    {
        private static readonly long MINIMUM_SEARCH_OFFSET = 6;

        private Dictionary<CosObjectKey, long> bfSearchCOSObjectKeyOffsets;
        
        private bool validateXrefOffsets(IRandomAccessRead reader, Dictionary<CosObjectKey, long> xrefOffset)
        {
            if (xrefOffset == null)
            {
                return true;
            }
            foreach (var objectEntry in xrefOffset)
            {
                CosObjectKey objectKey = objectEntry.Key;
                long objectOffset = objectEntry.Value;
                // a negative offset number represents a object number itself
                // see type 2 entry in xref stream
                if (objectOffset != null && objectOffset >= 0
                        && !checkObjectKeys(reader, objectKey, objectOffset))
                {
                    //LOG.debug("Stop checking xref offsets as at least one (" + objectKey
                    //        + ") couldn't be dereferenced");
                    return false;
                }
            }
            return true;
        }

        private bool checkObjectKeys(IRandomAccessRead source, CosObjectKey objectKey, long offset)
        {
            // there can't be any object at the very beginning of a pdf
            if (offset < MINIMUM_SEARCH_OFFSET)
            {
                return false;
            }
            long objectNr = objectKey.Number;
            long objectGen = objectKey.Generation;
            long originOffset = source.GetPosition();
            string objectString = ObjectHelper.createObjectString(objectNr, objectGen);
            try
            {
                source.Seek(offset);
                if (ReadHelper.IsString(source, OtherEncodings.StringAsLatin1Bytes(objectString)))
                {
                    // everything is ok, return origin object key
                    source.Seek(originOffset);
                    return true;
                }
            }
            catch (InvalidOperationException exception)
            {
                // Swallow the exception, obviously there isn't any valid object number
            }
            finally
            {
                source.Seek(originOffset);
            }
            // no valid object number found
            return false;
        }


        private Dictionary<CosObjectKey, long> getBFCosObjectOffsets(IRandomAccessRead reader)
        {
            if (bfSearchCOSObjectKeyOffsets == null)
            {
                bfSearchForObjects(reader);
            }
            return bfSearchCOSObjectKeyOffsets;
        }

        private void bfSearchForObjects(IRandomAccessRead source)
        {
            bfSearchForLastEOFMarker(source);
            bfSearchCOSObjectKeyOffsets = new Dictionary<CosObjectKey, long>();
            long originOffset = source.GetPosition();
            long currentOffset = MINIMUM_SEARCH_OFFSET;
            long lastObjectId = long.MinValue;
            int lastGenID = int.MinValue;
            long lastObjOffset = long.MinValue;
            char[] objString = " obj".ToCharArray();
            char[] endobjString = "endobj".ToCharArray();
            bool endobjFound = false;
            do
            {
                source.Seek(currentOffset);
                if (ReadHelper.IsString(source, "obj"))
                {
                    long tempOffset = currentOffset - 1;
                    source.Seek(tempOffset);
                    int genID = source.Peek();
                    // is the next char a digit?
                    if (ReadHelper.IsDigit(genID))
                    {
                        genID -= 48;
                        tempOffset--;
                        source.Seek(tempOffset);
                        if (ReadHelper.IsSpace(source))
                        {
                            while (tempOffset > MINIMUM_SEARCH_OFFSET && ReadHelper.IsSpace(source))
                            {
                                source.Seek(--tempOffset);
                            }
                            bool objectIDFound = false;
                            while (tempOffset > MINIMUM_SEARCH_OFFSET && ReadHelper.IsDigit(source))
                            {
                                source.Seek(--tempOffset);
                                objectIDFound = true;
                            }
                            if (objectIDFound)
                            {
                                source.Read();
                                long objectId = ObjectHelper.ReadObjectNumber(source);
                                if (lastObjOffset > 0)
                                {
                                    // add the former object ID only if there was a subsequent object ID
                                    bfSearchCOSObjectKeyOffsets[new CosObjectKey(lastObjectId, lastGenID)] = lastObjOffset;
                                }
                                lastObjectId = objectId;
                                lastGenID = genID;
                                lastObjOffset = tempOffset + 1;
                                currentOffset += objString.Length - 1;
                                endobjFound = false;
                            }
                        }
                    }
                }
                else if (ReadHelper.IsString(source, "endobj"))
                {
                    endobjFound = true;
                    currentOffset += endobjString.Length - 1;
                }
                currentOffset++;
            } while (currentOffset < lastEOFMarker && !source.IsEof());
            if ((lastEOFMarker < long.MaxValue || endobjFound) && lastObjOffset > 0)
            {
                // if the pdf wasn't cut off in the middle or if the last object ends with a "endobj" marker
                // the last object id has to be added here so that it can't get lost as there isn't any subsequent object id
                bfSearchCOSObjectKeyOffsets[new CosObjectKey(lastObjectId, lastGenID)] = lastObjOffset;
            }
            // reestablish origin position

            source.Seek(originOffset);
        }

        /**
         * Check the XRef table by dereferencing all objects and fixing the offset if necessary.
         * 
         * @throws InvalidOperationException if something went wrong.
         */
        public void checkXrefOffsets(IRandomAccessRead reader, CrossReferenceTable xrefTrailerResolver, bool isLenientParsing)
        {
            // repair mode isn't available in non-lenient mode
            if (!isLenientParsing)
            {
                return;
            }
            Dictionary<CosObjectKey, long> xrefOffset = xrefTrailerResolver.ObjectOffsets.ToDictionary(x => x.Key, x => x.Value);
            if (validateXrefOffsets(reader, xrefOffset))
            {
                return;
            }

            Dictionary<CosObjectKey, long> bfCOSObjectKeyOffsets = getBFCosObjectOffsets(reader);
            if (bfCOSObjectKeyOffsets.Count > 0)
            {
                List<CosObjectKey> objStreams = new List<CosObjectKey>();
                // find all object streams
                foreach (var entry in xrefOffset)
                {
                    long offset = entry.Value;
                    if (offset != null && offset < 0)
                    {
                        CosObjectKey objStream = new CosObjectKey(-offset, 0);
                        if (!objStreams.Contains(objStream))
                        {
                            objStreams.Add(new CosObjectKey(-offset, 0));
                        }
                    }
                }
                // remove all found object streams
                if (objStreams.Count > 0)
                {
                    foreach (CosObjectKey key in objStreams)
                    {
                        if (bfCOSObjectKeyOffsets.ContainsKey(key))
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

                foreach (var item in bfCOSObjectKeyOffsets)
                {
                    xrefOffset.Add(item.Key, item.Value);
                }

            }
        }

        private long? lastEOFMarker = null;
        private void bfSearchForLastEOFMarker(IRandomAccessRead source)
        {
            if (lastEOFMarker == null)
            {
                long originOffset = source.GetPosition();
                source.Seek(MINIMUM_SEARCH_OFFSET);
                while (!source.IsEof())
                {
                    // search for EOF marker
                    if (ReadHelper.IsString(source, "%%EOF"))
                    {
                        long tempMarker = source.GetPosition();
                        source.Seek(tempMarker + 5);
                        try
                        {
                            // check if the following data is some valid pdf content
                            // which most likely indicates that the pdf is linearized,
                            // updated or just cut off somewhere in the middle
                            ReadHelper.SkipSpaces(source);
                            ObjectHelper.ReadObjectNumber(source);
                            ObjectHelper.ReadGenerationNumber(source);
                        }
                        catch (InvalidOperationException exception)
                        {
                            // save the EOF marker as the following data is most likely some garbage
                            lastEOFMarker = tempMarker;
                        }
                    }
                    source.Read();
                }
                source.Seek(originOffset);
                // no EOF marker found
                if (lastEOFMarker == null)
                {
                    lastEOFMarker = long.MaxValue;
                }
            }
        }
    }
}
