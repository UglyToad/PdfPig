using System;
using System.Collections.Generic;
using System.Text;

namespace UglyToad.PdfPig.Content
{
    /// <summary>
    /// Direction of the text.
    /// </summary>
    public enum TextDirection
    {
        /// <summary>
        /// Text direction not known.
        /// </summary>
        Unknown,

        /// <summary>
        /// Usual text direction (Left to Right).
        /// </summary>
        Horizontal,

        /// <summary>
        /// Rotated text going down.
        /// </summary>
        Rotate90,

        /// <summary>
        /// Rotated text going up.
        /// </summary>
        Rotate270
    }
}
