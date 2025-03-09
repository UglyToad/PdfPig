namespace UglyToad.PdfPig.Images
{
    using System;
    using System.Buffers.Binary;

    internal static class Jpeg2000Helper
    {
        /// <summary>
        /// Get bits per component values for Jp2 (Jpx) encoded images (first component).
        /// </summary>
        public static byte GetBitsPerComponent(ReadOnlySpan<byte> jp2Bytes)
        {
            // Ensure the input has at least 12 bytes for the signature box
            if (jp2Bytes.Length < 12)
            {
                throw new InvalidOperationException("Input is too short to be a valid JP2 file.");
            }

            // Verify the JP2 signature box
            uint length = BinaryPrimitives.ReadUInt32BigEndian(jp2Bytes.Slice(0, 4));
            if (length == 0xFF4FFF51)
            {
                // J2K format detected (SOC marker) (See GHOSTSCRIPT-688999-2.pdf)
                return ParseCodestream(jp2Bytes);
            }

            uint type = BinaryPrimitives.ReadUInt32BigEndian(jp2Bytes.Slice(4, 4));
            uint magic = BinaryPrimitives.ReadUInt32BigEndian(jp2Bytes.Slice(8, 4));
            if (length == 0x0000000C && type == 0x6A502020 && magic == 0x0D0A870A)
            {
                // JP2 format detected
                return ParseBoxes(jp2Bytes.Slice(12));
            }

            throw new InvalidOperationException("Invalid JP2 or J2K signature.");
        }

        private static byte ParseBoxes(ReadOnlySpan<byte> jp2Bytes)
        {
            int offset = 0;
            while (offset < jp2Bytes.Length)
            {
                if (offset + 8 > jp2Bytes.Length)
                {
                    throw new InvalidOperationException("Invalid JP2 or J2K box structure.");
                }

                // Read box length and type
                uint boxLength = BinaryPrimitives.ReadUInt32BigEndian(jp2Bytes.Slice(offset, 4));
                uint boxType = BinaryPrimitives.ReadUInt32BigEndian(jp2Bytes.Slice(offset + 4, 4));

                // Check for the contiguous codestream box ('jp2c')
                if (boxType == 0x6A703263) // 'jp2c' in ASCII
                {
                    // Parse the codestream to find the SIZ marker
                    return ParseCodestream(jp2Bytes.Slice(offset + 8));
                }

                // Move to the next box
                offset += (int)(boxLength > 0 ? boxLength : 8); // Box length of 0 means the rest of the file
            }

            throw new InvalidOperationException("Codestream box not found in JP2 or J2K file.");
        }

        private static byte ParseCodestream(ReadOnlySpan<byte> codestream)
        {
            int offset = 0;
            while (offset + 2 <= codestream.Length)
            {
                // Read marker (2 bytes)
                ushort marker = BinaryPrimitives.ReadUInt16BigEndian(codestream.Slice(offset, 2));

                // Check for SIZ marker (0xFF51)
                if (marker == 0xFF51)
                {
                    if (offset + 38 > codestream.Length)
                    {
                        throw new InvalidOperationException("Invalid SIZ marker structure.");
                    }

                    // Skip marker length (2 bytes), capabilities (4 bytes), and reference grid size (8 bytes)
                    // Skip image offset (8 bytes), tile size (8 bytes), and tile offset (8 bytes)
                    offset += 38;

                    // Read number of components (2 bytes)
                    ushort numComponents = BinaryPrimitives.ReadUInt16BigEndian(codestream.Slice(offset, 2));

                    offset += 2;
                    if (numComponents < 1)
                    {
                        throw new InvalidOperationException("Invalid number of components in SIZ marker.");
                    }

                    // Read bits per component for the first component (1 byte per component)
                    byte bitsPerComponent = codestream[offset];

                    // Bits per component is stored as (bits - 1)
                    return ++bitsPerComponent;
                }
                // Move to the next marker
                offset += 2;
            }

            throw new InvalidOperationException("SIZ marker not found in JPEG2000 codestream.");
        }
    }
}
