namespace UglyToad.PdfPig.Filters
{
    using System.IO;
    using Tokens;

    internal class RunLengthFilter : IFilter
    {
        private const byte EndOfDataLength = 128;

        public byte[] Decode(byte[] input, DictionaryToken streamDictionary, int filterIndex)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
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

                            writer.Write(input[i]);

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
