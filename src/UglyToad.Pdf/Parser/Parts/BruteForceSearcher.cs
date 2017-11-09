namespace UglyToad.Pdf.Parser.Parts
{
    using System;
    using System.Collections.Generic;
    using Cos;
    using IO;
    using Util;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Store the results of a brute force search for all Cos Objects in the document so we only do it once.
    /// </summary>
    public class BruteForceSearcher
    {
        private const int MinimumSearchOffset = 6;

        private readonly IRandomAccessRead reader;

        private Dictionary<CosObjectKey, long> objectLocations;

        public BruteForceSearcher([NotNull] IRandomAccessRead reader)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        [NotNull]
        public IReadOnlyDictionary<CosObjectKey, long> GetObjectLocations()
        {
            if (objectLocations != null)
            {
                return objectLocations;
            }

            var lastEndOfFile = GetLastEndOfFileMarker();

            var results = new Dictionary<CosObjectKey, long>();

            var originPosition = reader.GetPosition();

            long currentOffset = MinimumSearchOffset;
            long lastObjectId = long.MinValue;
            int lastGenerationId = int.MinValue;
            long lastObjOffset = long.MinValue;
            byte[] objString = OtherEncodings.StringAsLatin1Bytes(" obj");
            byte[] endobjString = OtherEncodings.StringAsLatin1Bytes("endobj");

            bool endobjFound = false;
            do
            {
                reader.Seek(currentOffset);
                if (ReadHelper.IsString(reader, objString))
                {
                    long tempOffset = currentOffset - 1;
                    reader.Seek(tempOffset);
                    int generationId = reader.Peek();

                    // is the next char a digit?
                    if (ReadHelper.IsDigit(generationId))
                    {
                        generationId -= 48;
                        tempOffset--;
                        reader.Seek(tempOffset);
                        if (ReadHelper.IsSpace(reader))
                        {
                            while (tempOffset > MinimumSearchOffset && ReadHelper.IsSpace(reader))
                            {
                                reader.Seek(--tempOffset);
                            }

                            bool objectIdFound = false;
                            while (tempOffset > MinimumSearchOffset && ReadHelper.IsDigit(reader))
                            {
                                reader.Seek(--tempOffset);
                                objectIdFound = true;
                            }

                            if (objectIdFound)
                            {
                                reader.Read();
                                long objectId = ObjectHelper.ReadObjectNumber(reader);
                                if (lastObjOffset > 0)
                                {
                                    // add the former object ID only if there was a subsequent object ID
                                    results[new CosObjectKey(lastObjectId, lastGenerationId)] = lastObjOffset;
                                }
                                lastObjectId = objectId;
                                lastGenerationId = generationId;
                                lastObjOffset = tempOffset + 1;
                                currentOffset += objString.Length - 1;
                                endobjFound = false;
                            }
                        }
                    }
                }
                else if (ReadHelper.IsString(reader, "endobj"))
                {
                    endobjFound = true;
                    currentOffset += endobjString.Length - 1;
                }
                currentOffset++;
            } while (currentOffset < lastEndOfFile && !reader.IsEof());
            if ((lastEndOfFile < long.MaxValue || endobjFound) && lastObjOffset > 0)
            {
                // if the pdf wasn't cut off in the middle or if the last object ends with a "endobj" marker
                // the last object id has to be added here so that it can't get lost as there isn't any subsequent object id
                results[new CosObjectKey(lastObjectId, lastGenerationId)] = lastObjOffset;
            }

            // reestablish origin position
            reader.Seek(originPosition);

            objectLocations = results;

            return objectLocations;
        }

        private long GetLastEndOfFileMarker()
        {
            var originalOffset = reader.GetPosition();

            var searchTerm = OtherEncodings.StringAsLatin1Bytes("%%EOF");

            var minimumEndOffset = reader.Length() - searchTerm.Length;

            reader.Seek(minimumEndOffset);

            while (reader.GetPosition() > 0)
            {
                if (ReadHelper.IsString(reader, searchTerm))
                {
                    var position = reader.GetPosition();
                    reader.Seek(originalOffset);
                    return position;
                }

                reader.Seek(minimumEndOffset--);
            }

            reader.Seek(originalOffset);
            return long.MaxValue;
        }
    }
}
