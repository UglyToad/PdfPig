namespace UglyToad.PdfPig.Util
{
    // Ported from c++ implementation at https://github.com/aappleby/smhasher/blob/0ff96f7835817a27d0487325b6c16033e2992eb5/src/MurmurHash3.cpp#L1
    // The code was ported with LLM assistance.
    //
    // The original license is included below.

    //-----------------------------------------------------------------------------
    // MurmurHash3 was written by Austin Appleby, and is placed in the public
    // domain. The author hereby disclaims copyright to this source code.

    // Note - The x86 and x64 versions do _not_ produce the same results, as the
    // algorithms are optimized for their respective platforms. You can still
    // compile and run any of them on any platform, but your performance with the
    // non-native version will be less than optimal.

    using System;
    using System.Buffers.Binary;
    using System.Security.Cryptography;

    /// <summary>
    /// MurmurHash is a non-cryptographic hash function suitable for general hash-based lookup.
    /// <para>
    /// Note - The x86 and x64 versions do _not_ produce the same results, as the
    /// algorithms are optimized for their respective platforms. You can still
    /// compile and run any of them on any platform, but your performance with the
    /// non-native version will be less than optimal.
    /// </para>
    /// </summary>
    internal static class MurmurHash3
    {
         // From Wikipedia:
         // MurmurHash is a non-cryptographic hash function suitable for general hash-based lookup. It was created
         // by Austin Appleby in 2008 and, as of 8 January 2016, is hosted on GitHub along with its test suite named
         // SMHasher. It also exists in a number of variants, all of which have been released into the public domain.
         // The name comes from two basic operations, multiply (MU) and rotate (R), used in its inner loop.
         //
         // Unlike cryptographic hash functions, it is not specifically designed to be difficult to reverse by
         // an adversary, making it unsuitable for cryptographic purposes. 
        
        /// <summary>
        /// MurmurHash3 128-bit x86 variant, returns hash as byte array (16 bytes).
        /// <para>
        /// Note - The x86 and x64 versions do _not_ produce the same results, as the
        /// algorithms are optimized for their respective platforms. You can still
        /// compile and run any of them on any platform, but your performance with the
        /// non-native version will be less than optimal.
        /// </para>
        /// </summary>
        public static byte[] Compute_x86_128(ReadOnlySpan<byte> data)
        {
            return Compute_x86_128(data, data.Length, 0);
        }

        /// <summary>
        /// MurmurHash3 128-bit x86 variant, returns hash as byte array (16 bytes).
        /// <para>
        /// Note - The x86 and x64 versions do _not_ produce the same results, as the
        /// algorithms are optimized for their respective platforms. You can still
        /// compile and run any of them on any platform, but your performance with the
        /// non-native version will be less than optimal.
        /// </para>
        /// </summary>
        public static byte[] Compute_x86_128(ReadOnlySpan<byte> data, int len, uint seed)
        {
            Span<uint> hash = stackalloc uint[4];
            Compute_x86_128(data, len, seed, hash);

#if NET
            byte[] result = GC.AllocateUninitializedArray<byte>(16);
#else
            byte[] result = new byte[16];
#endif

            var span = result.AsSpan();

            Span<byte> buffer = stackalloc byte[4];
            GetBytes(buffer, hash[0]);
            buffer.CopyTo(span.Slice(0, 4));

            GetBytes(buffer, hash[1]);
            buffer.CopyTo(span.Slice(4, 4));

            GetBytes(buffer, hash[2]);
            buffer.CopyTo(span.Slice(8, 4));

            GetBytes(buffer, hash[3]);
            buffer.CopyTo(span.Slice(12, 4));

            return result;
        }

        /// <summary>
        /// MurmurHash3 128-bit x64 variant, returns hash as byte array (16 bytes).
        /// <para>
        /// Note - The x86 and x64 versions do _not_ produce the same results, as the
        /// algorithms are optimized for their respective platforms. You can still
        /// compile and run any of them on any platform, but your performance with the
        /// non-native version will be less than optimal.
        /// </para>
        /// </summary>
        public static byte[] Compute_x64_128(ReadOnlySpan<byte> data)
        {
            return Compute_x64_128(data, data.Length, 0);
        }

        /// <summary>
        /// MurmurHash3 128-bit x64 variant, returns hash as byte array (16 bytes).
        /// <para>
        /// Note - The x86 and x64 versions do _not_ produce the same results, as the
        /// algorithms are optimized for their respective platforms. You can still
        /// compile and run any of them on any platform, but your performance with the
        /// non-native version will be less than optimal.
        /// </para>
        /// </summary>
        public static byte[] Compute_x64_128(ReadOnlySpan<byte> data, int len, uint seed)
        {
            Span<ulong> hash = stackalloc ulong[2];
            Compute_x64_128(data, len, seed, hash);

#if NET
            byte[] result = GC.AllocateUninitializedArray<byte>(16);
#else
            byte[] result = new byte[16];
#endif
            
            var span = result.AsSpan();

            Span<byte> buffer = stackalloc byte[8];
            GetBytes(buffer, hash[0]);
            buffer.CopyTo(span.Slice(0, 8));

            GetBytes(buffer, hash[1]);
            buffer.CopyTo(span.Slice(8, 8));
            
            return result;
        }

        private static void Compute_x86_128(ReadOnlySpan<byte> data, int len, uint seed, Span<uint> outHash)
        {
            const uint c1 = 0x239b961b, c2 = 0xab0e9789, c3 = 0x38b34ae5, c4 = 0xa1e38b93;

            uint h1 = seed, h2 = seed, h3 = seed, h4 = seed;
            int nblocks = len / 16;

            // Body
            for (int i = 0; i < nblocks; ++i)
            {
                int offset = i * 16;

                uint k1 = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset));
                uint k2 = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset + 4));
                uint k3 = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset + 8));
                uint k4 = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset + 12));

                k1 *= c1;
                k1 = Rotl32(k1, 15);
                k1 *= c2;
                h1 ^= k1;
                h1 = Rotl32(h1, 19);
                h1 += h2;
                h1 = h1 * 5 + 0x561ccd1b;

                k2 *= c2;
                k2 = Rotl32(k2, 16);
                k2 *= c3;
                h2 ^= k2;
                h2 = Rotl32(h2, 17);
                h2 += h3;
                h2 = h2 * 5 + 0x0bcaa747;

                k3 *= c3;
                k3 = Rotl32(k3, 17);
                k3 *= c4;
                h3 ^= k3;
                h3 = Rotl32(h3, 15);
                h3 += h4;
                h3 = h3 * 5 + 0x96cd1c35;

                k4 *= c4;
                k4 = Rotl32(k4, 18);
                k4 *= c1;
                h4 ^= k4;
                h4 = Rotl32(h4, 13);
                h4 += h1;
                h4 = h4 * 5 + 0x32ac3b17;
            }

            // Tail
            int tailStart = nblocks * 16;
            uint tk1 = 0, tk2 = 0, tk3 = 0, tk4 = 0;
            switch (len & 15)
            {
                case 15:
                    tk4 ^= (uint)data[tailStart + 14] << 16;
                    goto case 14;
                case 14:
                    tk4 ^= (uint)data[tailStart + 13] << 8;
                    goto case 13;
                case 13:
                    tk4 ^= (uint)data[tailStart + 12];
                    tk4 *= c4;
                    tk4 = Rotl32(tk4, 18);
                    tk4 *= c1;
                    h4 ^= tk4;
                    goto case 12;
                case 12:
                    tk3 ^= (uint)data[tailStart + 11] << 24;
                    goto case 11;
                case 11:
                    tk3 ^= (uint)data[tailStart + 10] << 16;
                    goto case 10;
                case 10:
                    tk3 ^= (uint)data[tailStart + 9] << 8;
                    goto case 9;
                case 9:
                    tk3 ^= (uint)data[tailStart + 8];
                    tk3 *= c3;
                    tk3 = Rotl32(tk3, 17);
                    tk3 *= c4;
                    h3 ^= tk3;
                    goto case 8;
                case 8:
                    tk2 ^= (uint)data[tailStart + 7] << 24;
                    goto case 7;
                case 7:
                    tk2 ^= (uint)data[tailStart + 6] << 16;
                    goto case 6;
                case 6:
                    tk2 ^= (uint)data[tailStart + 5] << 8;
                    goto case 5;
                case 5:
                    tk2 ^= (uint)data[tailStart + 4];
                    tk2 *= c2;
                    tk2 = Rotl32(tk2, 16);
                    tk2 *= c3;
                    h2 ^= tk2;
                    goto case 4;
                case 4:
                    tk1 ^= (uint)data[tailStart + 3] << 24;
                    goto case 3;
                case 3:
                    tk1 ^= (uint)data[tailStart + 2] << 16;
                    goto case 2;
                case 2:
                    tk1 ^= (uint)data[tailStart + 1] << 8;
                    goto case 1;
                case 1:
                    tk1 ^= (uint)data[tailStart];
                    tk1 *= c1;
                    tk1 = Rotl32(tk1, 15);
                    tk1 *= c2;
                    h1 ^= tk1;
                    break;
            }

            // Finalization
            h1 ^= (uint)len;
            h2 ^= (uint)len;
            h3 ^= (uint)len;
            h4 ^= (uint)len;
            h1 += h2;
            h1 += h3;
            h1 += h4;
            h2 += h1;
            h3 += h1;
            h4 += h1;
            h1 = Fmix32(h1);
            h2 = Fmix32(h2);
            h3 = Fmix32(h3);
            h4 = Fmix32(h4);
            h1 += h2;
            h1 += h3;
            h1 += h4;
            h2 += h1;
            h3 += h1;
            h4 += h1;

            outHash[0] = h1;
            outHash[1] = h2;
            outHash[2] = h3;
            outHash[3] = h4;
        }

        private static void Compute_x64_128(ReadOnlySpan<byte> data, int len, uint seed, Span<ulong> outHash)
        {
            const ulong c1 = 0x87c37b91114253d5UL;
            const ulong c2 = 0x4cf5ad432745937fUL;

            ulong h1 = seed, h2 = seed;
            int nblocks = len / 16;

            // Body
            for (int i = 0; i < nblocks; ++i)
            {
                int offset = i * 16;
                ulong k1 = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(offset));
                ulong k2 = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(offset + 8));

                k1 *= c1;
                k1 = Rotl64(k1, 31);
                k1 *= c2;
                h1 ^= k1;
                h1 = Rotl64(h1, 27);
                h1 += h2;
                h1 = h1 * 5 + 0x52dce729;

                k2 *= c2;
                k2 = Rotl64(k2, 33);
                k2 *= c1;
                h2 ^= k2;
                h2 = Rotl64(h2, 31);
                h2 += h1;
                h2 = h2 * 5 + 0x38495ab5;
            }

            // Tail
            int tailStart = nblocks * 16;
            ulong tk1 = 0, tk2 = 0;
            switch (len & 15)
            {
                case 15:
                    tk2 ^= ((ulong)data[tailStart + 14]) << 48;
                    goto case 14;
                case 14:
                    tk2 ^= ((ulong)data[tailStart + 13]) << 40;
                    goto case 13;
                case 13:
                    tk2 ^= ((ulong)data[tailStart + 12]) << 32;
                    goto case 12;
                case 12:
                    tk2 ^= ((ulong)data[tailStart + 11]) << 24;
                    goto case 11;
                case 11:
                    tk2 ^= ((ulong)data[tailStart + 10]) << 16;
                    goto case 10;
                case 10:
                    tk2 ^= ((ulong)data[tailStart + 9]) << 8;
                    goto case 9;
                case 9:
                    tk2 ^= ((ulong)data[tailStart + 8]);
                    tk2 *= c2;
                    tk2 = Rotl64(tk2, 33);
                    tk2 *= c1;
                    h2 ^= tk2;
                    goto case 8;
                case 8:
                    tk1 ^= ((ulong)data[tailStart + 7]) << 56;
                    goto case 7;
                case 7:
                    tk1 ^= ((ulong)data[tailStart + 6]) << 48;
                    goto case 6;
                case 6:
                    tk1 ^= ((ulong)data[tailStart + 5]) << 40;
                    goto case 5;
                case 5:
                    tk1 ^= ((ulong)data[tailStart + 4]) << 32;
                    goto case 4;
                case 4:
                    tk1 ^= ((ulong)data[tailStart + 3]) << 24;
                    goto case 3;
                case 3:
                    tk1 ^= ((ulong)data[tailStart + 2]) << 16;
                    goto case 2;
                case 2:
                    tk1 ^= ((ulong)data[tailStart + 1]) << 8;
                    goto case 1;
                case 1:
                    tk1 ^= ((ulong)data[tailStart + 0]);
                    tk1 *= c1;
                    tk1 = Rotl64(tk1, 31);
                    tk1 *= c2;
                    h1 ^= tk1;
                    break;
            }

            // Finalization
            h1 ^= (ulong)len;
            h2 ^= (ulong)len;
            h1 += h2;
            h2 += h1;
            h1 = Fmix64(h1);
            h2 = Fmix64(h2);
            h1 += h2;
            h2 += h1;
            outHash[0] = h1;
            outHash[1] = h2;
        }

        // ---- Utility functions and mixing ----

        private static uint Rotl32(uint x, int r) => (x << r) | (x >> (32 - r));
        
        private static ulong Rotl64(ulong x, int r) => (x << r) | (x >> (64 - r));

        private static uint Fmix32(uint h)
        {
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;
            return h;
        }

        private static ulong Fmix64(ulong k)
        {
            k ^= k >> 33;
            k *= 0xff51afd7ed558ccdUL;
            k ^= k >> 33;
            k *= 0xc4ceb9fe1a85ec53UL;
            k ^= k >> 33;
            return k;
        }

        private static void GetBytes(Span<byte> buffer, ulong v)
        {
            if (BitConverter.IsLittleEndian)
            {
                v = BinaryPrimitives.ReverseEndianness(v);
            }

            BinaryPrimitives.WriteUInt64LittleEndian(buffer, v);
        }

        private static void GetBytes(Span<byte> buffer, uint v)
        {
            if (BitConverter.IsLittleEndian)
            {
                v = BinaryPrimitives.ReverseEndianness(v);
            }

            BinaryPrimitives.WriteUInt32LittleEndian(buffer, v);
        }
    }
}
