﻿namespace UglyToad.PdfPig.Images.Jpg.Helpers
{     
    internal static class JpgNaturalOrder
    {
        // Zig-zag sequence of quantized DCT coefficients
        // see Figure A.6 Page 30 JPEG ISO/IEC 10918-1 : 1993(E)  Table B.1
        // https://www.w3.org/Graphics/JPEG/itu-t81.pdf
        internal static readonly byte[] ZigZagSequenceOfQuantizedDCTCoefficients = new byte[] {
             0,  1,  8, 16,  9,  2,  3, 10,
            17, 24, 32, 25, 18, 11,  4,  5,
            12, 19, 26, 33, 40, 48, 41, 34,
            27, 20, 13,  6,  7, 14, 21, 28,
            35, 42, 49, 56, 57, 50, 43, 36,
            29, 22, 15, 23, 30, 37, 44, 51,
            58, 59, 52, 45, 38, 31, 39, 46,
            53, 60, 61, 54, 47, 55, 62, 63
        };
    }
}
