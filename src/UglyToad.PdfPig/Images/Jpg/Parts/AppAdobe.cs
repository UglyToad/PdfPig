namespace UglyToad.PdfPig.Images.Jpg.Parts
{
    using Debug = System.Diagnostics.Debug;
    using System;
    using static UglyToad.PdfPig.Images.Jpg.Helpers.Context;

    internal static class AppAdobe
    {
        internal static void ParseAppAdobe(JpgBinaryStreamReader reader, ContextForAdobeAppSegment context)
        {

            int length = reader.ReadInt16BE();
            if (length != 14)
            {
                Debug.WriteLine($"Jpg AppAdobe segment length is {length} Expected: 14.");
            }
            var pos = reader.BaseStream.Position;
            var ab = new byte[length - 2];
            var read = reader.Read(ab, 0, ab.Length);
            if (read != ab.Length)
            {
                Debug.WriteLine($"Jpg AppAdobe failed to read Adobe Application-Specific JPEG segment. Read: {read} Expected: 12.");
                return;
            }

            // The text ‘Adobe’ as a five-character ASCII big-endian string
            var abVendor = new byte[5];
            Buffer.BlockCopy(ab, 0, abVendor, 0, abVendor.Length);
            var vendor = new System.Text.ASCIIEncoding().GetString(abVendor);
            if (vendor != "Adobe")
            {
                // DCTDecode ignores and skips any APPE marker segment that does not begin
                // with the ‘Adobe’ 5 - character string.
                Debug.WriteLine($"Jpg AppAdobe Expected 'Adobe' as vendor in Adobe Application-Specific segment. Got: '{vendor}'");
                return;
            }

            // Two-byte DCTEncode/DCTDecode version number
            var versionDctEncodeDecode = ab[6] + (ab[5] << 8);

            // Two-byte flags0 0x8000 bit: Encoder used Blend=1 downsampling
            var flag0 = ab[8] + (ab[7] << 8);
            // Two-byte flags1
            var flag1 = ab[10] + (ab[9] << 8);
            // One-byte color transform code
            // The default is to use the YCC-to-RGB [color]transform
            //      0 = CMYK             
            //      1== YCCK
            var colorTransformCode = ab[11];

            // The convention for flags0 and flags1 is that 0 bits are benign.
            // 1 bits in flags0 pass information that is possibly useful but not essential for decoding.
            // 1 bits in flags1 pass information essential for decoding.
            // DCTDecode could reject a compressed imageif there are 1 bits in flags1 or color transform codes that it cannot interpret
            // The current implementation will reject only if the Picky option is non-zero.
             
            Debug.WriteLine($"versionDctEncodeDecode: 0x{versionDctEncodeDecode:X},flag0: 0x{flag0:X} ,flag1: 0x{flag1:X},colorTransformCode: 0x{colorTransformCode:X}");

#if DEBUG
            UglyToad.PdfPig.Images.Jpg.Jpg.hasJpgEndOfStreamWithoutEndOfImageMarker = true;
            UglyToad.PdfPig.Images.Jpg.Jpg.hasAdobeAppSegment = true;
            UglyToad.PdfPig.Images.Jpg.Jpg.AdobeAppSegmentTransformCode= colorTransformCode; 
#endif

            context.hasAdobeSegment = true;
            context.versionDctEncodeDecode = versionDctEncodeDecode;
            context.flag0 = flag0;
            context.flag1 = flag1;
            context.colorTransformCode = colorTransformCode;            
            return;
        } 
    }
}
