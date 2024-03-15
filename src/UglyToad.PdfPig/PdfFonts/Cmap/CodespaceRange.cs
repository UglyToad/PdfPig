namespace UglyToad.PdfPig.PdfFonts.Cmap
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///  A codespace range is specified by a pair of codes of some particular length giving the lower and upper bounds of that range.
    /// </summary>
    internal class CodespaceRange
    {
        /// <summary>
        /// The lower-bound of this range.
        /// </summary>
        public IReadOnlyList<byte> Start { get; }

        /// <summary>
        /// The upper-bound of this range.
        /// </summary>
        public IReadOnlyList<byte> End { get; }

        /// <summary>
        /// The lower-bound of this range as an integer.
        /// </summary>
        public int StartInt { get; }

        /// <summary>
        /// The upper-bound of this range as an integer.
        /// </summary>
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
            if (code is null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            return IsFullMatch(code, code.Length);
        }

        /// <summary>
        /// Returns true if the given code bytes match this codespace range.
        /// </summary>
        public bool IsFullMatch(byte[] code, int codeLength)
        {
            if (code is null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            // the code must be the same length as the bounding codes
            if (codeLength != CodeLength)
            {
                return false;
            }

            var value = code.ToInt(codeLength);
            if (value >= StartInt && value <= EndInt)
            {
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return $"Length {CodeLength}: {StartInt} -> {EndInt}";
        }
    }
}
