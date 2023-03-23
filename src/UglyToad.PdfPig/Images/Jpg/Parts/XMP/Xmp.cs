namespace UglyToad.PdfPig.Images.Jpg.Parts.XMP
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using static UglyToad.PdfPig.Images.Jpg.Helpers.Context;

    internal class Xmp
    {
        internal static void GetXmp(ContextForApp1Segment context, int segmentLength, byte[] segmentData)
        {
            const string sig = "http://ns.adobe.com/xap/1.0/\0";
            byte[]rdp = new byte[segmentLength - sig.Length];

            Buffer.BlockCopy(segmentData,sig.Length,rdp,0, rdp.Length-2);
            var rdpString = new ASCIIEncoding().GetString(rdp);

            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(rdpString);

            context.XMP = doc;
        }
    }
}
