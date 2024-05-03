﻿namespace UglyToad.PdfPig.Images.Png
{
    /// <summary>
    /// 32-bit Cyclic Redundancy Code used by the PNG for checking the data is intact.
    /// </summary>
    internal class Crc32
    {
        private const uint Polynomial = 0xEDB88320;

        private static readonly uint[] Lookup;

        static Crc32()
        {
            Lookup = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                var value = i;
                for (var j = 0; j < 8; ++j)
                {
                    if ((value & 1) != 0)
                    {
                        value = (value >> 1) ^ Polynomial;
                    }
                    else
                    {
                        value >>= 1;
                    }
                }

                Lookup[i] = value;
            }
        }

        /// <summary>
        /// Calculate the CRC32 for data.
        /// </summary>
        public static uint Calculate(ReadOnlySpan<byte> data)
        {
            var crc32 = uint.MaxValue;
            for (var i = 0; i < data.Length; i++)
            {
                var index = (crc32 ^ data[i]) & 0xFF;
                crc32 = (crc32 >> 8) ^ Lookup[index];
            }

            return crc32 ^ uint.MaxValue;
        }

        /// <summary>
        /// Calculate the combined CRC32 for data.
        /// </summary>
        public static uint Calculate(ReadOnlySpan<byte> data, ReadOnlySpan<byte> data2)
        {
            var crc32 = uint.MaxValue;
            for (var i = 0; i < data.Length; i++)
            {
                var index = (crc32 ^ data[i]) & 0xFF;
                crc32 = (crc32 >> 8) ^ Lookup[index];
            }

            for (var i = 0; i < data2.Length; i++)
            {
                var index = (crc32 ^ data2[i]) & 0xFF;
                crc32 = (crc32 >> 8) ^ Lookup[index];
            }

            return crc32 ^ uint.MaxValue;
        }


        private uint state = uint.MaxValue;

        /// <summary>
        /// Calculate the CRC32 for data.
        /// </summary>
        public void Append(ReadOnlySpan<byte> data)
        {
            for (var i = 0; i < data.Length; i++)
            {
                var index = (state ^ data[i]) & 0xFF;
                state = (state >> 8) ^ Lookup[index];
            }
        }

        public uint GetCurrentHashAsUInt32()
        {
            return state ^ uint.MaxValue;
        }

        public void Reset()
        {
            state = uint.MaxValue;
        }
    }
}
