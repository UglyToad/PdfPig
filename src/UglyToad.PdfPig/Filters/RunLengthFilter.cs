namespace UglyToad.PdfPig.Filters
{
    using System;
    using Tokens;
    using Core;

    /// <summary>
    /// The Run Length filter encodes data in a simple byte-oriented format based on run length.
    /// The encoded data is a sequence of runs, where each run consists of a length byte followed by 1 to 128 bytes of data.
    /// </summary>
    public sealed class RunLengthFilter : IFilter
    {
        private const byte EndOfDataLength = 128;

        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public Memory<byte> Decode(Memory<byte> input, DictionaryToken streamDictionary, IFilterProvider filterProvider, int filterIndex)
        {
            using var output = new ArrayPoolBufferWriter<byte>(input.Length);

            Span<byte> inputSpan = input.Span;
            
            var i = 0;
            while (i < input.Length)
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

                        output.Write(inputSpan[i]);

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

                    output.GetSpan(numberOfTimesToCopy).Slice(0, numberOfTimesToCopy).Fill(byteToCopy);
                    output.Advance(numberOfTimesToCopy);

                    // Move to the single byte after the byte to copy.
                    i += 2;
                }
            }

            return output.WrittenMemory.ToArray();
        }
    }
}