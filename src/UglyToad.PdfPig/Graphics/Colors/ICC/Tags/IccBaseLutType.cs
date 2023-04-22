using System;
using System.Linq;

namespace IccProfileNet.Tags
{
    internal abstract class IccBaseLutType : IccTagTypeBase, IIccClutType
    {
        public const int NumberOfInputChannelsOffset = 8;
        public const int NumberOfInputChannelsLength = 1;
        public const int NumberOfOutputChannelsOffset = 9;
        public const int NumberOfOutputChannelsLength = 1;
        public const int NumberOfClutPointOffset = 10;
        public const int NumberOfClutPointLength = 1;
        public const int E1Offset = 12;
        public const int E1Length = 4;
        public const int E2Offset = 16;
        public const int E2Length = 4;
        public const int E3Offset = 20;
        public const int E3Length = 4;
        public const int E4Offset = 24;
        public const int E4Length = 4;
        public const int E5Offset = 28;
        public const int E5Length = 4;
        public const int E6Offset = 32;
        public const int E6Length = 4;
        public const int E7Offset = 36;
        public const int E7Length = 4;
        public const int E8Offset = 40;
        public const int E8Length = 4;
        public const int E9Offset = 44;
        public const int E9Length = 4;

        protected Lazy<int> _numberOfInputChannels;
        public int NumberOfInputChannels => _numberOfInputChannels.Value;

        protected Lazy<int> _numberOfInputEntries;
        public int NumberOfInputEntries => _numberOfInputEntries.Value;

        protected Lazy<int> _numberOfOutputChannels;
        public int NumberOfOutputChannels => _numberOfOutputChannels.Value;

        protected Lazy<int> _numberOfOutputEntries;
        public int NumberOfOutputEntries => _numberOfOutputEntries.Value;

        protected Lazy<int> _numberOfClutPoints;
        public int NumberOfClutPoints => _numberOfClutPoints.Value;

        protected Lazy<double> _e1;
        public double E1 => _e1.Value;

        protected Lazy<double> _e2;
        public double E2 => _e2.Value;

        protected Lazy<double> _e3;
        public double E3 => _e3.Value;

        protected Lazy<double> _e4;
        public double E4 => _e4.Value;

        protected Lazy<double> _e5;
        public double E5 => _e5.Value;

        protected Lazy<double> _e6;
        public double E6 => _e6.Value;

        protected Lazy<double> _e7;
        public double E7 => _e7.Value;

        protected Lazy<double> _e8;
        public double E8 => _e8.Value;

        protected Lazy<double> _e9;
        public double E9 => _e9.Value;

        protected Lazy<double[][]> _inputTable;
        public double[][] InputTable => _inputTable.Value;

        protected Lazy<double[][]> _clut;
        public double[][] Clut => _clut.Value;

        protected Lazy<double[][]> _outputTable;
        public double[][] OutputTable => _outputTable.Value;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable RCS1160 // Abstract type should not have public constructors.
        public IccBaseLutType(byte[] rawData)
#pragma warning restore RCS1160 // Abstract type should not have public constructors.
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            // Signature check is done in child class

            RawData = rawData;

            _numberOfInputChannels = new Lazy<int>(() =>
            {
                // Number of Input Channels (i)
                // 8
                return RawData.Skip(NumberOfInputChannelsOffset).Take(NumberOfInputChannelsLength).ToArray()[0];
            });

            _numberOfOutputChannels = new Lazy<int>(() =>
            {
                // Number of Output Channels (o)
                // 9
                return RawData.Skip(NumberOfOutputChannelsOffset).Take(NumberOfOutputChannelsLength).ToArray()[0];
            });

            _numberOfClutPoints = new Lazy<int>(() =>
            {
                // Number of CLUT grid points (identical for each side) (g)
                // 10
                return RawData.Skip(NumberOfClutPointOffset).Take(NumberOfClutPointLength).ToArray()[0];
            });

            _e1 = new Lazy<double>(() =>
            {
                // Encoded e1 parameter
                // 12 to 15
                return IccHelper.Reads15Fixed16Number(RawData.Skip(E1Offset).Take(E1Length).ToArray());
            });

            _e2 = new Lazy<double>(() =>
            {
                // Encoded e2 parameter
                // 16 to 19
                return IccHelper.Reads15Fixed16Number(RawData.Skip(E2Offset).Take(E2Length).ToArray());
            });

            _e3 = new Lazy<double>(() =>
            {
                // Encoded e3 parameter
                // 20 to 23
                return IccHelper.Reads15Fixed16Number(RawData.Skip(E3Offset).Take(E3Length).ToArray());
            });

            _e4 = new Lazy<double>(() =>
            {
                // Encoded e4 parameter
                // 24 to 27
                return IccHelper.Reads15Fixed16Number(RawData.Skip(E4Offset).Take(E4Length).ToArray());
            });

            _e5 = new Lazy<double>(() =>
            {
                // Encoded e5 parameter
                // 28 to 31
                return IccHelper.Reads15Fixed16Number(RawData.Skip(E5Offset).Take(E5Length).ToArray());
            });

            _e6 = new Lazy<double>(() =>
            {
                // Encoded e6 parameter
                // 32 to 35
                return IccHelper.Reads15Fixed16Number(RawData.Skip(E6Offset).Take(E6Length).ToArray());
            });

            _e7 = new Lazy<double>(() =>
            {
                // Encoded e7 parameter
                // 36 to 39
                return IccHelper.Reads15Fixed16Number(RawData.Skip(E7Offset).Take(E7Length).ToArray());
            });

            _e8 = new Lazy<double>(() =>
            {
                // Encoded e8 parameter
                // 40 to 43
                return IccHelper.Reads15Fixed16Number(RawData.Skip(E8Offset).Take(E8Length).ToArray());
            });

            _e9 = new Lazy<double>(() =>
            {
                // Encoded e9 parameter
                // 44 to 47
                return IccHelper.Reads15Fixed16Number(RawData.Skip(E9Offset).Take(E9Length).ToArray());
            });
        }

        public virtual double[] LookupClut(double[] input)
        {
            return IccHelper.Lookup(input, Clut, NumberOfClutPoints);
        }

        public double[] ApplyMatrix(double[] values, IccProfileHeader header)
        {
            // p217
            // In LUT8 and LUT16, the matrix element can only be used when PCS is XYZ.
            if (header.Pcs == IccProfileConnectionSpace.PCSLAB)
            {
                return values;
            }

            if (values.Length != 3)
            {
                throw new InvalidOperationException("Input values dimensions not correct.");
            }

            double x1 = values[0];
            double x2 = values[1];
            double x3 = values[2];

            // Check if identity, then do nothing

            return new double[]
            {
                x1 * E1 + x2 * E2 + x3 * E3,
                x1 * E4 + x2 * E5 + x3 * E6,
                x1 * E7 + x2 * E8 + x3 * E9
            };
        }

        private static double[] ProcessCurves(double[] values, double[][] table)
        {
            if (values.Length != table.Length)
            {
                throw new InvalidOperationException("TODO");
            }

            double[] result = new double[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = Interpolate(values[i], table[i]);
            }

            return result;
        }

        private static double Interpolate(double x, double[] values)
        {
            // Interpolate
            double index = (values.Length - 1.0) * x;

            bool hasDecimal = Math.Abs(index % 1) > double.Epsilon * 100;
            if (!hasDecimal)
            {
                return values[(int)index];
            }

            int indexInt = (int)Math.Floor(index);
            double w = index - indexInt;
            double y1 = values[indexInt];
            double y2 = values[indexInt + 1];
            return y1 + w * (y2 - y1);
        }

        public double[] Process(double[] values, IccProfileHeader header)
        {
            // p217
            // LUT8 and LUT16 have the following processing elements: matrix – one-dimensional
            // curves – CLUT – one-dimensional curves.
            double[] result = ApplyMatrix(values, header);
            result = ProcessCurves(result, InputTable);
            result = LookupClut(result);
            return ProcessCurves(result, OutputTable);
        }

        public static IccBaseLutType Parse(byte[] bytes)
        {
            string typeSignature = IccHelper.GetString(bytes, IccTagTypeBase.TypeSignatureOffset, IccTagTypeBase.TypeSignatureLength);

            if (typeSignature != "mft1" && typeSignature != "mft2")
            {
                throw new ArgumentException(nameof(typeSignature));
            }

            switch (typeSignature)
            {
                case "mft1":
                    return new IccLut8Type(bytes);
                case "mft2":
                    return new IccLut16Type(bytes);
                default:
                    throw new ArgumentException(nameof(typeSignature));
            }
        }
    }
}
