namespace UglyToad.PdfPig.Writer.Fonts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using IO;
    using PdfPig.Fonts.TrueType;
    using PdfPig.Fonts.TrueType.Parser;

    internal static class TrueTypeCMapReplacer
    {
        private const int SizeOfFraction = 4;
        private const int SizeOfShort = 2;
        private const int SizeOfTag = 4;
        private const int SizeOfInt = 4;

        private const string CMapTag = "cmap";
        private const string HeadTag = "head";

        public static byte[] ReplaceCMapTables(TrueTypeFontProgram fontProgram, IInputBytes fontBytes, IReadOnlyDictionary<char, byte> newEncoding)
        {
            if (fontBytes == null)
            {
                throw new ArgumentNullException(nameof(fontBytes));
            }

            if (newEncoding == null)
            {
                throw new ArgumentNullException(nameof(newEncoding));
            }

            var buffer = new byte[2048];

            var inputTableHeaders = new Dictionary<string, InputHeader>(StringComparer.OrdinalIgnoreCase);
            var outputTableHeaders = new Dictionary<string, TrueTypeHeaderTable>(StringComparer.OrdinalIgnoreCase);
            
            var fileChecksumOffset = SizeOfTag;

            byte[] result;

            using (var stream = new MemoryStream())
            {
                // Write the file header details and read the number of tables out.
                CopyThroughBufferPreserveData(stream, buffer, fontBytes, SizeOfFraction + (SizeOfShort * 4));

                var numberOfTables = ReadUShortFromBuffer(buffer, SizeOfFraction);

                // For each table read the table header values and preserve the order by storing the offset in the input file
                // at which the the header was read.
                for (var i = 0; i < numberOfTables; i++)
                {
                    var offsetOfHeader = (uint)stream.Position;

                    CopyThroughBufferPreserveData(stream, buffer, fontBytes, SizeOfTag + (SizeOfInt * 3));

                    var tag = Encoding.UTF8.GetString(buffer, 0, SizeOfTag);

                    var checksum = ReadUIntFromBuffer(buffer, fileChecksumOffset);
                    var offset = ReadUIntFromBuffer(buffer, SizeOfTag + SizeOfInt);
                    var length = ReadUIntFromBuffer(buffer, SizeOfTag + (SizeOfInt * 2));

                    var headerTable = new TrueTypeHeaderTable(tag, checksum, offset, length);

                    // Store the locations of the tables in this font.
                    inputTableHeaders[tag] = new InputHeader(headerTable, offsetOfHeader);
                }

                // Copy raw bytes for each of the tables from the input to the output including any additional bytes not in
                // tables but present in the input.
                var inputOffset = fontBytes.CurrentOffset;

                foreach (var inputHeader in inputTableHeaders.OrderBy(x => x.Value.HeaderTable.Offset))
                {
                    var location = inputHeader.Value.HeaderTable;

                    var gapFromPrevious = location.Offset - inputOffset;

                    if (gapFromPrevious > 0)
                    {
                        CopyThroughBufferDiscardData(stream, buffer, fontBytes, gapFromPrevious);
                    }

                    if (inputHeader.Value.IsTable(CMapTag))
                    {
                        // Skip the CMap table for now, move it to the end in the output so we can resize it dynamically.
                        inputOffset = location.Offset + location.Length;
                        fontBytes.Seek(inputOffset);

                        continue;
                    }

                    var outputOffset = (uint)stream.Position;

                    outputTableHeaders[location.Tag] = new TrueTypeHeaderTable(location.Tag, 0, outputOffset, location.Length);

                    CopyThroughBufferDiscardData(stream, buffer, fontBytes, location.Length);

                    var writtenLength = stream.Position - outputOffset;

                    if (writtenLength != location.Length)
                    {
                        throw new InvalidOperationException($"Expected to write {location.Length} bytes for table {location.Tag} " +
                                                            $"but wrote {stream.Position - outputOffset}.");
                    }

                    inputOffset = fontBytes.CurrentOffset;
                }

                // Create a new cmap table here.
                var table = GenerateWindowsSymbolTable(fontProgram, newEncoding);
                var cmapLocation = inputTableHeaders[CMapTag];

                fontBytes.Seek(cmapLocation.HeaderTable.Offset);

                var newCmapTableLocation = (uint)stream.Position;
                var newCmapTableLength = (uint)table.Length;
                CopyThroughBufferDiscardData(stream, buffer, new ByteArrayInputBytes(table), newCmapTableLength);

                outputTableHeaders[cmapLocation.Tag] = new TrueTypeHeaderTable(cmapLocation.Tag, 0, newCmapTableLocation, newCmapTableLength);

                foreach (var inputHeader in inputTableHeaders)
                {
                    // Go back to the location of the offset
                    var headerOffsetLocation = inputHeader.Value.OffsetInInput + SizeOfTag + SizeOfInt;
                    stream.Seek(headerOffsetLocation, SeekOrigin.Begin);

                    var outputHeader = outputTableHeaders[inputHeader.Key];

                    var inputLength = inputHeader.Value.HeaderTable.Length;

                    var isCmap = inputHeader.Value.IsTable(CMapTag);

                    if (outputHeader.Length != inputLength && !isCmap)
                    {
                        throw new InvalidOperationException($"Actual data length {outputHeader.Length} " +
                                                            $"did not match header length {inputLength} for table {inputHeader.Key}.");
                    }

                    WriteUInt(stream, outputHeader.Offset);

                    if (isCmap)
                    {
                        // Also overwrite length.
                        WriteUInt(stream, outputHeader.Length);
                    }
                }

                stream.Seek(0, SeekOrigin.Begin);

                // Done writing to stream, just checksums left to repair.
                result = stream.ToArray();
            }

            var inputBytes = new ByteArrayInputBytes(result);
            
            // Overwrite checksum values per table.
            foreach (var inputHeader in inputTableHeaders)
            {
                var outputHeader = outputTableHeaders[inputHeader.Key];

                var headerOffset = inputHeader.Value.OffsetInInput;
                
                var newChecksum = TrueTypeChecksumCalculator.Calculate(inputBytes, outputHeader);

                // Overwrite the checksum value.
                WriteUInt(result, headerOffset + SizeOfTag, newChecksum);
            }

            // Overwrite the checksum adjustment which records the whole font checksum.
            var headTable = outputTableHeaders[HeadTag];
            var wholeFontChecksum = TrueTypeChecksumCalculator.CalculateWholeFontChecksum(inputBytes, headTable);

            // Calculate the checksum for the entire font and subtract the value from the hex value B1B0AFBA.
            var checksumAdjustmentLocation = headTable.Offset + 8;
            var checksumAdjustment = 0xB1B0AFBA - wholeFontChecksum;

            // Store the result in checksum adjustment.
            WriteUInt(result, checksumAdjustmentLocation, checksumAdjustment);
            
            var canParse = new TrueTypeFontParser().Parse(new TrueTypeDataBytes(new ByteArrayInputBytes(result)));

            return result;
        }

        private static ushort ReadUShortFromBuffer(byte[] buffer, int location)
        {
            return (ushort)((buffer[location] << 8) + (buffer[location + 1] << 0));
        }

        private static uint ReadUIntFromBuffer(byte[] buffer, int location)
        {
            return (uint)(((long)buffer[location] << 24)
                   + ((long)buffer[location + 1] << 16)
                   + (buffer[location + 2] << 8)
                   + (buffer[location + 3] << 0));
        }

        private static void WriteUInt(Stream stream, uint value)
        {
            var buffer = new[]
            {
                (byte) (value >> 24),
                (byte) (value >> 16),
                (byte) (value >> 8),
                (byte) value
            };

            stream.Write(buffer, 0, 4);
        }

        private static void WriteUShort(Stream stream, ushort value)
        {
            var buffer = new[]
            {
                (byte) (value >> 8),
                (byte) value
            };

            stream.Write(buffer, 0, 2);
        }

        private static void WriteUInt(byte[] array, uint offset, uint value)
        {
            array[offset] = (byte)(value >> 24);
            array[offset + 1] = (byte)(value >> 16);
            array[offset + 2] = (byte)(value >> 8);
            array[offset + 3] = (byte)(value >> 0);
        }

        private static void CopyThroughBufferDiscardData(Stream destination, byte[] buffer, IInputBytes input, long size)
        {
            var filled = 0;
            while (filled < size)
            {
                var expected = (int)Math.Min(size - filled, 2048);

                var read = input.Read(buffer, expected);

                if (read != expected)
                {
                    throw new InvalidOperationException($"Failed to read {size} bytes starting at offset {input.CurrentOffset - read}.");
                }

                destination.Write(buffer, 0, read);

                filled += read;
            }
        }

        /// <summary>
        /// Copies data from the input to the destination stream while also populating the buffer with the full
        /// run of copied data in the buffer from position 0 -> size.
        /// </summary>
        private static void CopyThroughBufferPreserveData(Stream destination, byte[] buffer, IInputBytes input, int size)
        {
            if (size > buffer.Length)
            {
                throw new InvalidOperationException("Cannot use this method to read more bytes than fit in the buffer.");
            }

            var read = input.Read(buffer, size);
            if (read != size)
            {
                throw new InvalidOperationException($"Failed to read {size} bytes starting at offset {input.CurrentOffset - read}.");
            }

            destination.Write(buffer, 0, read);
        }

        private static byte[] GenerateWindowsSymbolTable(TrueTypeFontProgram font, IReadOnlyDictionary<char, byte> newEncoding)
        {
            // We generate a format 6 sub-table.
            const ushort cmapVersion = 0;
            const ushort numberOfSubtables = 1;
            const ushort platformId = 3;
            const ushort encodingId = 0;
            const ushort format = 6;
            const ushort languageId = 0;

            var glyphIndices = MapNewEncodingToGlyphIndexArray(font, newEncoding);

            using (var memoryStream = new MemoryStream())
            {
                // Write cmap table header.
                WriteUShort(memoryStream, cmapVersion);
                WriteUShort(memoryStream, numberOfSubtables);

                // Write sub-table index.
                WriteUShort(memoryStream, platformId);
                WriteUShort(memoryStream, encodingId);
                WriteUInt(memoryStream, (uint)(memoryStream.Position + SizeOfInt));

                // Write format 6 sub-table.
                WriteUShort(memoryStream, format);
                var length = (ushort)((5 * SizeOfShort) + (SizeOfShort * glyphIndices.Length));
                WriteUShort(memoryStream, length);
                WriteUShort(memoryStream, languageId);
                WriteUShort(memoryStream, 0);
                WriteUShort(memoryStream, (ushort)glyphIndices.Length);

                for (var j = 0; j < glyphIndices.Length; j++)
                {
                    WriteUShort(memoryStream, glyphIndices[j]);
                }

                return memoryStream.ToArray();
            }
        }
        
        private static ushort[] MapNewEncodingToGlyphIndexArray(TrueTypeFontProgram font, IReadOnlyDictionary<char, byte> newEncoding)
        {
            var mappingTable = font.WindowsUnicodeCMap ?? font.WindowsSymbolCMap;

            if (mappingTable == null)
            {
                throw new InvalidOperationException();
            }

            var first = default(ushort?);
            var glyphIndices = new ushort[newEncoding.Count + 1];
            glyphIndices[0] = 0;
            var i = 1;
            foreach (var pair in newEncoding.OrderBy(x => x.Value))
            {
                if (first.HasValue && pair.Value - first.Value != 1)
                {
                    throw new InvalidOperationException("The new encoding contained a gap.");
                }

                first = pair.Value;

                // this must be the actual glyph index from the original cmap table.
                glyphIndices[i++] = (ushort)mappingTable.CharacterCodeToGlyphIndex(pair.Key);
            }

            if (!first.HasValue)
            {
                throw new InvalidOperationException();
            }

            return glyphIndices;
        }

        private class InputHeader
        {
            public string Tag => HeaderTable.Tag;

            public TrueTypeHeaderTable HeaderTable { get; }

            public uint OffsetInInput { get; }

            public InputHeader(TrueTypeHeaderTable headerTable, uint offsetInInput)
            {
                if (headerTable.Tag == null)
                {
                    throw new ArgumentException($"No tag for header table: {HeaderTable}.");
                }

                HeaderTable = headerTable;
                OffsetInInput = offsetInInput;
            }

            public bool IsTable(string tag)
            {
                return string.Equals(tag, Tag, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
