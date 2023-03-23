namespace UglyToad.PdfPig.Images.Jpg.Parts
{
    using System;
    using System.Diagnostics;
    using UglyToad.PdfPig.Images.Jpg.Helpers;
    using ContextForQuantizationTableSpecification = Helpers.Context.ContextForQuantizationTableSpecification;

   
    /// <summary>
    /// Define Quantization Table
    /// </summary>
    internal class DefineQuantizationTable
    {
        static byte[] ZZSeq => JpgNaturalOrder.ZigZagSequenceOfQuantizedDCTCoefficients;

      
        internal static void ParseDqt(JpgBinaryStreamReader reader, ContextForQuantizationTableSpecification context)
        {

            var qtab = context.qtab;
            var qtavail = context.qtavail;

            var frameHeader = reader.DecodeFrameHeader();

            var Lq = frameHeader.length;

            byte[] t;

            while (frameHeader.remaining >= 65)
            {

                // Details - Pq and Tq (8 bits  = Pq (4 most significnat bits) + Tq (4 bits)

                // Pq: Quantization table element precision   (4 bits) Possible Values: 0 (Baseline DCT)
                // Specifies the precision of the Qk values.
                // Value 0 indicates 8-bit Qk values;     Pq shall be zero for 8 bit sample precision.
                // value 1 indicates 16 - bit Qk values. 

                // Tq: Quantization table destination identifier  (4 bits) Possible Values: 0-3 (Baseline DCT)
                // Specifies one of four possible destinations at the decoder into
                // which the quantization table shall be installed.

                var details = frameHeader.ReadByte();
                var Pq = (details & 0xF0) >> 4;   // 4 most significant bits (top nibble)
                if (Pq == 1) {                   
                    throw new NotSupportedException("Jpg 16 bit precison is not supported only 8 bit.");     
                }
                else if (Pq == 0)
                {
                    
                }
                 
                var Tq = details & 0x0F;        // 4 least significant bits (bottom nibble)
                if (Tq is 0 or 1 or 2 or 3 == false)
                {
                    Debug.WriteLine($"Warning: Jpg QuantizationTableSpecification Tq (Quantization table destination identifier) is : {Tq}. Expected: 1-3");
                }

                // Qk: Quantization table element  (8 or 16 bits) Possible Values: 1-244 or 1-65535
                // Specifies the kth element out of 64 elements, where k is the index in the zigzag ordering of the DCT coefficients.
                // The quantization elements shall be specified in zig - zag scan order.

                qtavail |= 1 << Tq;
                t = qtab[Tq];
                if (Pq == 0)
                {
                    // 8 bit - one byte per element
                    for (int i = 0; i < 64; i++)
                    {
                        t[i] = frameHeader.ReadByte();
                    }
                } else if (Pq == 1)
                {
                    // 16 bit - two bytes per element                    
                    for (int i = 0; i < 64; i++)
                    {                        
                        var msb = frameHeader.ReadByte(); // Most significant byte (msb)
                        var lsb = frameHeader.ReadByte(); // Least signifiant byte (lsb)

                        {
                            var us = (msb << 8) + lsb;
                            var result = (byte)Math.Round((255 * us) / (double)ushort.MaxValue);
                            t[i] = result;                            
                        }
                    }
                }
                
            }
            if (frameHeader.remaining != 0)
            {
                Debug.WriteLine($"Warning: Jpg QuantizationTableSpecification buffer not exhausted. Remaining: {frameHeader.remaining}. Expected: 0");
            }
            context.qtab = qtab;
            context.qtavail = qtavail;
        }        
    }
}
