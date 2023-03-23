using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace UglyToad.PdfPig.Images.Jpg.Parts.ExifTypes
{
    internal enum FileSource : byte
    {
        [Description("Film Scanner")]
        FilmScanner = 1,
        [Description("Reflection Print Scanner")]
        ReflectionPrintScanner = 2,
        [Description("Digital Camera")]
        DigitalCamera = 3
    }
}
