using System;
using System.Linq;

namespace IccProfileNet.Tags
{
    internal sealed class IccCurveType : IccBaseCurveType
    {
        public const int CountOffset = 8;
        public const int CountLength = 4;

        private readonly uint _parametersCount;

        public CurveTypeType CurveType { get; }

        private readonly Func<double, double> _func;
        private readonly Lazy<double> _gamma;

        public IccCurveType(byte[] rawData)
        {
            string typeSignature = IccHelper.GetString(rawData, TypeSignatureOffset, TypeSignatureLength);

            if (typeSignature != "curv")
            {
                throw new ArgumentException(nameof(typeSignature));
            }

            RawData = rawData;
            Signature = "curv";

            // Count value specifying the number of entries (n) that follow
            // 8 to 11
            _parametersCount = IccHelper.ReadUInt32(RawData.Skip(CountOffset).Take(CountLength).ToArray());
            BytesRead = CountOffset + CountLength + (2 * (int)_parametersCount);

            _parameters = new Lazy<double[]>(() =>
            {
                double[] values = new double[_parametersCount];

                // Actual curve values starting with the zeroth entry and ending with the entry n 1
                // 12 to end
                // The curveType embodies a one-dimensional function which maps an input value in the domain of the function
                // to an output value in the range of the function.The domain and range values are in the range of 0,0 to 1,0.
                // - When n is equal to 0, an identity response is assumed.
                // - When n is equal to 1, then the curve value shall be interpreted as a gamma value, encoded as
                // u8Fixed8Number. Gamma shall be interpreted as the exponent in the equation y = x^g and not as an inverse.
                // - When n is greater than 1, the curve values(which embody a sampled one - dimensional function) shall be
                // defined as follows:
                //      - The first entry represents the input value 0,0, the last entry represents the input value 1,0, and intermediate
                //      entries are uniformly spaced using an increment of 1,0 / (n-1). These entries are encoded as uInt16Numbers
                //      (i.e. the values represented by the entries, which are in the range 0,0 to 1,0 are encoded in the range 0 to
                //      65 535). Function values between the entries shall be obtained through linear interpolation.
                if (_parametersCount == 0)
                {
                    // When n is equal to 0, an identity response is assumed.
                }
                else if (_parametersCount == 1)
                {
                    // When n is equal to 1, then the curve value shall be interpreted as a gamma value, encoded as
                    // u8Fixed8Number. Gamma shall be interpreted as the exponent in the equation y = x^g and not as an inverse.
                    // * If n = 1, the field length is 2 bytes and the value is encoded as a u8Fixed8Number
                    values[0] = IccHelper.ReadU8Fixed8Number(RawData
                        .Skip(CountOffset + CountLength)
                        .Take(IccHelper.UInt16Length).ToArray());
                }
                else
                {
                    // When n is greater than 1, the curve values(which embody a sampled one - dimensional function) shall be
                    // defined as follows:
                    // The first entry represents the input value 0,0, the last entry represents the input value 1,0, and intermediate
                    // entries are uniformly spaced using an increment of 1,0 / (n-1). These entries are encoded as uInt16Numbers
                    // (i.e. the values represented by the entries, which are in the range 0,0 to 1,0 are encoded in the range 0 to
                    // 65 535). Function values between the entries shall be obtained through linear interpolation.                
                    for (int c = 0; c < _parametersCount; c++)
                    {
                        values[c] = IccHelper.ReadUInt16(RawData
                            .Skip(CountOffset + CountLength + (IccHelper.UInt16Length * c))
                            .Take(IccHelper.UInt16Length)
                            .ToArray()) / (double)ushort.MaxValue;
                    }
                }
                return values;
            });

            _gamma = new Lazy<double>(() =>
            {
                if (_parametersCount != 1)
                {
                    throw new InvalidOperationException("TODO");
                }
                return Parameters[0];
            });

            switch (_parametersCount)
            {
                case 0:
                    CurveType = CurveTypeType.Identity;
                    _func = x => x;
                    break;

                case 1:
                    CurveType = CurveTypeType.Gamma;
                    _func = x => Math.Pow(x, _gamma.Value);
                    break;

                default:
                    CurveType = CurveTypeType.LinearInterpolation;
                    _func = x =>
                    {
                        // Interpolate
                        double index = (Parameters.Length - 1.0) * x;

                        bool hasDecimal = Math.Abs(index % 1) > double.Epsilon * 100;
                        if (!hasDecimal)
                        {
                            return Parameters[(int)index];
                        }

                        int indexInt = (int)Math.Floor(index);
                        double w = index - indexInt;
                        double y1 = Parameters[indexInt];
                        double y2 = Parameters[indexInt + 1];
                        return y1 + w * (y2 - y1);
                    };
                    break;
            }
        }

        public override double Process(double values)
        {
            return _func(values);
        }

        public override string ToString()
        {
            switch (CurveType)
            {
                case CurveTypeType.Identity:
                    return CurveType.ToString();

                default:
                    if (Parameters.Length > 1)
                    {
                        return $"{CurveType} ({Parameters.Length} points)";
                    }

                    return $"{CurveType} ({Math.Round(Parameters[0], 4)})";
            }
        }

        public enum CurveTypeType : byte
        {
            Identity = 0,
            Gamma = 1,
            LinearInterpolation = 2
        }
    }
}
