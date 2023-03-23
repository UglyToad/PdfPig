using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace UglyToad.PdfPig.Images.Jpg.Parts.ExifTypes
{
    internal enum Predictor
    {
        None = 1,
        [Description("Horizontal differencing")]
        HorizontalDifferencing = 2,
        [Description("Floating point")]
        FloatingPoint = 3,
        [Description("Horizontal difference X2")]
        HorizontalDifferenceX2 = 34892,
        [Description("Horizontal difference X4")]
        HorizontalDifferenceX4 = 34893,
        [Description("Floating point X2")]
        FloatingPointX2 = 34894,
        [Description("Floating point X4")]
        FloatingPointX4 = 34895
    }
}
