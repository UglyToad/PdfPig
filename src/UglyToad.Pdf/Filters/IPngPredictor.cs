namespace UglyToad.Pdf.Filters
{
    internal interface IPngPredictor
    {
        byte[] Decode(byte[] input, int predictor, int colors, int bitsPerComponent, int columns);
    }
}