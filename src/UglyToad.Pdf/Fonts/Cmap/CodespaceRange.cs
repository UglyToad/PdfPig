using System;
using System.Collections.Generic;
using System.Text;

namespace UglyToad.Pdf.Fonts.Cmap
{
    /// <summary>
    ///  A codespace range is specified by a pair of codes of some particular length giving the lower and upper bounds of that range.
    /// </summary>
    public class CodespaceRange
    {
        private byte[] start;
        private byte[] end;
        private int startInt;
        private int endInt;

        public int CodeLength { get; private set; }

        /**
         * Creates a new instance of CodespaceRange.
         */
        public CodespaceRange()
        {
        }
        

        /** Getter for property end.
         * @return Value of property end.
         *
         */
        public byte[] getEnd()
        {
            return end;
        }

        /** Setter for property end.
         * @param endBytes New value of property end.
         *
         */
        void setEnd(byte[] endBytes)
        {
            end = endBytes;
            endInt = endBytes.ToInt(endBytes.Length);
        }

        /** Getter for property start.
         * @return Value of property start.
         *
         */
        public byte[] getStart()
        {
            return start;
        }

        /** Setter for property start.
         * @param startBytes New value of property start.
         *
         */
        void setStart(byte[] startBytes)
        {
            start = startBytes;
            CodeLength = start.Length;
            startInt = startBytes.ToInt(startBytes.Length);
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
                if (value >= startInt && value <= endInt)
                {
                    return true;
                }
            }
            return false;
        }

    }

}
