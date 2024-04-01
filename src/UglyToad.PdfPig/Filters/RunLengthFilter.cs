namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// The Run Length filterencodes data in a simple byte-oriented format based on run length.
    /// The encoded data is a sequence of runs, where each run consists of a length byte followed by 1 to 128 bytes of data.
    /// </summary>
    internal class RunLengthFilter : IFilter
    {
        private const byte EndOfDataLength = 128;

        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public byte[] Decode(ReadOnlyMemory<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            var inputSpan = input.Span;
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                var i = 0;
                while (i < inputSpan.Length)
                {
                    var runLength = inputSpan[i];

                    if (runLength == EndOfDataLength)
                    {
                        break;
                    }

                    // if length byte in range 0 - 127 copy the following length + 1 bytes literally to the output.
                    if (runLength <= 127)
                    {
                        var rangeToWriteLiterally = runLength + 1;

                        while (rangeToWriteLiterally > 0)
                        {
                            i++;

                            writer.Write(inputSpan[i]);

                            rangeToWriteLiterally--;
                        }

                        // Move to the following byte.
                        i++;
                    }
                    // Otherwise copy the single following byte 257 - length times (between 2 - 128 times)
                    else
                    {
                        var numberOfTimesToCopy = 257 - runLength;

                        var byteToCopy = inputSpan[i + 1];

                        for (int j = 0; j < numberOfTimesToCopy; j++)
                        {
                            writer.Write(byteToCopy);
                        }

                        // Move to the single byte after the byte to copy.
                        i += 2;
                    }
                }

                writer.Flush();

                return memoryStream.ToArray();
            }
        }
    }
}
