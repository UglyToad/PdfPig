namespace UglyToad.PdfPig.Images.Jpg.Parts
{
    using System;
    using System.Diagnostics;
    using static UglyToad.PdfPig.Images.Jpg.Helpers.Context;
 
    internal static class DefineRestartInterval
    {
        internal static void ParseDefineRestartInterval(JpgBinaryStreamReader reader, ContextForDefineRestartInterval context)
        {
            
            // Lf: Frame header length (16 bits)   Possible Values: 8 + 3 × Nf
            // Specifies the length of the frame header
            var frameHeader = reader.DecodeFrameHeader(); // Get Length

            if (frameHeader.remaining < 2) { throw new Exception($"Jpg Restart Internval Segment length expected: 2 bytes. Got: {frameHeader.remaining}"); }


            // Ri:  Restart interval  (16 bits) Possible Values: 0-65535 (DCT Baseline) n x MCUR (Progressive DCT) (Lossless)
            // Specifies the number of MCU in the restart interval.
            var Ri = frameHeader.ReadInt16BE();             
            if (Ri == 0)
            {
                Debug.WriteLine($"Jpg reset interval defined as 0.");
            }
            context.rstinterval = Ri;
            
             
        }

    }
}
