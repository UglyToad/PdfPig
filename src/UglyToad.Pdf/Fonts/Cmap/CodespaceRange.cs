namespace UglyToad.Pdf.Fonts.Cmap
{
    using System.Collections.Generic;

    /// <summary>
    ///  A codespace range is specified by a pair of codes of some particular length giving the lower and upper bounds of that range.
    /// </summary>
    public class CodespaceRange
    {
        public IReadOnlyList<byte> Start { get; }

        public IReadOnlyList<byte> End { get; }

        public int StartInt { get; }

        public int EndInt { get; }
        
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
        
        /**
         * Returns true if the given code bytes match this codespace range.
         */
        public bool matches(byte[] code)
        {
            return isFullMatch(code, code.Length);
        }

        /**
         * Returns true if the given code bytes match this codespace range.
         */
        public bool isFullMatch(byte[] code, int codeLen)
        {
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
