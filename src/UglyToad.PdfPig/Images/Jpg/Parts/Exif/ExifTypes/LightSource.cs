using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace UglyToad.PdfPig.Images.Jpg.Parts.ExifTypes
{
    internal enum LightSource
    {
        Unknown = 0,
        Fluorescent = 2,
        Daylight = 1,
        Flash = 4,
        [Description("Tungsten (Incandescent)")]
        TungstenIncandescent = 3,
        [Description("Fine Weather")]
        FineWeather = 9,
        Cloudy = 10,
        Shade = 11,
        [Description("Daylight Fluorescent")]
        DaylightFluorescent = 12,
        [Description("Day White Fluorescent")]
        DayWhiteFluorescent = 13,
        [Description("Cool White Fluorescent")]
        CoolWhiteFluorescent = 14,
        [Description("White Fluorescent")]
        WhiteFluorescent = 15,
        [Description("Warm White Fluorescent")]
        WarmWhiteFluorescent = 16,
        [Description("Standard Light A")]
        StandardLightA = 17,
        [Description("Standard Light B")]
        StandardLightB = 18,
        [Description("Standard Light C")]
        StandardLightC = 19,
        D55 = 20,
        D65 = 21,
        D75 = 22,
        D50 = 23,
        [Description("ISO Studio Tungsten")]
        ISOStudioTungsten = 24,
        Other = 255
    }
}
