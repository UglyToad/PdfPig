namespace UglyToad.PdfPig.Filters
{
    using System;
    using Core;
    using Tokens;

    /// <summary>
    /// ASCII 85 (Base85) is a binary to text encoding using 5 ASCII characters per 4 bytes of data.
    /// </summary>
    public sealed class Ascii85Filter : IFilter
    {
        private const byte EmptyBlock = (byte)'z';
        private const byte Offset = (byte)'!';
        private const byte EmptyCharacterPadding = (byte) 'u';

        private static ReadOnlySpan<byte> EndOfDataBytes => [(byte)'~', (byte)'>'];

        private static readonly int[] PowerByIndex = [
            1,
            85,
            85 * 85,
            85 * 85 * 85,
            85 * 85 * 85 *85
        ];

        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public ReadOnlyMemory<byte> Decode(ReadOnlySpan<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            Span<byte> asciiBuffer = stackalloc byte[5];

            var index = 0;

            using var writer = new ArrayPoolBufferWriter<byte>();
           
            for (var i = 0; i < input.Length; i++)
            {
                var value = input[i];

                if (IsWhiteSpace(value))
                {
                    continue;
                }

                if (value == EndOfDataBytes[0])
                {
                    if (i == input.Length - 1 || input[i + 1] == EndOfDataBytes[1])
                    {
                        if (index > 0)
                        {
                            WriteData(asciiBuffer, index, writer);
                        }

                        index = 0;

                        // The end
                        break;
                    }

                    // TODO: this shouldn't be possible?
                }

                if (value == EmptyBlock)
                {
                    if (index > 0)
                    {
                        throw new InvalidOperationException("Encountered z within a 5 character block");
                    }

                    for (int j = 0; j < 4; j++)
                    {
                        writer.Write(0);
                    }

                    index = 0;

                    // We've completed our block.
                }
                else
                {
                    asciiBuffer[index] = (byte) (value - Offset);
                    index++;
                }

                if (index == 5)
                {
                    WriteData(asciiBuffer, index, writer);
                    index = 0;
                }
            }

            if (index > 0)
            {
                WriteData(asciiBuffer, index, writer);
            }

            return writer.WrittenMemory.ToArray();
        }

        private static void WriteData(Span<byte> ascii, int index, ArrayPoolBufferWriter<byte> writer)
        {
            if (index < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Cannot convert a block padded by 4 'u' characters.");
            }

            // Write any empty padding if the block ended early.
            for (var i = index; i < 5; i++)
            {
                ascii[i] = EmptyCharacterPadding - Offset;
            }
            
            int value = 0;
            value += ascii[0] * PowerByIndex[4];
            value += ascii[1] * PowerByIndex[3];
            value += ascii[2] * PowerByIndex[2];
            value += ascii[3] * PowerByIndex[1];
            value += ascii[4] * PowerByIndex[0];

            writer.Write((byte)(value >> 24));

            if (index > 2)
            {
                writer.Write((byte) (value >> 16));
            }

            if (index > 3)
            {
                writer.Write((byte) (value >> 8));
            }

            if (index > 4)
            {
                writer.Write((byte) value);
            }
        }

        private static bool IsWhiteSpace(byte b)
        {
            if (b == '\r' || b == '\n' || b == ' ')
            {
                return true;
            }

            return false;
        }
    }
}
