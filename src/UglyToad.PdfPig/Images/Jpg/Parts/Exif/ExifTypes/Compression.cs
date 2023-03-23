using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace UglyToad.PdfPig.Images.Jpg.Parts.ExifTypes
{
    internal enum Compression
    {
        Uncompressed = 1,
        [Description("CCITT 1D")]
        CCITT1D = 2,
        [Description("T4/Group 3 Fax")]
        T4Group3Fax = 3,
        [Description("T6/Group 4 Fax")]
        T6Group4Fax = 4,
        LZW = 5,
        [Description("JPEG (old-style)")]
        JPEGOldStyle = 6,
        JPEG = 7,
        [Description("Adobe Deflate")]
        AdobeDeflate = 8,
        [Description("JBIG B&W")]
        JBIGBW = 9,
        [Description("JBIG Color")]
        JBIGColor = 10,
        JPEG2 = 99,
        [Description("Kodak 262")]
        Kodak262 = 262,
        Next = 32766,
        [Description("Sony ARW Compressed")]
        SonyARWCompressed = 32767,
        [Description("Packed RAW")]
        PackedRAW = 32769,
        [Description("Samsung SRW Compressed")]
        SamsungSRWCompressed = 32770,
        CCIRLEW = 32771,
        [Description("Samsung SRW Compressed 2")]
        SamsungSRWCompressed2 = 32772,
        PackBits = 32773,
        [Description("Thunderscan")]
        Thunderscan = 32809,
        [Description("Kodak KDC Compressed")]
        KodakKDCCompressed = 32867,
        IT8CTPAD = 32895,
        IT8LW = 32896,
        IT8MP = 32897,
        IT8BL = 32898,
        PixarFilm = 32908,
        PixarLog = 32909,
        Deflate = 32946,
        DCS = 32947,
        [Description("Aperio JPEG 2000 YCbCr")]
        AperioJPEG2000YCbCr = 33003,
        [Description("Aperio JPEG 2000 RGB")]
        AperioJPEG2000RGB = 33005,
        JBIG = 34661,
        SGILog = 34676,
        SGILog24 = 34677,
        [Description("JPEG 2000")]
        JPEG2000 = 34712,
        [Description("Nikon NEF Compressed")]
        NikonNEFCompressed = 34713,
        [Description("JBIG2 TIFF FX")]
        JBIG2TIFFFX = 34715,
        [Description("Microsoft Document Imaging (MDI) Binary Level Codec")]
        MicrosoftDocumentImagingMDIBinaryLevelCodec = 34718,
        [Description("Microsoft Document Imaging (MDI) Progressive Transform Codec")]
        MicrosoftDocumentImagingMDIProgressiveTransformCodec = 34719,
        [Description("Microsoft Document Imaging (MDI) Vector")]
        MicrosoftDocumentImagingMDIVector = 34720,
        [Description("ESRI Lerc")]
        ESRILerc = 34887,
        [Description("Lossy JPEG")]
        LossyJPEG = 34892,
        LZMA2 = 34925,
        Zstd = 34926,
        WebP = 34927,
        PNG = 34933,
        JPEGXR = 34934,
        [Description("Kodak DCR Compressed")]
        KodakDCRCompressed = 65000,
        [Description("Pentax PEF Compressed")]
        PentaxPEFCompressed = 65535,

    }
}
