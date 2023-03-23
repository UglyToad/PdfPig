namespace UglyToad.PdfPig.Images.Jpg.Parts
{
    using System;
    using ContextForStartOfScan = Helpers.Context.ContextForStartOfScan;
    using BitReader = Helpers.BitReader;
    using System.Diagnostics;
    using System.Drawing;

    internal static class StartOfScan
    {
         
        internal static void ParseSos(JpgBinaryStreamReader reader, ContextForStartOfScan contextForStartOfScan) 
        {
            var components = contextForStartOfScan.comp;
            var vlctab = contextForStartOfScan.vlctab;
            var rstinterval = contextForStartOfScan.rstinterval;
            var qtab = contextForStartOfScan.qtab;

#if DEBUG
            var isOnDebug = UglyToad.PdfPig.Images.Jpg.Jpg.isOnDebug;
            if (isOnDebug) { Debug.WriteLine($"StartOfScan {reader.BaseStream.Position}"); }
#endif

            var frameHeader = reader.DecodeFrameHeader();
                 
            // Ns: Number of image components in scan
            // Specifies the number of source image components in the scan. The
            // value of Ns shall be equal to the number of sets of scan component specification parameters(Csj, Tdj, and Taj)
            // present in the scan header.
            var Ns = frameHeader.ReadByte();
            var NumberOfComponents = contextForStartOfScan.ncomp;
            if (frameHeader.length < (4 + 2 * NumberOfComponents)) { throw new Exception(); } // Syntax
            if (Ns != NumberOfComponents) { throw new Exception(); } // Unsupported


            for (int i = 0; i < NumberOfComponents; i++)
            {
                var component = components[i];

                // Csj: Scan component selector (8 bits) Possible Values: Ci values in frame headers
                // Selects which of the Nf image components specified in the frame parameters
                // shall be the jth component in the scan. 
                var Csj = frameHeader.ReadByte();
                if (Csj != component.cid) { throw new Exception(); } // Syntax

                // Entropy Coding Table Destination Select (ECTD) (8 bits - 2 x 4 bit values : Tdj and Taj)

                // Tdj: DC entropy coding table destination selector (4 bits) Possible Values: 0 or 1 (Baseline DCT)
                // Specifies one of four possible DC entropy coding table
                // destinations from which the entropy table needed for decoding of the DC coefficients of component Csj is
                // retrieved.The DC entropy table shall have been installed in this destination(see B.2.4.2 and B.2.4.3) by the
                // time the decoder is ready to decode the current scan.
                // This parameter specifies the entropy coding table destination for the lossless processes.

                // Taj: AC entropy coding table destination selector Possible Values: 0 or 1 (Baseline DCT)
                // Specifies one of four possible AC entropy coding table
                // destinations from which the entropy table needed for decoding of the AC coefficients of component Csj is
                // retrieved.The AC entropy table selected shall have been installed in this destination(see B.2.4.2 and B.2.4.3)
                // by the time the decoder is ready to decode the current scan.
                // This parameter is zero for the lossless processes.
                var ECTD = frameHeader.ReadByte();
                if ((ECTD & 0xEE) != 0) { throw new Exception(); } // Syntax
                component.dctabsel = ECTD >> 4;
                component.actabsel = (ECTD & 1) | 2;
            }

            // Ss: Start of spectral or predictor selection (8 bits) Possible Values: 0 (Baseline DCT)
            // In the DCT modes of operation, this parameter specifies the first
            // DCT coefficient in each block in zig - zag order which shall be coded in the scan. This parameter shall be set to
            // zero for the sequential DCT processes. In the lossless mode of operations this parameter is used to select the
            //predictor.
            var Ss = frameHeader.ReadByte();
            if (Ss != 0) { throw new Exception(); } // Syntax

            // Se: End of spectral selection (8 bits) Possible Values: 63  (Baseline DCT)
            // Specifies the last DCT coefficient in each block in zig-zag order which shall be
            // coded in the scan. This parameter shall be set to 63 for the sequential DCT processes. In the lossless mode of
            // operations this parameter has no meaning.It shall be set to zero.
            var Se = frameHeader.ReadByte();
            if (Se != 63) { throw new Exception(); } // Syntax

            // Approximation bit position  (ABP) (8 bits - 2 x 4 bit values : Ah and Al) Possible Values: 0 (Baseline DCT)

            // Ah: Successive approximation bit position high
            // This parameter specifies the point transform used in the
            // preceding scan(i.e.successive approximation bit position low in the preceding scan) for the band of coefficients
            // specified by Ss and Se.This parameter shall be set to zero for the first scan of each band of coefficients. In the
            // lossless mode of operations this parameter has no meaning.It shall be set to zero.

            // Al: Successive approximation bit position low or point transform
            // In the DCT modes of operation this
            // parameter specifies the point transform, i.e.bit position low, used before coding the band of coefficients
            // specified by Ss and Se. This parameter shall be set to zero for the seq
            var ABP = frameHeader.ReadByte();
            if (ABP != 0) { throw new Exception(); } // Syntax

            frameHeader.Skip(frameHeader.remaining); // pos: 323  length: 3
            // pos: 326 length: 0
            int mbx = 0;
            int mby = 0;
            var rstcount = rstinterval;
            int nextrst = 0;
            int[] block = new int[64];
            var bitreader = new BitReader(reader.BaseStream);
            while (true)
            {
                for (var i = 0; i < NumberOfComponents; ++i)  
                {
                    var component = components[i];
                    for (var sby = 0; sby < component.VSF; ++sby)  
                    {
                        for (var sbx = 0; sbx < component.HSF; ++sbx) 
                        {                             
                            var value = ((mby * component.VSF + sby) * component.stride + mbx * component.HSF + sbx) << 3;
                            var isSuccess = DecodeBlock(bitreader, component, value, vlctab, qtab,ref block);
                            if (isSuccess == false) { throw new Exception(); }
                        }
                    }
                }
                if (++mbx >= contextForStartOfScan.mbwidth)
                {
                    mbx = 0;
                    if (++mby >= contextForStartOfScan.mbheight)
                    {
                        break;
                    }
                }
                
                if (rstinterval != 0 && (--rstcount) == 0)
                {
#if DEBUG
                    UglyToad.PdfPig.Images.Jpg.Jpg.hasRestart = true;
                    //Debug.WriteLine($"RestartInterval position: {reader.BaseStream.Position}");
#endif

                    bitreader.Align();
                    var i = bitreader.Read(16);
                    //Debug.WriteLine($"RestartInterval i: {i}");
                    if (((i & 0xFFF8) != 0xFFD0) || ((i & 7) != nextrst)) { throw new Exception(); } // SYNTAX
                    nextrst = (nextrst + 1) & 7;
                    rstcount = contextForStartOfScan.rstinterval;
                    //Debug.WriteLine($"RestartInterval {rstcount}");
                    for (i = 0; i < components.Length; ++i)
                    {
                        components[i].dcpred = 0;
                    }
                }
                 
            }


        }

        public static readonly byte[] ZZSeq = Helpers.JpgNaturalOrder.ZigZagSequenceOfQuantizedDCTCoefficients;
         

        private static bool DecodeBlock(BitReader reader, Component component, int outv, HuffmanTreeNode[][] vlctab, byte[][]qtab, ref int[]block)
        { 

            byte discard = 0;
            byte code = 0;
            int value, coef = 0;
            block = new int[64];
            var details = GetVariableLengthCode(reader, vlctab[component.dctabsel], ref discard);
            component.dcpred += details.value;
            block[0] = (component.dcpred) * qtab[component.qtsel][0];
  
            do
            {
                (value, var isSuccess) = GetVariableLengthCode(reader,vlctab[component.actabsel], ref code);

                if (code == 0) { break; }  // EOB
                if ((code & 0x0F) == 0 && (code != 0xF0)) { throw new Exception(); } //SYNTAX
                coef += (code >> 4) + 1;

                if (coef > 63) { throw new Exception($"Jpg DecodeBlock expected coef <= 63. Got: {coef}."); } //SYNTAX
                block[(int)ZZSeq[coef]] = value * qtab[component.qtsel][coef];
                 
            } while (coef < 63);
            for (coef = 0; coef < 64; coef += 8)
            {
                GetRowIDCT(block, coef);
            }
            for (coef = 0; coef < 8; ++coef)
            {
                GetColIDCT(block, coef, component.pixels, outv + coef, component.stride);
            }
            return true;
        }
         

        public static readonly int W1 = 2841;
        public static readonly int W2 = 2676;
        public static readonly int W3 = 2408;
        public static readonly int W5 = 1609;
        public static readonly int W6 = 1108;
        public static readonly int W7 = 565;
        public static void GetRowIDCT(int[] blk, int coef)
        {            
            int x0, x1, x2, x3, x4, x5, x6, x7, x8;
            if (((x1 = blk[coef + 4] << 11)
                | (x2 = blk[coef + 6])
                | (x3 = blk[coef + 2])
                | (x4 = blk[coef + 1])
                | (x5 = blk[coef + 7])
                | (x6 = blk[coef + 5])
                | (x7 = blk[coef + 3])) == 0)
            {
                
                blk[coef] = blk[coef + 1] = blk[coef + 2] = blk[coef + 3] = blk[coef + 4] = blk[coef + 5] = blk[coef + 6] = blk[coef + 7] = blk[coef] << 3;
                 
                return;
            }
            x0 = (blk[coef] << 11) + 128;
            x8 = W7 * (x4 + x5);
            x4 = x8 + (W1 - W7) * x4;
            x5 = x8 - (W1 + W7) * x5;
            x8 = W3 * (x6 + x7);
            x6 = x8 - (W3 - W5) * x6;
            x7 = x8 - (W3 + W5) * x7;
            x8 = x0 + x1;
            x0 -= x1;
            x1 = W6 * (x3 + x2);
            x2 = x1 - (W2 + W6) * x2;
            x3 = x1 + (W2 - W6) * x3;
            x1 = x4 + x6;
            x4 -= x6;
            x6 = x5 + x7;
            x5 -= x7;
            x7 = x8 + x3;
            x8 -= x3;
            x3 = x0 + x2;
            x0 -= x2;
            x2 = (181 * (x4 + x5) + 128) >> 8;
            x4 = (181 * (x4 - x5) + 128) >> 8;
            blk[coef] = (x7 + x1) >> 8;
            blk[coef + 1] = (x3 + x2) >> 8;
            blk[coef + 2] = (x0 + x4) >> 8;
            blk[coef + 3] = (x8 + x6) >> 8;
            blk[coef + 4] = (x8 - x6) >> 8;
            blk[coef + 5] = (x0 - x4) >> 8;
            blk[coef + 6] = (x3 - x2) >> 8;
            blk[coef + 7] = (x7 - x1) >> 8;             
        }


        public static void GetColIDCT(int[] blk, int coef, byte[] pixels, int outv, int stride)
        {
            int x0, x1, x2, x3, x4, x5, x6, x7, x8;
            if (((x1 = blk[coef + 8 * 4] << 8)
                | (x2 = blk[coef + 8 * 6])
                | (x3 = blk[coef + 8 * 2])
                | (x4 = blk[coef + 8 * 1])
                | (x5 = blk[coef + 8 * 7])
                | (x6 = blk[coef + 8 * 5])
                | (x7 = blk[coef + 8 * 3])) == 0)
            {
                x1 = Clip(((blk[coef] + 32) >> 6) + 128);
                for (x0 = 8; x0 != 0; --x0)
                {
                    pixels[outv] = (byte)x1;
                    outv += stride;
                }
                return;
            }
            x0 = (blk[coef] << 8) + 8192;
            x8 = W7 * (x4 + x5) + 4;
            x4 = (x8 + (W1 - W7) * x4) >> 3;
            x5 = (x8 - (W1 + W7) * x5) >> 3;
            x8 = W3 * (x6 + x7) + 4;
            x6 = (x8 - (W3 - W5) * x6) >> 3;
            x7 = (x8 - (W3 + W5) * x7) >> 3;
            x8 = x0 + x1;
            x0 -= x1;
            x1 = W6 * (x3 + x2) + 4;
            x2 = (x1 - (W2 + W6) * x2) >> 3;
            x3 = (x1 + (W2 - W6) * x3) >> 3;
            x1 = x4 + x6;
            x4 -= x6;
            x6 = x5 + x7;
            x5 -= x7;
            x7 = x8 + x3;
            x8 -= x3;
            x3 = x0 + x2;
            x0 -= x2;
            x2 = (181 * (x4 + x5) + 128) >> 8;
            x4 = (181 * (x4 - x5) + 128) >> 8;
            pixels[outv] = Clip(((x7 + x1) >> 14) + 128); outv += stride;
            pixels[outv] = Clip(((x3 + x2) >> 14) + 128); outv += stride;
            pixels[outv] = Clip(((x0 + x4) >> 14) + 128); outv += stride;
            pixels[outv] = Clip(((x8 + x6) >> 14) + 128); outv += stride;
            pixels[outv] = Clip(((x8 - x6) >> 14) + 128); outv += stride;
            pixels[outv] = Clip(((x0 - x4) >> 14) + 128); outv += stride;
            pixels[outv] = Clip(((x3 - x2) >> 14) + 128); outv += stride;
            pixels[outv] = Clip(((x7 - x1) >> 14) + 128);
        }

        public static byte Clip(int x)
        {
            return (byte)((x < 0) ? 0 : ((x > 0xFF) ? 0xFF : (byte)x));
        }


        private static (int value, bool isSuccess) GetVariableLengthCode(BitReader reader, HuffmanTreeNode[] vlc, ref byte code)
        {
            int value = reader.EnsureData(16); 
            int bits = vlc[value].bits;
            if (bits == 0) { 
                return (0, false); 
            }
            reader.Skip(bits); 
            value = vlc[value].code;
            code = (byte)value;
            bits = value & 15;
            if (bits == 0)
            {
                return (0, true);
            }
            value = reader.Read(bits);
            if (value < (1 << (bits - 1)))
            {
                value += ((-1) << bits) + 1;
            }

            return (value, true);

        }

        private static int ShowBits(JpgBinaryStreamReader reader, int bits)
        {
            return GetNumberOfBitsFromStream(reader, bits);
        }

        private static int GetNumberOfBitsFromStream(JpgBinaryStreamReader reader, int bits)
        {
            
            return 0;
        }         
    }    
}
