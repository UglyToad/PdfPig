namespace UglyToad.Pdf.ContentStream
{
    public struct IndirectReference
    {
        public long ObjectNumber { get; }

        public int Generation { get; }

        public IndirectReference(long objectNumber, int generation)
        {
            ObjectNumber = objectNumber;
            Generation = generation;
        }
    }
}