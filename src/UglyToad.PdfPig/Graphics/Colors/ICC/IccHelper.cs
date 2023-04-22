using System;
using System.Linq;
using System.Text;

namespace IccProfileNet
{
    /// <summary>
    /// ICC
    /// </summary>
    internal static class IccHelper
    {
        internal static double[] Lookup(double[] input, double[][] clut, byte[] clutGridPoints)
        {
            // https://stackoverflow.com/questions/35109195/how-do-the-the-different-parts-of-an-icc-file-work-together
            if (input.Length != clutGridPoints.Length)
            {
                throw new ArgumentException("TODO");
            }

            // TODO - Need interpolation
            double index = 0;
            for (int i = 0; i < input.Length; i++)
            {
                double w = input[i] * clutGridPoints[i];
                for (int j = i + 1; j < input.Length; j++)
                {
                    w *= clutGridPoints[j];
                }
                index += w;
            }

            return clut[(int)index];
        }

        internal static double[] Lookup(double[] input, double[][] clut, int clutGridPoints)
        {
            // https://stackoverflow.com/questions/35109195/how-do-the-the-different-parts-of-an-icc-file-work-together

            // TODO - Need interpolation
            double index = 0;
            for (int i = 0; i < input.Length; i++)
            {
                index += input[i] * Math.Pow(clutGridPoints, input.Length - i);
            }

            return clut[(int)index];
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="profileBytes"></param>
        /// <returns></returns>
        public static byte[] ComputeProfileId(byte[] profileBytes)
        {
            // Compute profile id
            // This field, if not zero (00h), shall hold the Profile ID. The Profile ID shall be calculated using the MD5
            // fingerprinting method as defined in Internet RFC 1321.The entire profile, whose length is given by the size field
            // in the header, with the profile flags field (bytes 44 to 47, see 7.2.11), rendering intent field (bytes 64 to 67, see
            // 7.2.15), and profile ID field (bytes 84 to 99) in the profile header temporarily set to zeros (00h), shall be used to
            // calculate the ID. A profile ID field value of zero (00h) shall indicate that a profile ID has not been calculated.
            // Profile creators should compute and record a profile ID.

            // with the profile flags field (bytes 44 to 47, see 7.2.11)
            for (int i = IccProfileHeader.ProfileFlagsOffset; i < IccProfileHeader.ProfileFlagsOffset + IccProfileHeader.ProfileFlagsLength; i++)
            {
                profileBytes[i] = 0;
            }

            // rendering intent field (bytes 64 to 67, see 7.2.15)
            for (int i = IccProfileHeader.RenderingIntentOffset; i < IccProfileHeader.RenderingIntentOffset + IccProfileHeader.RenderingIntentLength; i++)
            {
                profileBytes[i] = 0;
            }

            for (int i = IccProfileHeader.ProfileIdOffset; i < IccProfileHeader.ProfileIdOffset + IccProfileHeader.ProfileIdLength; i++)
            {
                profileBytes[i] = 0;
            }

            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                return md5.ComputeHash(profileBytes);
            }
        }

        internal static string GetString(byte[] bytes, int index, int count)
        {
            return Encoding.ASCII.GetString(bytes, index, count).Replace("\0", string.Empty);
        }

        internal static string GetString(byte[] bytes)
        {
            return Encoding.ASCII.GetString(bytes).Replace("\0", string.Empty);
        }

        internal static string GetHexadecimalString(byte[] bytes, int startIndex, int length)
        {
            return BitConverter.ToString(bytes, startIndex, length).Replace("-", string.Empty);
        }

        internal static string GetHexadecimalString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty);
        }

        internal static IccXyz ReadXyz(byte[] bytes)
        {
            var xyz = Reads15Fixed16Array(bytes);
            return new IccXyz(xyz[0], xyz[1], xyz[2]);
        }

        internal static DateTime? ReadDateTime(byte[] bytes)
        {
            if (bytes.All(b => b == 0))
            {
                return null;
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);

                var secondsL = BitConverter.ToUInt16(bytes, 0);
                var minutesL = BitConverter.ToUInt16(bytes, 2);
                var hoursL = BitConverter.ToUInt16(bytes, 4);
                var dayL = BitConverter.ToUInt16(bytes, 6);
                var monthL = BitConverter.ToUInt16(bytes, 8);
                var yearL = BitConverter.ToUInt16(bytes, 10);
                return new DateTime(yearL, monthL, dayL, hoursL, minutesL, secondsL);
            }

            var year = BitConverter.ToUInt16(bytes, 0);
            var month = BitConverter.ToUInt16(bytes, 2);
            var day = BitConverter.ToUInt16(bytes, 4);
            var hours = BitConverter.ToUInt16(bytes, 6);
            var minutes = BitConverter.ToUInt16(bytes, 8);
            var seconds = BitConverter.ToUInt16(bytes, 10);
            return new DateTime(year, month, day, hours, minutes, seconds);
        }

        internal const int S15Fixed16Length = 4;

        internal static double ReadU8Fixed8Number(byte[] bytes)
        {
            // TODO - use ReadUInt16 instead of BitConverter.ToInt16 ????
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return Math.Round(BitConverter.ToInt16(bytes, 0) / ((double)byte.MaxValue + 1), 4, MidpointRounding.AwayFromZero);
        }

        internal static double Reads15Fixed16Number(byte[] bytes)
        {
            // TODO - use ReadUInt32 instead of BitConverter.ToInt32 ????

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return Math.Round((BitConverter.ToInt32(bytes, 0) - 0.5f) / ((double)ushort.MaxValue + 1), 4, MidpointRounding.AwayFromZero);
        }

        internal static double[] Reads15Fixed16Array(byte[] bytes)
        {
            if (bytes.Length % 4 != 0)
            {
                throw new ArgumentException("Array should be of length that is a multiple of 4", nameof(bytes));
            }

            double[] values = new double[bytes.Length / S15Fixed16Length];
            for (int e = 0; e < values.Length; e++)
            {
                byte[] a = bytes.Skip(e * S15Fixed16Length).Take(S15Fixed16Length).ToArray();
                values[e] = Reads15Fixed16Number(a);
            }

            return values;
        }

        internal static uint ReadUInt32(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToUInt32(bytes, 0);
        }

        internal const int UInt16Length = 2;

        internal static ushort ReadUInt16(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToUInt16(bytes, 0);
        }

        internal static byte ReadUInt8(byte[] bytes)
        {
            return bytes[0];
        }

        /// <summary>
        /// Each curve and processing element shall start on a 4-byte boundary.
        /// <para>
        /// To achieve this, each item shall be followed by up to three 00h pad bytes as needed.
        /// </para>
        /// </summary>
        internal static int AdjustOffsetTo4ByteBoundary(int offset)
        {
            return offset + (offset % 4);
        }

        internal static IccPrimaryPlatforms GetPrimaryPlatforms(string platform)
        {
            switch (platform)
            {
                case "APPL":
                    return IccPrimaryPlatforms.AppleComputer;

                case "MSFT":
                    return IccPrimaryPlatforms.MicrosoftCorporation;

                case "SGI ":
                    return IccPrimaryPlatforms.SiliconGraphics;

                case "SUNW":
                    return IccPrimaryPlatforms.SunMicrosystems;

                default:
                    return IccPrimaryPlatforms.Unidentified;
            }
        }

        internal static IccColourSpaceType GetColourSpaceType(string colourSpace)
        {
            switch (colourSpace)
            {
                case "XYZ ":
                    return IccColourSpaceType.nCIEXYZorPCSXYZ;

                case "Lab ":
                    return IccColourSpaceType.CIELABorPCSLAB;

                case "Luv ":
                    return IccColourSpaceType.CIELUV;

                case "YCbr":
                    return IccColourSpaceType.YCbCr;

                case "Yxy ":
                    return IccColourSpaceType.CIEYxy;

                case "RGB ":
                    return IccColourSpaceType.RGB;

                case "GRAY":
                    return IccColourSpaceType.Gray;

                case "HSV ":
                    return IccColourSpaceType.HSV;

                case "HLS ":
                    return IccColourSpaceType.HLS;

                case "CMYK":
                    return IccColourSpaceType.CMYK;

                case "CMY ":
                    return IccColourSpaceType.CMY;

                case "2CLR":
                    return IccColourSpaceType.Colour2;

                case "3CLR":
                    return IccColourSpaceType.Colour3;

                case "4CLR":
                    return IccColourSpaceType.Colour4;

                case "5CLR":
                    return IccColourSpaceType.Colour5;

                case "6CLR":
                    return IccColourSpaceType.Colour6;

                case "7CLR":
                    return IccColourSpaceType.Colour7;

                case "8CLR":
                    return IccColourSpaceType.Colour8;

                case "9CLR":
                    return IccColourSpaceType.Colour9;

                case "ACLR":
                    return IccColourSpaceType.Colour10;

                case "BCLR":
                    return IccColourSpaceType.Colour11;

                case "CCLR":
                    return IccColourSpaceType.Colour12;

                case "DCLR":
                    return IccColourSpaceType.Colour13;

                case "ECLR":
                    return IccColourSpaceType.Colour14;

                case "FCLR":
                    return IccColourSpaceType.Colour15;

                default:
                    throw new ArgumentException($"Unknown colour space type '{colourSpace}'.");
            }
        }

        internal static IccProfileClass GetProfileClass(string profile)
        {
            switch (profile)
            {
                case "scnr":
                    return IccProfileClass.Input;

                case "mntr":
                    return IccProfileClass.Display;

                case "prtr":
                    return IccProfileClass.Output;

                case "link":
                    return IccProfileClass.DeviceLink;

                case "spac":
                    return IccProfileClass.ColorSpace;

                case "abst":
                    return IccProfileClass.Abstract;

                case "nmcl":
                    return IccProfileClass.NamedColor;

                default:
                    throw new ArgumentException($"Unknown profile class '{profile}'.");
            }
        }
    }
}
