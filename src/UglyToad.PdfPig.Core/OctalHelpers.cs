namespace UglyToad.PdfPig.Core
{
    using System;

    /// <summary>
    /// Interprets numbers in octal format.
    /// </summary>
    public static class OctalHelpers
    {
        /// <summary>
        /// Read a short.
        /// </summary>
        public static short CharacterToShort(this char c)
        {
            return c switch {
                '0' => 0,
                '1' => 1,
                '2' => 2,
                '3' => 3,
                '4' => 4,
                '5' => 5,
                '6' => 6,
                '7' => 7,
                '8' => 8,
                '9' => 9,
                _ => throw new InvalidOperationException($"Could not convert the character {c} to a short.")
            };
        }
    }
}
