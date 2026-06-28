namespace UglyToad.PdfPig.Images
{
    using Graphics.Colors;
    using System;
    using System.Buffers.Binary;

    internal static class Jpeg2000Helper
    {
        // JPX enumerated colour space values (ISO/IEC 15444-1 and 15444-2), as referenced
        // by PDF 2.0 7.4.9 ("the JPX baseline set of enumerated colour spaces").
        private const uint EnumCsCmyk = 12;      // CMYK
        private const uint EnumCsCieLab = 14;    // CIELab
        private const uint EnumCsSrgb = 16;      // sRGB
        private const uint EnumCsGreyscale = 17; // greyscale
        private const uint EnumCsSycc = 18;      // sYCC
        private const uint EnumCsCieJab = 19;    // CIEJab - shall not occur in a PDF (7.4.9)
        private const uint EnumCsEsrgb = 20;     // e-sRGB
        private const uint EnumCsRommRgb = 21;   // ROMM-RGB
        private const uint EnumCsYpbpr1125 = 22; // YPbPr(1125/60)
        private const uint EnumCsYpbpr1250 = 23; // YPbPr(1250/50)
        private const uint EnumCsEsycc = 24;     // e-sYCC

        /// <summary>
        /// A classification of the colour space declared inside a JPEG2000 (JPX) data stream, used to
        /// decide how the decoded samples are interpreted when the image dictionary has no explicit
        /// <c>/ColorSpace</c> entry (PDF 2.0, 7.4.9).
        /// </summary>
        private enum Jpeg2000ColorSpace : byte
        {
            /// <summary>
            /// No enumerated colour space could be read from the data (for example a raw codestream
            /// with no JP2 boxes, or a stream that declares an ICC profile instead). The caller should
            /// fall back to the colour space implied by the number of components.
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// Single channel greyscale (enumerated colour space 17).
            /// </summary>
            Gray,

            /// <summary>
            /// Three channel RGB family (enumerated colour spaces 16, 18, 20, 21, 22, 23, 24).
            /// </summary>
            Rgb,

            /// <summary>
            /// Four channel CMYK (enumerated colour space 12).
            /// </summary>
            Cmyk,

            /// <summary>
            /// Three channel CIE L*a*b* (enumerated colour space 14).
            /// </summary>
            CieLab,

            /// <summary>
            /// The colour space is described by an embedded ICC profile (the JP2 <c>colr</c> box uses a
            /// restricted or any ICC method).
            /// </summary>
            Icc,

            /// <summary>
            /// A colour space that shall not occur in a conforming PDF (enumerated colour space 19,
            /// CIEJab - explicitly excluded by 7.4.9).
            /// </summary>
            Unsupported
        }

        /// <summary>
        /// Determines the colour space of a JPXDecode image that has no explicit <c>/ColorSpace</c>
        /// entry, from the colour space information embedded in the JPEG2000 data (PDF 2.0, 7.4.9).
        /// </summary>
        /// <remarks>
        /// The enumerated colour spaces of the JPX baseline set map to device colour spaces, CIELab maps
        /// to a <see cref="LabColorSpaceDetails"/>, and an embedded ICC profile maps to an
        /// <see cref="ICCBasedColorSpaceDetails"/> that retains the raw profile bytes. CIEJab (enumerated
        /// colour space 19) shall not occur in a PDF and is rejected (returns <c>null</c>, dropping the
        /// image). The number of ordinary colour channels in the JPEG2000 data shall match the number of
        /// components in the colour space (7.4.9): when the declared colour space disagrees with the
        /// channel count, and for raw codestreams that carry no colour space box, the device colour space
        /// implied by the channel count is used.
        /// </remarks>
        public static ColorSpaceDetails? GetJpxColorSpaceDetails(ReadOnlyMemory<byte> jpxData)
        {
            ReadOnlySpan<byte> jpxSpan = jpxData.Span;
            int numberOfComponents = GetNumberOfComponents(jpxSpan);

            switch (GetColorSpace(jpxSpan, out int iccProfileOffset, out int iccProfileLength))
            {
                case Jpeg2000ColorSpace.Gray:
                    return numberOfComponents == 1
                        ? DeviceGrayColorSpaceDetails.Instance
                        : GetJpxDeviceColorSpaceDetails(numberOfComponents);
                case Jpeg2000ColorSpace.Rgb:
                    return numberOfComponents == 3
                        ? DeviceRgbColorSpaceDetails.Instance
                        : GetJpxDeviceColorSpaceDetails(numberOfComponents);
                case Jpeg2000ColorSpace.Cmyk:
                    return numberOfComponents == 4
                        ? DeviceCmykColorSpaceDetails.Instance
                        : GetJpxDeviceColorSpaceDetails(numberOfComponents);
                case Jpeg2000ColorSpace.CieLab:
                    if (numberOfComponents == 3)
                    {
                        // JP2 CIELab uses a D50 illuminant by default; the a*/b* range defaults match the
                        // PDF Lab defaults.
                        var (x, y, z) = RGBWorkingSpace.ReferenceWhites.D50;
                        return new LabColorSpaceDetails([x, y, z], null, null);
                    }
                    return GetJpxDeviceColorSpaceDetails(numberOfComponents);
                case Jpeg2000ColorSpace.Icc:
                    if (numberOfComponents == 1 || numberOfComponents == 3 || numberOfComponents == 4)
                    {
                        var iccProfile = jpxData.Slice(iccProfileOffset, iccProfileLength); // TODO - use for later
                        return new ICCBasedColorSpaceDetails(numberOfComponents, null, null, null);
                    }
                    return null;
                case Jpeg2000ColorSpace.Unsupported:
                    // CIEJab (enumerated colour space 19) shall not occur in a PDF.
                    return null;
                default:
                    // Unknown: a raw codestream with no colour space box, or an unrecognised enumerated
                    // value. Use the device colour space implied by the channel count.
                    return GetJpxDeviceColorSpaceDetails(numberOfComponents);
            }

            static ColorSpaceDetails? GetJpxDeviceColorSpaceDetails(int numberOfComponents)
            {
                switch (numberOfComponents)
                {
                    case 1:
                        return DeviceGrayColorSpaceDetails.Instance;
                    case 3:
                        return DeviceRgbColorSpaceDetails.Instance;
                    case 4:
                        return DeviceCmykColorSpaceDetails.Instance;
                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Get bits per component values for Jp2 (Jpx) encoded images (first component).
        /// </summary>
        public static byte GetBitsPerComponent(ReadOnlySpan<byte> jp2Bytes)
        {
            return ParseSiz(jp2Bytes).BitsPerComponent;
        }

        public static ushort GetNumberOfComponents(ReadOnlySpan<byte> jp2Bytes)
        {
            return ParseSiz(jp2Bytes).NumberOfComponents;
        }

        private static Jpeg2000ColorSpace GetColorSpace(ReadOnlySpan<byte> jp2Bytes)
        {
            return GetColorSpace(jp2Bytes, out _, out _);
        }

        private static Jpeg2000ColorSpace GetColorSpace(ReadOnlySpan<byte> jp2Bytes, out int iccProfileOffset, out int iccProfileLength)
        {
            iccProfileOffset = 0;
            iccProfileLength = 0;

            if (jp2Bytes.Length < 12)
            {
                throw new InvalidOperationException("Input is too short to be a valid JP2 file.");
            }

            uint length = BinaryPrimitives.ReadUInt32BigEndian(jp2Bytes.Slice(0, 4));
            if (length == 0xFF4FFF51)
            {
                // Raw J2K codestream (SOC marker): there are no JP2 boxes and therefore no enumerated
                // colour space to read.
                return Jpeg2000ColorSpace.Unknown;
            }

            uint type = BinaryPrimitives.ReadUInt32BigEndian(jp2Bytes.Slice(4, 4));
            uint magic = BinaryPrimitives.ReadUInt32BigEndian(jp2Bytes.Slice(8, 4));
            if (length == 0x0000000C && type == 0x6A502020 && magic == 0x0D0A870A)
            {
                // JP2 format: read the 'colr' box (a child of the 'jp2h' header super box).
                if (TryReadColr(jp2Bytes, 12, out uint enumeratedColorSpace, out iccProfileOffset, out iccProfileLength))
                {
                    if (iccProfileLength > 0)
                    {
                        return Jpeg2000ColorSpace.Icc;
                    }

                    return MapEnumeratedColorSpace(enumeratedColorSpace);
                }

                return Jpeg2000ColorSpace.Unknown;
            }

            throw new InvalidOperationException("Invalid JP2 or J2K signature.");
        }

        private static Jpeg2000ColorSpace MapEnumeratedColorSpace(uint enumeratedColorSpace)
        {
            switch (enumeratedColorSpace)
            {
                case EnumCsGreyscale:
                    return Jpeg2000ColorSpace.Gray;
                case EnumCsSrgb:
                case EnumCsSycc:
                case EnumCsEsrgb:
                case EnumCsRommRgb:
                case EnumCsYpbpr1125:
                case EnumCsYpbpr1250:
                case EnumCsEsycc:
                    return Jpeg2000ColorSpace.Rgb;
                case EnumCsCmyk:
                    return Jpeg2000ColorSpace.Cmyk;
                case EnumCsCieLab:
                    return Jpeg2000ColorSpace.CieLab;
                case EnumCsCieJab:
                    return Jpeg2000ColorSpace.Unsupported;
                default:
                    return Jpeg2000ColorSpace.Unknown;
            }
        }

        /// <summary>
        /// Reads the 'colr' box (a child of the 'jp2h' header super box). When it specifies an
        /// enumerated colour space (METH == 1) the value is returned through
        /// <paramref name="enumeratedColorSpace"/>; when it specifies an ICC profile (METH >= 2) the
        /// profile's position within <paramref name="jp2Bytes"/> is returned through
        /// <paramref name="iccProfileOffset"/> and <paramref name="iccProfileLength"/> (no bytes are
        /// copied). An enumerated colour space is preferred when both are present (JPX permits more than
        /// one 'colr' box).
        /// </summary>
        private static bool TryReadColr(ReadOnlySpan<byte> jp2Bytes, int start, out uint enumeratedColorSpace,
            out int iccProfileOffset, out int iccProfileLength)
        {
            enumeratedColorSpace = 0;
            iccProfileOffset = 0;
            iccProfileLength = 0;

            // The 'colr' box is a child of the 'jp2h' header super box.
            if (!TryFindBox(jp2Bytes, start, 0x6A703268, out int jp2hStart, out int jp2hEnd))
            {
                return false;
            }

            int iccOffset = 0;
            int iccLength = 0;

            int offset = jp2hStart;
            while (offset + 8 <= jp2hEnd)
            {
                uint boxLength = BinaryPrimitives.ReadUInt32BigEndian(jp2Bytes.Slice(offset, 4));
                uint boxType = BinaryPrimitives.ReadUInt32BigEndian(jp2Bytes.Slice(offset + 4, 4));

                int contentStart = offset + 8;
                // A box length of 0 means the box extends to the end of the enclosing box.
                int boxEnd = boxLength == 0 ? jp2hEnd : offset + (int)boxLength;
                if (boxEnd <= offset || boxEnd > jp2hEnd)
                {
                    break;
                }

                if (boxType == 0x636F6C72) // 'colr'
                {
                    int contentLength = boxEnd - contentStart;
                    // colr: METH (1) | PREC (1) | APPROX (1) | [METH == 1: EnumCS (4)] | [METH >= 2: ICC profile]
                    if (contentLength >= 1)
                    {
                        byte method = jp2Bytes[contentStart];
                        if (method == 1 && contentLength >= 7)
                        {
                            // Enumerated colour space - preferred, stop here.
                            enumeratedColorSpace = BinaryPrimitives.ReadUInt32BigEndian(jp2Bytes.Slice(contentStart + 3, 4));
                            return true;
                        }

                        if (method >= 2 && contentLength > 3 && iccLength == 0)
                        {
                            // Restricted ICC (METH 2) or any ICC (METH 3) profile - remember its location
                            // (in place, no copy) as a fallback while scanning for an enumerated colour space.
                            iccOffset = contentStart + 3;
                            iccLength = contentLength - 3;
                        }
                    }
                }

                offset = boxEnd;
            }

            if (iccLength > 0)
            {
                iccProfileOffset = iccOffset;
                iccProfileLength = iccLength;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the first box of the given type at or after <paramref name="start"/> and returns the
        /// start and end offsets of its content within <paramref name="jp2Bytes"/>.
        /// </summary>
        private static bool TryFindBox(ReadOnlySpan<byte> jp2Bytes, int start, uint wantedType,
            out int contentStart, out int contentEnd)
        {
            contentStart = 0;
            contentEnd = 0;

            int offset = start;
            while (offset + 8 <= jp2Bytes.Length)
            {
                uint boxLength = BinaryPrimitives.ReadUInt32BigEndian(jp2Bytes.Slice(offset, 4));
                uint boxType = BinaryPrimitives.ReadUInt32BigEndian(jp2Bytes.Slice(offset + 4, 4));

                int boxContentStart = offset + 8;
                int boxEnd = boxLength == 0 ? jp2Bytes.Length : offset + (int)boxLength;
                if (boxEnd <= offset || boxEnd > jp2Bytes.Length)
                {
                    return false;
                }

                if (boxType == wantedType)
                {
                    contentStart = boxContentStart;
                    contentEnd = boxEnd;
                    return true;
                }

                offset = boxEnd;
            }

            return false;
        }

        private static (ushort NumberOfComponents, byte BitsPerComponent) ParseSiz(ReadOnlySpan<byte> jp2Bytes)
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

        private static (ushort NumberOfComponents, byte BitsPerComponent) ParseBoxes(ReadOnlySpan<byte> jp2Bytes)
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

        private static (ushort NumberOfComponents, byte BitsPerComponent) ParseCodestream(ReadOnlySpan<byte> codestream)
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

                    if (offset >= codestream.Length)
                    {
                        throw new InvalidOperationException("Invalid SIZ marker structure.");
                    }

                    // Read bits per component for the first component (1 byte per component)
                    byte bitsPerComponent = codestream[offset];

                    // Bits per component is stored as (bits - 1)
                    bitsPerComponent++;

                    return (numComponents, bitsPerComponent);
                }
                // Move to the next marker
                offset += 2;
            }

            throw new InvalidOperationException("SIZ marker not found in JPEG2000 codestream.");
        }
    }
}
