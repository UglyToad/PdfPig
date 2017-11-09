namespace UglyToad.Pdf.Fonts.Cmap
{
    public class CidRange
    {
        private readonly char from;

        private readonly char to;

        private readonly int cid;

        public CidRange(char from, char to, int cid)
        {
            this.from = from;
            this.to = to;
            this.cid = cid;
        }

        /// <summary>
        /// Maps the given Unicode character to the corresponding CID in this range.
        /// </summary>
        /// <param name="ch">Unicode character</param>
        /// <returns>corresponding CID, or -1 if the character is out of range</returns>
        public int Map(char ch)
        {
            if (from <= ch && ch <= to)
            {
                return cid + (ch - from);
            }
            return -1;
        }

        /// <summary>
        /// Maps the given CID to the corresponding Unicode character in this range.
        /// </summary>
        /// <param name="code">CID</param>
        /// <returns>corresponding Unicode character, or -1 if the CID is out of range</returns>
        public int Unmap(int code)
        {
            if (cid <= code && code <= cid + (to - from))
            {
                return from + (code - cid);
            }
            return -1;
        }

    }

}
