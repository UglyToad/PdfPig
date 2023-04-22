using System;
using System.Linq;

namespace IccProfileNet.Tags
{
    internal sealed class IccParametricCurveType : IccBaseCurveType
    {
        public const int FunctionTypeOffset = 8;
        public const int FunctionTypeLength = 2;
        public const int ParametersOffset = 12;

        public ushort FunctionType { get; }

        private readonly Func<double, double> _func;
        private readonly Lazy<double> _g;
        private readonly Lazy<double> _a;
        private readonly Lazy<double> _b;
        private readonly Lazy<double> _c;
        private readonly Lazy<double> _d;
        private readonly Lazy<double> _e;
        private readonly Lazy<double> _f;

        public IccParametricCurveType(byte[] rawData)
        {
            string typeSignature = IccHelper.GetString(rawData, TypeSignatureOffset, TypeSignatureLength);

            if (typeSignature != "para")
            {
                throw new ArgumentException(nameof(typeSignature));
            }

            RawData = rawData;
            Signature = "para";

            // Encoded value of the function type
            // 8 to 9
            FunctionType = IccHelper.ReadUInt16(RawData
                .Skip(FunctionTypeOffset)
                .Take(FunctionTypeLength)
                .ToArray());

            int parametersCount = GetParametersCount(FunctionType);

            int fieldLength = parametersCount * 4;
            BytesRead = ParametersOffset + fieldLength;

            _parameters = new Lazy<double[]>(() =>
            {
                // One or more parameters (see Table 67)
                // 12 to end
                return IccHelper.Reads15Fixed16Array(RawData
                    .Skip(ParametersOffset)
                    .Take(fieldLength)
                    .ToArray());
            });

            switch (FunctionType)
            {
                case 0:
                    _g = new Lazy<double>(() => Parameters[0]);
                    _func = x => Math.Pow(x, _g.Value);
                    break;

                case 1:
                    _g = new Lazy<double>(() => Parameters[0]);
                    _a = new Lazy<double>(() => Parameters[1]);
                    _b = new Lazy<double>(() => Parameters[2]);
                    _func = x =>
                    {
                        if (x >= -_b.Value / _a.Value)
                        {
                            return Math.Pow(_a.Value * x + _b.Value, _g.Value);
                        }
                        return 0.0;
                    };
                    break;

                case 2:
                    _g = new Lazy<double>(() => Parameters[0]);
                    _a = new Lazy<double>(() => Parameters[1]);
                    _b = new Lazy<double>(() => Parameters[2]);
                    _c = new Lazy<double>(() => Parameters[3]);
                    _func = x =>
                    {
                        if (x >= -_b.Value / _a.Value)
                        {
                            return Math.Pow(_a.Value * x + _b.Value, _g.Value) + _c.Value;
                        }
                        return _c.Value;
                    };
                    break;

                case 3:
                    _g = new Lazy<double>(() => Parameters[0]);
                    _a = new Lazy<double>(() => Parameters[1]);
                    _b = new Lazy<double>(() => Parameters[2]);
                    _c = new Lazy<double>(() => Parameters[3]);
                    _d = new Lazy<double>(() => Parameters[4]);
                    _func = x =>
                    {
                        if (x >= _d.Value)
                        {
                            return Math.Pow(_a.Value * x + _b.Value, _g.Value);
                        }
                        return _c.Value * x;
                    };
                    break;

                case 4:
                    _g = new Lazy<double>(() => Parameters[0]);
                    _a = new Lazy<double>(() => Parameters[1]);
                    _b = new Lazy<double>(() => Parameters[2]);
                    _c = new Lazy<double>(() => Parameters[3]);
                    _d = new Lazy<double>(() => Parameters[4]);
                    _e = new Lazy<double>(() => Parameters[5]);
                    _f = new Lazy<double>(() => Parameters[6]);
                    _func = x =>
                    {
                        if (x >= _d.Value)
                        {
                            return Math.Pow(_a.Value * x + _b.Value, _g.Value) + _e.Value;
                        }
                        return _c.Value * x + _f.Value;
                    };
                    break;

                default:
                    throw new InvalidOperationException($"Unknown Parametric Curve function type '{FunctionType}'. Allowed values are 1, 2, 3, 4.");
            }
        }

        private static int GetParametersCount(int functionType)
        {
            // Table 68 — parametricCurveType function type encoding
            switch (functionType)
            {
                case 0:
                    return 1;

                case 1:
                case 2:
                case 3:
                    return functionType + 2;

                case 4:
                    return 7;

                default:
                    throw new InvalidOperationException($"{functionType}");
            }
        }

        public override double Process(double values)
        {
            return _func(values);
        }

        public override string ToString()
        {
            string[] names = new string[] { "g", "a", "b", "c", "d", "e", "f" };
            string str = "(";

            int i = 0;
            for (i = 0; i < Parameters.Length - 1; i++)
            {
                str += $"{names[i]}={Math.Round(Parameters[i], 4)},";
            }

            str += $"{names[i]}={Math.Round(Parameters[i], 4)}";

            return str + ")";
        }
    }
}
