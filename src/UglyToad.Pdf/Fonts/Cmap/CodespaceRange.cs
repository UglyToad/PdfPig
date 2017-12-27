namespace UglyToad.Pdf.Fonts.Cmap
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///  A codespace range is specified by a pair of codes of some particular length giving the lower and upper bounds of that range.
    /// </summary>
    public class CodespaceRange
    {
        /// <summary>
        /// The lower-bound of this range.
        /// </summary>
        public IReadOnlyList<byte> Start { get; }

        /// <summary>
        /// The upper-bound of this range.
        /// </summary>
        public IReadOnlyList<byte> End { get; }

        public int StartInt { get; }

        public int EndInt { get; }

        /// <summary>
        /// The number of bytes for numbers in this range.
        /// </summary>
        public int CodeLength { get; }

        /// <summary>
        /// Creates a new instance of <see cref="CodespaceRange"/>.
        /// </summary>
        public CodespaceRange(IReadOnlyList<byte> start, IReadOnlyList<byte> end)
        {
            Start = start;
            End = end;
            StartInt = start.ToInt(start.Count);
            EndInt = end.ToInt(end.Count);
            CodeLength = start.Count;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the given code bytes match this codespace range.
        /// </summary>
        public bool Matches(byte[] code)
        {
            if (code == null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            return IsFullMatch(code, code.Length);
        }

        /// <summary>
        /// Returns true if the given code bytes match this codespace range.
        /// </summary>
        public bool IsFullMatch(byte[] code, int codeLen)
        {
            if (code == null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            // code must be the same length as the bounding codes
            if (codeLen == CodeLength)
            {
                int value = code.ToInt(codeLen);
                if (value >= StartInt && value <= EndInt)
                {
                    return true;
                }
            }
            return false;
        }

    }

}
