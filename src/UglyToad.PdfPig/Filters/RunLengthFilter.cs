namespace UglyToad.PdfPig.Filters
{
    using System;
    using Tokens;
    using UglyToad.PdfPig.Core;

    /// <inheritdoc />
    /// <summary>
    /// The Run Length filterencodes data in a simple byte-oriented format based on run length.
    /// The encoded data is a sequence of runs, where each run consists of a length byte followed by 1 to 128 bytes of data.
    /// </summary>
    internal sealed class RunLengthFilter : IFilter
    {
        private const byte EndOfDataLength = 128;

        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public byte[] Decode(ReadOnlySpan<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            using var output = new ArrayPoolBufferWriter<byte>(input.Length);

            var i = 0;
            while (i < input.Length)
            {
                var runLength = input[i];

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

                        output.Write(input[i]);

                        rangeToWriteLiterally--;
                    }

                    // Move to the following byte.
                    i++;
                }
                // Otherwise copy the single following byte 257 - length times (between 2 - 128 times)
                else
                {
                    var numberOfTimesToCopy = 257 - runLength;

                    var byteToCopy = input[i + 1];

                    for (int j = 0; j < numberOfTimesToCopy; j++)
                    {
                        output.Write(byteToCopy);
                    }

                    // Move to the single byte after the byte to copy.
                    i += 2;
                }
            }

            return output.WrittenSpan.ToArray();
        }
    }
}