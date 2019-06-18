namespace UglyToad.PdfPig.Parser.Parts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using IO;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Store the results of a brute force search for all objects in the document so we only do it once.
    /// </summary>
    internal class BruteForceSearcher
    {
        private const int MinimumSearchOffset = 6;

        private readonly IInputBytes bytes;

        private Dictionary<IndirectReference, long> objectLocations;

        public BruteForceSearcher([NotNull] IInputBytes bytes)
        {
            this.bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        }

        [NotNull]
        public IReadOnlyDictionary<IndirectReference, long> GetObjectLocations()
        {
            if (objectLocations != null)
            {
                return objectLocations;
            }

            var lastEndOfFile = GetLastEndOfFileMarker();

            var results = new Dictionary<IndirectReference, long>();

            var originPosition = bytes.CurrentOffset;

            long currentOffset = MinimumSearchOffset;
            long lastObjectId = long.MinValue;
            int lastGenerationId = int.MinValue;
            long lastObjOffset = long.MinValue;

            bool inObject = false;
            bool endobjFound = false;
            do
            {
                if (inObject)
                {
                    if (bytes.CurrentByte == 'e')
                    {
                        var next = bytes.Peek();

                        if (next.HasValue && next == 'n')
                        {
                            if (ReadHelper.IsString(bytes, "endobj"))
                            {
                                inObject = false;
                                endobjFound = true;

                                for (int i = 0; i < "endobj".Length; i++)
                                {
                                    bytes.MoveNext();
                                    currentOffset++;
                                }
                            }
                            else
                            {
                                bytes.MoveNext();
                                currentOffset++;
                            }
                        }
                        else
                        {
                            bytes.MoveNext();
                            currentOffset++;
                        }
                    }
                    else
                    {
                        bytes.MoveNext();
                        currentOffset++;
                    }

                    continue;
                }

                bytes.Seek(currentOffset);

                if (!ReadHelper.IsString(bytes, " obj"))
                {
                    currentOffset++;
                    continue;
                }

                // Current byte is ' '[obj]
                var offset = currentOffset - 1;

                bytes.Seek(offset);

                var generationBytes = new StringBuilder();
                while (ReadHelper.IsDigit(bytes.CurrentByte) && offset >= MinimumSearchOffset)
                {
                    generationBytes.Insert(0, (char)bytes.CurrentByte);
                    offset--;
                    bytes.Seek(offset);
                }

                // We should now be at the space between object and generation number.
                if (!ReadHelper.IsSpace(bytes.CurrentByte))
                {
                    continue;
                }

                bytes.Seek(--offset);

                var objectNumberBytes = new StringBuilder();
                while (ReadHelper.IsDigit(bytes.CurrentByte) && offset >= MinimumSearchOffset)
                {
                    objectNumberBytes.Insert(0, (char)bytes.CurrentByte);
                    offset--;
                    bytes.Seek(offset);
                }

                if (!ReadHelper.IsWhitespace(bytes.CurrentByte))
                {
                    continue;
                }

                var obj = long.Parse(objectNumberBytes.ToString(), CultureInfo.InvariantCulture);
                var generation = int.Parse(generationBytes.ToString(), CultureInfo.InvariantCulture);

                results[new IndirectReference(obj, generation)] = bytes.CurrentOffset + 1;

                inObject = true;
                endobjFound = false;

                currentOffset++;

                bytes.Seek(currentOffset);
            } while (currentOffset < lastEndOfFile && !bytes.IsAtEnd());

            if ((lastEndOfFile < long.MaxValue || endobjFound) && lastObjOffset > 0)
            {
                // if the pdf wasn't cut off in the middle or if the last object ends with a "endobj" marker
                // the last object id has to be added here so that it can't get lost as there isn't any subsequent object id
                results[new IndirectReference(lastObjectId, lastGenerationId)] = lastObjOffset;
            }

            // reestablish origin position
            bytes.Seek(originPosition);

            objectLocations = results;

            return objectLocations;
        }

        private long GetLastEndOfFileMarker()
        {
            var originalOffset = bytes.CurrentOffset;

            const string searchTerm = "%%EOF";

            var minimumEndOffset = bytes.Length - searchTerm.Length;

            bytes.Seek(minimumEndOffset);

            while (bytes.CurrentOffset > 0)
            {
                if (ReadHelper.IsString(bytes, searchTerm))
                {
                    var position = bytes.CurrentOffset;

                    bytes.Seek(originalOffset);

                    return position;
                }

                bytes.Seek(minimumEndOffset--);
            }

            bytes.Seek(originalOffset);
            return long.MaxValue;
        }
    }
}
