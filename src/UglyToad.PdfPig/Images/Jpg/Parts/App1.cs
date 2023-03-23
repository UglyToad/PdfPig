namespace UglyToad.PdfPig.Images.Jpg.Parts
{
    using Debug = System.Diagnostics.Debug;
    using System;
    using ContextForApp1Segment = Helpers.Context.ContextForApp1Segment;
    using Exif;
  
  


    // https://dev.exiv2.org/projects/exiv2/wiki/The_Metadata_in_JPEG_files


    internal static class App1
    {
        internal static void Parse(JpgBinaryStreamReader reader, ContextForApp1Segment context)
        {

            int segmentLength = reader.ReadInt16BE();
            if ((segmentLength >= 12) == false)
            {
                Debug.WriteLine($"Jpg App1 segment length is {segmentLength} Expected: >12.");
            }
            var pos = reader.BaseStream.Position;
            var segmentData = new byte[segmentLength - 2];
            var read = reader.Read(segmentData, 0, segmentData.Length);

            var headerType = Header(segmentData);

            switch (headerType)
            {
                case App1HeaderType.Exif:
                    Exif.Exif.GetExifProperties(context,segmentLength, segmentData);
                    break;
                case App1HeaderType.XmpMetadata:
                    XMP.Xmp.GetXmp(context, segmentLength, segmentData);
                    break;
                case App1HeaderType.Xap:
                    Debug.WriteLine($"Jpg App1 Xap Not yet implment");
                    break;


            }

            

        }
        enum App1HeaderType
        {
            Exif = 1,
            XmpMetadata = 2,
            Xap = 3
        };
        private static App1HeaderType Header(byte[] segmentData)
        {
            // Check for Exif signature
            {
                const string sig = "Exif\0\0";
                var abType = new byte[6];
                Buffer.BlockCopy(segmentData, 0, abType, 0, abType.Length);
                var segType = new System.Text.ASCIIEncoding().GetString(abType);
                if (segType == sig)
                {
                    return App1HeaderType.Exif;
                }
            }

            {
                const string sig = "http://ns.adobe.com/xap/1.0/\0";
                var abType = new byte[sig.Length];
                Buffer.BlockCopy(segmentData, 0, abType, 0, abType.Length);
                var segType = new System.Text.ASCIIEncoding().GetString(abType);
                if (segType == sig)
                {
                    // RDF (Resource Description Framework) implemented as an application of XML
                    // https://www.w3.org/TR/REC-rdf-syntax/
                    // XMP 3.2
                    return App1HeaderType.XmpMetadata;
                }
            }


            {
                var abType = new byte[10];
                Buffer.BlockCopy(segmentData, 0, abType, 0, abType.Length);
                var segType = new System.Text.ASCIIEncoding().GetString(abType);
                throw new Exception($"Jpg App1 segment. Expected signature bytes of 'Exif\\0' or 'http://ns.adobe.com/xap/1.0/\\0'. Got '{segType}'.");
            }

        }
    }

   
}
