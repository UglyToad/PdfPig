namespace UglyToad.PdfPig.Parser.Parts
{
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Brute force search for all objects in the document.
    /// </summary>
    internal static class BruteForceSearcher
    {
        private const int MinimumSearchOffset = 6;

        /// <summary>
        /// Find the offset of every object contained in the document by searching the entire document contents.
        /// </summary>
        /// <param name="bytes">The bytes of the document.</param>
        /// <returns>The object keys and offsets for the objects in this document.</returns>
        [NotNull]
        public static IReadOnlyDictionary<IndirectReference, long> GetObjectLocations(IInputBytes bytes)
        {
            if (bytes is null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            var loopProtection = 0;

            var lastEndOfFile = GetLastEndOfFileMarker(bytes);

            var results = new Dictionary<IndirectReference, long>();

            var generationBytes = new StringBuilder();
            var objectNumberBytes = new StringBuilder();

            var originPosition = bytes.CurrentOffset;

            var currentOffset = (long)MinimumSearchOffset;

            var currentlyInObject = false;

            var objBuffer = new byte[4];

            do
            {
                if (loopProtection > 10_000_000)
                {
                    throw new PdfDocumentFormatException("Failed to brute-force search the file due to an infinite loop.");
                }

                loopProtection++;

                if (currentlyInObject)
                {
                    if (bytes.CurrentByte == 'e')
                    {
                        var next = bytes.Peek();

                        if (next.HasValue && next == 'n')
                        {
                            if (ReadHelper.IsString(bytes, "endobj"))
                            {
                                currentlyInObject = false;
                                loopProtection = 0;

                                for (var i = 0; i < "endobj".Length; i++)
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
                        loopProtection = 0;
                    }

                    continue;
                }

                bytes.Seek(currentOffset);

                bytes.Read(objBuffer);

                if (!IsStartObjMarker(objBuffer))
                {
                    currentOffset++;
                    continue;
                }

                // Current byte is ' '[obj]
                var offset = currentOffset + 1;

                bytes.Seek(offset);

                while (ReadHelper.IsWhitespace(bytes.CurrentByte) && offset >= MinimumSearchOffset)
                {
                    bytes.Seek(--offset);
                }

                while (ReadHelper.IsDigit(bytes.CurrentByte) && offset >= MinimumSearchOffset)
                {
                    generationBytes.Insert(0, (char)bytes.CurrentByte);
                    offset--;
                    bytes.Seek(offset);
                }

                // We should now be at the space between object and generation number.
                if (!ReadHelper.IsWhitespace(bytes.CurrentByte))
                {
                    currentOffset++;
                    continue;
                }

                while (ReadHelper.IsWhitespace(bytes.CurrentByte))
                {
                    bytes.Seek(--offset);
                }

                while (ReadHelper.IsDigit(bytes.CurrentByte) && offset >= MinimumSearchOffset)
                {
                    objectNumberBytes.Insert(0, (char)bytes.CurrentByte);
                    offset--;
                    bytes.Seek(offset);
                }

                if (objectNumberBytes.Length == 0 || generationBytes.Length == 0)
                {
                    generationBytes.Clear();
                    objectNumberBytes.Clear();
                    currentOffset++;
                    continue;
                }

                var obj = long.Parse(objectNumberBytes.ToString(), CultureInfo.InvariantCulture);
                var generation = int.Parse(generationBytes.ToString(), CultureInfo.InvariantCulture);

                results[new IndirectReference(obj, generation)] = bytes.CurrentOffset;

                generationBytes.Clear();
                objectNumberBytes.Clear();

                currentlyInObject = true;

                currentOffset++;

                bytes.Seek(currentOffset);
                loopProtection = 0;
            } while (currentOffset < lastEndOfFile && !bytes.IsAtEnd());
            
            // reestablish origin position
            bytes.Seek(originPosition);
            
            return results;
        }

        private static long GetLastEndOfFileMarker(IInputBytes bytes)
        {
            var originalOffset = bytes.CurrentOffset;

            const string searchTerm = "%%EOF";

            var minimumEndOffset = bytes.Length - searchTerm.Length + 1; // Issue #512 - Unable to open PDF - BruteForceScan starts from earlier of two EOF marker due to min end offset off by 1

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

        private static bool IsStartObjMarker(ReadOnlySpan<byte> data)
        {
            if (!ReadHelper.IsWhitespace(data[0]))
            {
                return false;
            }

            return (data[1] == 'o' || data[1] == 'O')
                   && (data[2] == 'b' || data[2] == 'B')
                   && (data[3] == 'j' || data[3] == 'J');
        }
    }
}
