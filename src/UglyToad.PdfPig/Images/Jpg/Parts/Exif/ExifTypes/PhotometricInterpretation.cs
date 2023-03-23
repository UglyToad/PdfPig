using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace UglyToad.PdfPig.Images.Jpg.Parts.ExifTypes
{
    internal enum PhotometricInterpretation
    {
        [Description("White Is Zero")]
        WhiteIsZero = 0,
        [Description("Black Is Zero")]
        BlackIsZero = 1,
        RGB = 2,
        [Description("RGB Palette")]
        RGBPalette = 3,
        [Description("Transparency Mask")]
        TransparencyMask = 4,
        CMYK = 5,
        YCbCr = 6,
        CIELab = 8,
        ICCLab = 9,
        ITULab = 10,
        [Description("Color Filter Array")]
        ColorFilterArray = 32803,
        [Description("Pixar LogL")]
        PixarLogL = 32844,
        [Description("Pixar LogLuv")]
        PixarLogLuv = 32845,
        [Description("Sequential Color Filter")]
        SequentialColorFilter = 32892,
        [Description("Linear Raw")]
        LinearRaw = 34892,
        [Description("Depth Map")]
        DepthMap = 51177
    }
}
