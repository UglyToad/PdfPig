using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace UglyToad.PdfPig.Images.Jpg.Parts.ExifTypes
{
    /// <summary>
    /// ExIf ColorSpace
    /// </summary>
    internal enum ColorSpace
    {
        /// <summary>
        /// sRGB
        /// </summary>
        SRGB = 0x1,
        /// <summary>
        /// Adobe RGB
        /// </summary>
        AdobeRGB = 0x2,
        /// <summary>
        /// Wide Gamut RGB
        /// </summary>
        WideGamutRGB = 0xfffd,
        /// <summary>
        /// ICC Profile
        /// </summary>
        ICCProfile = 0xfffe,
        /// <summary>
        /// Uncalibrated
        /// /// </summary>
        Uncalibrated = 0xffff

    }
}
