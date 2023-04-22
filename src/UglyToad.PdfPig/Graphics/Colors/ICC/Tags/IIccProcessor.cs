namespace IccProfileNet.Tags
{
    internal interface IIccProcessor
    {
        double[] Process(double[] input, IccProfileHeader header);
    }
}
