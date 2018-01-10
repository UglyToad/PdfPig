namespace UglyToad.PdfPig.Cos
{
    using System;

    internal static class CosNumberFactory
    {
        /**
     * This factory method will get the appropriate number object.
     *
     * @param number The string representation of the number.
     *
     * @return A number object, either float or int.
     *
     * @throws IOException If the string is not a number.
     */
        public static ICosNumber get(string value)
        {
            if (value.Length == 1)
            {
                char digit = value[0];
                if ('0' <= digit && digit <= '9')
                {
                    return CosInt.Get(digit - '0');
                }
                else if (digit == '-' || digit == '.')
                {
                    // See https://issues.apache.org/jira/browse/PDFBOX-592
                    return CosInt.Zero;
                }
                else
                {
                    throw new ArgumentException($"Not a number: {value}");
                }
            }
            else
            {
                if (value.IndexOf('.') == -1 && (value.ToLower().IndexOf('e') == -1))
                {
                    try
                    {
                        if (value[0] == '+')
                        {
                            return CosInt.Get(long.Parse(value.Substring(1)));
                        }
                        return CosInt.Get(long.Parse(value));
                    }
                    catch (FormatException)
                    {
                        // might be a huge number, see PDFBOX-3116
                        return new CosFloat(value);
                    }
                }

                return new CosFloat(value);
            }
        }
    }
}