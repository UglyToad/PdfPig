namespace UglyToad.PdfPig.Images.Jpg.Parts
{
    using Debug = System.Diagnostics.Debug;
    using System;
    using ContextForApp0JFIFSegment = Helpers.Context.ContextForApp0JFIFSegment;
    internal static class App0JFIF  
    {
        internal static void Parse(JpgBinaryStreamReader reader, ContextForApp0JFIFSegment context )
        {

            int length = reader.ReadInt16BE();
            if ((length >= 12 /*JFIF*/|| length >=6 /*JFXX*/) == false)
            {
                Debug.WriteLine($"Jpg App0 segment length is {length} Expected: >6.");
            }
            var pos = reader.BaseStream.Position;
            var ab = new byte[length - 2];
            var read = reader.Read(ab, 0, ab.Length);
            if (read != ab.Length)
            {
                Debug.WriteLine($"Jpg AppA0 failed to read start of segment. Read: {read} Expected: 12.");
                return;
            }

            // The text ‘JIFI’ as a five-character ASCII big-endian string
            var abType = new byte[4];
            Buffer.BlockCopy(ab, 0, abType, 0, abType.Length);
            var segType = new System.Text.ASCIIEncoding().GetString(abType);
            if (segType == "JFXX")
            {
                switch (ab[5])
                {
                    case 0x10:
                        // THUMB_JPEG
                        break;
                    case 0x11:
                        // THUMB_PALETTE
                        break;
                    case 0x13:
                        // THUMB_RGB
                        break;
                    default:
                        // JTRC_JFIF_EXTENSION
                        break;
                }
                return; 
            }
            if (segType != "JFIF")
            {                
                Debug.WriteLine($"Jpg App0 Expected 'JFIF' or 'JFXX' as type in Application Specific 0 segment. Got: '{segType}'");
                return;
            }
            if (length < 12 /*JFIF*/)
            {
                Debug.WriteLine($"Jpg App0 JFIF segment length is {length} Expected: >=12.");
                return;
            }

            var versionMajor = ab[5];
            var versionMinor = ab[6];
            var density_unit = ab[7];
            var X_density = ab[9] + (ab[8]<<8);
            var Y_density = ab[11] + (ab[10] << 8);

            context.hasApp0segment = true;
            context.App0versionMajor = versionMajor;
            context.App0versionMinor = versionMinor;
            context.App0density_unit = density_unit;
            context.App0X_density = X_density;
            context.App0Y_density = Y_density;
            return;
        } 
    }
}
