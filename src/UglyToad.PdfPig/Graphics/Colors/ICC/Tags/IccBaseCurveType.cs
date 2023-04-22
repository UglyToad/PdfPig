using System;

namespace IccProfileNet.Tags
{
    internal abstract class IccBaseCurveType : IccTagTypeBase
    {
        /// <summary>
        /// Curve type signature.
        /// </summary>
        public string Signature { get; protected set; }

        protected Lazy<double[]> _parameters;
        /// <summary>
        /// TODO
        /// </summary>
        public double[] Parameters => _parameters.Value;

        public int BytesRead { get; protected set; }

        public abstract double Process(double values);

        /// <summary>
        /// TODO
        /// </summary>
        public static IccBaseCurveType Parse(byte[] bytes)
        {
            string typeSignature = IccHelper.GetString(bytes, TypeSignatureOffset, TypeSignatureLength);
            switch (typeSignature)
            {
                case "curv":
                    return new IccCurveType(bytes);

                case "para":
                    return new IccParametricCurveType(bytes);

                default:
                    throw new InvalidOperationException(typeSignature);
            }
        }
    }
}
