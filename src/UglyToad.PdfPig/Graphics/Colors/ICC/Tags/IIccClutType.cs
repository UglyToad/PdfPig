namespace IccProfileNet.Tags
{
    internal interface IIccClutType : IIccProcessor
    {
        /// <summary>
        /// Lookup the values in the color lookup table.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        double[] LookupClut(double[] input);

        double[] ApplyMatrix(double[] values, IccProfileHeader header);
    }
}
