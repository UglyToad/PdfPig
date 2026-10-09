using System.Collections.Generic;

namespace UglyToad.PdfPig.Filters.CcittFax;
/// <summary>Associates a CCITT codeword with its encoded bit count and decoded run length.</summary>
/// <remarks>Bits is the codeword integer in MSB-first reading order; Length counts encoded bits;
/// Run counts pixels. Runs 0..63 terminate a run sequence; longer values are makeup lengths.</remarks>
internal readonly struct CcittCode
{
    internal readonly int Bits, Length, Run;
    internal CcittCode(int codeBits, int length, int run)
    {
        Bits = codeBits;
        Length = length;
        Run = run;
    }
}

/// <summary>Provides the standard white and black run codewords used by both CCITT decoding paths.</summary>
/// <remarks>
/// <para>The jagged arrays group codewords and pixel counts by encoded bit length. Matching
/// indices identify one code. CreateRunCodes combines these pairs into immutable value records;
/// the decoder then generates its prefix and short-run-pair lookup tables from these definitions.</para>
/// <para>Provenance: codeword values and run lengths are retained unchanged from the Apache-2.0
/// <see href="https://github.com/UglyToad/PdfPig/blob/bdbc5f47fdbca11542db7ee876426ee601374427/src/UglyToad.PdfPig/Filters/CcittFax/CcittFaxDecoderStream.cs">pinned PdfPig master decoder</see>.
/// That decoder attributes its C# port to the Apache-2.0
/// <see href="https://github.com/apache/pdfbox/blob/e644c29279e276bde14ce7a33bdeef0cb1001b3e/pdfbox/src/main/java/org/apache/pdfbox/filter/CCITTFaxDecoderStream.java">PDFBox decoder</see>.
/// The definitions represent standard T.4 run codes, also used by T.6 horizontal operations.
/// Flattening the definitions and building packed lookup entries are the current PdfPig implementation.</para>
/// </remarks>
internal static class CcittFaxCodebook
{
    private static readonly short[][] BlackCodeBitsByLength = new short[][]
    {
        new short[] { // 2 bits
            0x2, 0x3,
        },
        new short[] { // 3 bits
            0x2, 0x3,
        },
        new short[] { // 4 bits
            0x2, 0x3,
        },
        new short[] { // 5 bits
            0x3,
        },
        new short[] { // 6 bits
            0x4, 0x5,
        },
        new short[] { // 7 bits
            0x4, 0x5, 0x7,
        },
        new short[] { // 8 bits
            0x4, 0x7,
        },
        new short[] { // 9 bits
            0x18,
        },
        new short[] { // 10 bits
            0x17, 0x18, 0x37, 0x8, 0xf,
        },
        new short[] { // 11 bits
            0x17, 0x18, 0x28, 0x37, 0x67, 0x68, 0x6c, 0x8, 0xc, 0xd,
        },
        new short[] { // 12 bits
            0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x1c, 0x1d, 0x1e, 0x1f, 0x24, 0x27, 0x28, 0x2b, 0x2c, 0x33,
            0x34, 0x35, 0x37, 0x38, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5a, 0x5b, 0x64, 0x65,
            0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0xc8, 0xc9, 0xca, 0xcb, 0xcc, 0xcd, 0xd2, 0xd3,
            0xd4, 0xd5, 0xd6, 0xd7, 0xda, 0xdb,
        },
        new short[] { // 13 bits
            0x4a, 0x4b, 0x4c, 0x4d, 0x52, 0x53, 0x54, 0x55, 0x5a, 0x5b, 0x64, 0x65, 0x6c, 0x6d, 0x72, 0x73,
            0x74, 0x75, 0x76, 0x77,
        }
    };
    private static readonly short[][] BlackRunLengthsByLength = new short[][]
    {
        new short[] { // 2 bits
            3, 2,
        },
        new short[] { // 3 bits
            1, 4,
        },
        new short[] { // 4 bits
            6, 5,
        },
        new short[] { // 5 bits
            7,
        },
        new short[] { // 6 bits
            9, 8,
        },
        new short[] { // 7 bits
            10, 11, 12,
        },
        new short[] { // 8 bits
            13, 14,
        },
        new short[] { // 9 bits
            15,
        },
        new short[] { // 10 bits
            16, 17, 0, 18, 64,
        },
        new short[] { // 11 bits
            24, 25, 23, 22, 19, 20, 21, 1792, 1856, 1920,
        },
        new short[] { // 12 bits
            1984, 2048, 2112, 2176, 2240, 2304, 2368, 2432, 2496, 2560, 52, 55, 56, 59, 60, 320,
            384, 448, 53, 54, 50, 51, 44, 45, 46, 47, 57, 58, 61, 256, 48, 49,
            62, 63, 30, 31, 32, 33, 40, 41, 128, 192, 26, 27, 28, 29, 34, 35,
            36, 37, 38, 39, 42, 43,
        },
        new short[] { // 13 bits
            640, 704, 768, 832, 1280, 1344, 1408, 1472, 1536, 1600, 1664, 1728, 512, 576, 896, 960,
            1024, 1088, 1152, 1216,
        }
    };
    private static readonly short[][] WhiteCodeBitsByLength = new short[][]
    {
        new short[] { // 4 bits
            0x7, 0x8, 0xb, 0xc, 0xe, 0xf,
        },
        new short[] { // 5 bits
            0x12, 0x13, 0x14, 0x1b, 0x7, 0x8,
        },
        new short[] { // 6 bits
            0x17, 0x18, 0x2a, 0x2b, 0x3, 0x34, 0x35, 0x7, 0x8,
        },
        new short[] { // 7 bits
            0x13, 0x17, 0x18, 0x24, 0x27, 0x28, 0x2b, 0x3, 0x37, 0x4, 0x8, 0xc,
        },
        new short[] { // 8 bits
            0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x1a, 0x1b, 0x2, 0x24, 0x25, 0x28, 0x29, 0x2a, 0x2b, 0x2c,
            0x2d, 0x3, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x4, 0x4a, 0x4b, 0x5, 0x52, 0x53, 0x54, 0x55,
            0x58, 0x59, 0x5a, 0x5b, 0x64, 0x65, 0x67, 0x68, 0xa, 0xb,
        },
        new short[] { // 9 bits
            0x98, 0x99, 0x9a, 0x9b, 0xcc, 0xcd, 0xd2, 0xd3, 0xd4, 0xd5, 0xd6, 0xd7, 0xd8, 0xd9, 0xda, 0xdb,
        },
        new short[] { // 10 bits

        },
        new short[] { // 11 bits
            0x8, 0xc, 0xd,
        },
        new short[] { // 12 bits
            0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x1c, 0x1d, 0x1e, 0x1f,
        }
    };
    private static readonly short[][] WhiteRunLengthsByLength = new short[][]
    {
        new short[] { // 4 bits
            2, 3, 4, 5, 6, 7,
        },
        new short[] { // 5 bits
            128, 8, 9, 64, 10, 11,
        },
        new short[] { // 6 bits
            192, 1664, 16, 17, 13, 14, 15, 1, 12,
        },
        new short[] { // 7 bits
            26, 21, 28, 27, 18, 24, 25, 22, 256, 23, 20, 19,
        },
        new short[] { // 8 bits
            33, 34, 35, 36, 37, 38, 31, 32, 29, 53, 54, 39, 40, 41, 42, 43,
            44, 30, 61, 62, 63, 0, 320, 384, 45, 59, 60, 46, 49, 50, 51, 52,
            55, 56, 57, 58, 448, 512, 640, 576, 47, 48,
        },
        new short[] { // 9 bits
            1472, 1536, 1600, 1728, 704, 768, 832, 896, 960, 1024, 1088, 1152, 1216, 1280, 1344, 1408,
        },
        new short[] { // 10 bits

        },
        new short[] { // 11 bits
            1792, 1856, 1920,
        },
        new short[] { // 12 bits
            1984, 2048, 2112, 2176, 2240, 2304, 2368, 2432, 2496, 2560,
        }
    };
    internal static readonly CcittCode[] WhiteRunCodes = CreateRunCodes(WhiteCodeBitsByLength, WhiteRunLengthsByLength, 4);
    internal static readonly CcittCode[] BlackRunCodes = CreateRunCodes(BlackCodeBitsByLength, BlackRunLengthsByLength, 2);
    private static CcittCode[] CreateRunCodes(short[][] codeBitsByLength, short[][] runLengthsByLength, int minimumBitCount)
    {
        var runCodes = new List<CcittCode>();
        for (int lengthIndex = 0; lengthIndex < codeBitsByLength.Length; lengthIndex++)
        {
            for (int codeIndex = 0; codeIndex < codeBitsByLength[lengthIndex].Length; codeIndex++)
            {
                runCodes.Add(new(
                    codeBitsByLength[lengthIndex][codeIndex],
                    lengthIndex + minimumBitCount,
                    runLengthsByLength[lengthIndex][codeIndex]));
            }
        }
        return runCodes.ToArray();
    }
}
