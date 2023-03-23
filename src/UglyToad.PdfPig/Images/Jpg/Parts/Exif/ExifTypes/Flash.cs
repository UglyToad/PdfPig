using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace UglyToad.PdfPig.Images.Jpg.Parts.ExifTypes
{
    internal enum Flash
    {
        [Description("No Flash")]
        NoFlash = 0x0,
        [Description("Fired")]
        Fired = 0x1,
        [Description("Fired, Return not detected")]
        FiredReturnNotDetected = 0x5,
        [Description("Fired, Return detected")]
        FiredReturnDetected = 0x7,
        [Description("On, Did not fire")]
        OnDidNotFire = 0x8,
        [Description("On, Fired")]
        OnFired = 0x9,
        [Description("On, Return not detected")]
        OnReturnNotDetected = 0xd,
        [Description("On, Return detected")]
        OnReturnDetected = 0xf,
        [Description("Off, Did not fire")]
        OffDidNotFire = 0x10,
        [Description("Off, Did not fire, Return not detected")]
        OffDidNotFireReturnNotDetected = 0x14,
        [Description("Auto, Did not fire")]
        AutoDidNotFire = 0x18,
        [Description("Auto, Fired")]
        AutoFired = 0x19,
        [Description("Auto, Fired, Return not detected")]
        AutoFiredReturnNotDetected = 0x1d,
        [Description("Auto, Fired, Return detected")]
        AutoFiredReturnDetected = 0x1f,
        [Description("No flash function")]
        NoFlashFunction = 0x20,
        [Description("Off, No flash function")]
        OffNoFlashFunction = 0x30,
        [Description("Fired, Red-eye reduction")]
        FiredRedEyeReduction = 0x41,
        [Description("Fired, Red-eye reduction, Return not detected")]
        FiredRedEyeReductionReturnNotDetected = 0x45,
        [Description("Fired, Red-eye reduction, Return detected")]
        FiredRedEyeReductionReturnDetected = 0x47,
        [Description("On, Red-eye reduction")]
        OnRedEyeReduction = 0x49,
        [Description("On, Red-eye reduction, Return not detected")]
        OnRedEyeRductionReturnNotDetected = 0x4d,
        [Description("On, Red-eye reduction, Return detected")]
        OnRedEyeReductionReturnDetected = 0x4f,
        [Description("Off, Red-eye reduction")]
        OffRedEyeReduction = 0x50,
        [Description("Auto, Did not fire, Red-eye reduction")]
        AutoDidNotFireRedEyeReduction = 0x58,
        [Description("Auto, Fired, Red-eye reduction")]
        AutoFiredRedEyeReduction = 0x59,
        [Description("Auto, Fired, Red-eye reduction, Return not detected")]
        AutoFiredRedEyeReductionReturnNotDetected = 0x5d,
        [Description("Auto, Fired, Red-eye reduction, Return detected")]
        AutoFiredRedEyeReductionReturnDetected = 0x5f
    }
}
