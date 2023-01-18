namespace UglyToad.PdfPig.Filters.Jbig2
{
    /// <summary>
    /// This enumeration keeps the available logical operator defined in the JBIG2 ISO standard.
    /// </summary>
    internal enum CombinationOperator
    {
        OR, AND, XOR, XNOR, REPLACE
    }

    internal static class CombinationOperators
    {
        public static CombinationOperator TranslateOperatorCodeToEnum(short combinationOperatorCode)
        {
            switch (combinationOperatorCode)
            {
                case 0:
                    return CombinationOperator.OR;

                case 1:
                    return CombinationOperator.AND;

                case 2:
                    return CombinationOperator.XOR;

                case 3:
                    return CombinationOperator.XNOR;

                default:
                    return CombinationOperator.REPLACE;
            }
        }
    }
}
