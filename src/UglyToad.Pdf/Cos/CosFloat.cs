using System;
using System.Text.RegularExpressions;
using UglyToad.Pdf.Util;
using System.IO;
using System.Text;
using UglyToad.Pdf.Core;

namespace UglyToad.Pdf.Cos
{
    internal class CosFloat : CosBase, ICosNumber, ICosStreamWriter
    {
        private readonly decimal value;
        private readonly string valueAsString;

        /**
         * Constructor.
         *
         * @param aFloat The primitive float object that this object wraps.
         */
        public CosFloat(float aFloat)
        {
            // use a BigDecimal as intermediate state to avoid 
            // a floating point string representation of the float value
            value = new decimal(aFloat);
            valueAsString = RemoveNullDigits(value.ToString("G"));
        }

        private static readonly Regex ZeroNegativeDecimalPart = new Regex("^0\\.0*\\-\\d+");

        /**
         * Constructor.
         *
         * @param aFloat The primitive float object that this object wraps.
         *
         * @throws IOException If aFloat is not a float.
         */
        public CosFloat(string value)
        {
            try
            {
                (this.value, valueAsString) = CheckMinMaxValues(decimal.Parse(value), value);
            }
            catch (FormatException e)
            {
                if (ZeroNegativeDecimalPart.IsMatch(value))
                {
                    // PDFBOX-2990 has 0.00000-33917698
                    // PDFBOX-3369 has 0.00-35095424
                    // PDFBOX-3500 has 0.-262
                    try
                    {
                        valueAsString = "-" + valueAsString.ReplaceLimited("\\-", "", 1);
                        this.value = decimal.Parse(valueAsString);

                        (this.value, valueAsString) = CheckMinMaxValues(this.value, value);
                    }
                    catch (FormatException e2)
                    {
                        throw new ArgumentException($"Error expected floating point number actual=\'{value}\'", e2);
                    }
                }
                else
                {
                    throw new ArgumentException($"Error expected floating point number actual=\'{value}\'", e);
                }
            }
        }

        private static (decimal, string) CheckMinMaxValues(decimal currentValue, string currentValueAsString)
        {
            float floatValue = (float)currentValue;
            double doubleValue = (double)currentValue;
            bool valueReplaced = false;
            // check for huge values
            if (float.IsNegativeInfinity(floatValue) || float.IsPositiveInfinity(floatValue))
            {

                if (Math.Abs(doubleValue) > float.MaxValue)
                {
                    floatValue = float.MaxValue * (float.IsPositiveInfinity(floatValue) ? 1 : -1);
                    valueReplaced = true;
                }
            }
            // check for very small values
            else if (floatValue == 0 && doubleValue != 0)
            {
                // todo what is min normal?
                if (Math.Abs(doubleValue) < float.MinValue)
                {
                    floatValue = float.MinValue;
                    floatValue *= doubleValue >= 0 ? 1 : -1;
                    valueReplaced = true;
                }
            }
            if (valueReplaced)
            {
                return (new decimal(floatValue), RemoveNullDigits(currentValue.ToString("g")));
            }

            return (currentValue, currentValueAsString);
        }

        private static string RemoveNullDigits(string plainStringValue)
        {
            // remove fraction digit "0" only
            if (plainStringValue.IndexOf('.') > -1 && !plainStringValue.EndsWith(".0"))
            {
                while (plainStringValue.EndsWith("0") && !plainStringValue.EndsWith(".0"))
                {
                    plainStringValue = plainStringValue.Substring(0, plainStringValue.Length - 1);
                }
            }
            return plainStringValue;
        }

        public override bool Equals(object obj)
        {
            var cosFloat = obj as CosFloat;

            return Equals(cosFloat);
        }

        protected bool Equals(CosFloat other)
        {
            return value == other?.value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        /**
         * {@inheritDoc}
         */

        public override string ToString()
        {
            return "COSFloat{" + valueAsString + "}";
        }

        public void WriteToPdfStream(BinaryWriter output)
        {
            var encoding = Encoding.GetEncoding("ISO-8859-1");
            output.Write(encoding.GetBytes(valueAsString));
        }

        /**
         * visitor pattern double dispatch method.
         *
         * @param visitor The object to notify when visiting this object.
         * @return any object, depending on the visitor implementation, or null
         * @throws IOException If an error occurs while visiting this object.
         */

        public override object Accept(ICosVisitor visitor)
        {
            return visitor.VisitFromFloat(this);
        }

        public float AsFloat()
        {
            return (float)value;
        }

        public double AsDouble()
        {
            return (double) value;
        }

        public int AsInt()
        {
            return (int)value;
        }

        public long AsLong()
        {
            return (long) value;
        }

        public decimal AsDecimal()
        {
            return value;
        }
    }
}
