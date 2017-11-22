namespace UglyToad.Pdf.Graphics.Operations
{
    using System;

    internal class ModifyTransformationMatrix : IGraphicsStateOperation
    {
        public const string Symbol = "cm";

        public string Operator => Symbol;

        public decimal[] Value { get; }

        public ModifyTransformationMatrix(decimal[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length != 6)
            {
                throw new ArgumentException("The cm operator must pass 6 numbers. Instead got: " + value);
            }
            Value = value;
        }

        public override string ToString()
        {
            return $"{Value[0]} {Value[1]} {Value[2]} {Value[3]} {Value[4]} {Value[5]} {Symbol}";
        }
    }
}