namespace UglyToad.PdfPig.Function
{
    using System;

    /// <summary>
    /// The identity function.
    /// </summary>
    public class PdfFunctionTypeIdentity : PdfFunction
    {
        /// <inheritdoc/>
        public PdfFunctionTypeIdentity()
            : base(null, null)
        { }

        /// <inheritdoc/>
        public override int FunctionType => throw new NotSupportedException();

        /// <inheritdoc/>
        public override float[] Eval(float[] input)
        {
            return input;
        }
    }
}
