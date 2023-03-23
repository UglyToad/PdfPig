using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace UglyToad.PdfPig.Images.Jpg.Parts.ExifTypes
{
    internal enum SecurityClassification
    {
        Confidential = 'C',
        Restricted = 'R',
        Secret = 'S',
        [Description("Top Secret")]
        TopSecret = 'T',
        Unclassified = 'U'
    }
}
