namespace UglyToad.Pdf.Util
{
    internal class ParsePosition
    {
        public int Index { get; set; }

        public int ErrorIndex { get; set; }

        public ParsePosition(int index)
        {
            Index = index;
            ErrorIndex = -1;
        }

        public override string ToString()
        {
            return $"{Index} (Error: {ErrorIndex})";
        }
    }
}