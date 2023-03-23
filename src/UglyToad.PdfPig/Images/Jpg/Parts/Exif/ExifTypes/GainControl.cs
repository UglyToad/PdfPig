using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace UglyToad.PdfPig.Images.Jpg.Parts.ExifTypes
{
    internal enum GainControl
    {
        None = 0,
        /// <summary>
        /// Low Gain Up
        /// </summary>
        LowGainUp = 1,
        /// <summary>
        /// High Gain Up
        /// </summary>
        HighGainUp = 2,
        /// <summary>
        /// Low Gain Down
        /// </summary>
        LowGainDown = 3,
        /// <summary>
        /// High Gain Down
        /// </summary>
        HighGainDown = 4
    }
}
