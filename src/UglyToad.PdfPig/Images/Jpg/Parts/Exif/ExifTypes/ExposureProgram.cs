using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace UglyToad.PdfPig.Images.Jpg.Parts.ExifTypes
{
    internal enum ExposureProgram
    {
        [Description("Not Defined")]
        NotDefined = 0,
        Manual = 1,
        ProgramAE = 2,
        [Description("Aperture-priority AE")]
        AperturePriorityAE = 3,
        [Description("Shutter speed priority AE")]
        ShutterSpeedPriorityAE = 4,
        [Description("Creative (Slow speed)")]
        CreativeSlowSpeed = 5,
        [Description("Action (High speed)")]
        ActionHighSpeed = 6,
        Portrait = 7,
        Landscape = 8,
        Bulb = 9
    }
}
