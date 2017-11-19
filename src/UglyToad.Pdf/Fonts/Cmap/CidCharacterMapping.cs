namespace UglyToad.Pdf.Fonts.Cmap
{
    public class CidCharacterMapping
    {
        public int Source { get; }
        public int Destination { get; }

        public CidCharacterMapping(int source, int destination)
        {
            Source = source;
            Destination = destination;
        }
    }
}
