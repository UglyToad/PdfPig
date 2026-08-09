namespace UglyToad.PdfPig.Graphics.Colors.Icc
{
    using System;

    /// <summary>
    /// The few fields of an ICC profile header PdfPig reads for itself.
    /// <para>
    /// Interpreting an ICC profile is the job of an <see cref="IIccProfileService"/>, and nothing on the
    /// reading path parses profile bytes here. This exists for the writing path, which has no service to
    /// ask: when PdfPig embeds its own profile it still has to declare that profile's component count
    /// correctly, and the profile is the only thing that knows it.
    /// </para>
    /// </summary>
    internal static class IccProfileHeader
    {
        /// <summary>
        /// Offset of the data colour space signature in the 128-byte header (ICC.1, Table 14).
        /// </summary>
        private const int DataColourSpaceOffset = 16;

        private const int HeaderLength = 128;

        /// <summary>
        /// The number of input components implied by the profile's data colour space signature.
        /// </summary>
        public static bool TryGetNumberOfComponents(ReadOnlySpan<byte> profile, out int numberOfComponents)
        {
            numberOfComponents = 0;

            if (profile.Length < HeaderLength)
            {
                return false;
            }

            var signature = profile.Slice(DataColourSpaceOffset, 4);

            switch (AsAscii(signature))
            {
                case "GRAY":
                    numberOfComponents = 1;
                    return true;

                // Every three-component data colour space ICC.1 defines.
                case "XYZ ":
                case "Lab ":
                case "Luv ":
                case "YCbr":
                case "Yxy ":
                case "RGB ":
                case "HSV ":
                case "HLS ":
                case "CMY ":
                    numberOfComponents = 3;
                    return true;

                case "CMYK":
                    numberOfComponents = 4;
                    return true;
            }

            // The "nCLR" family: a leading hex digit gives the channel count, so 2CLR .. FCLR is 2 .. 15.
            if (signature[1] == (byte)'C' && signature[2] == (byte)'L' && signature[3] == (byte)'R')
            {
                int count = FromHexDigit(signature[0]);
                if (count > 0)
                {
                    numberOfComponents = count;
                    return true;
                }
            }

            return false;
        }

        private static string AsAscii(ReadOnlySpan<byte> signature)
        {
            Span<char> chars = stackalloc char[4];
            for (int i = 0; i < 4; i++)
            {
                chars[i] = (char)signature[i];
            }

            return chars.ToString();
        }

        private static int FromHexDigit(byte value)
        {
            if (value >= '0' && value <= '9')
            {
                return value - '0';
            }

            if (value >= 'A' && value <= 'F')
            {
                return value - 'A' + 10;
            }

            return -1;
        }
    }
}
