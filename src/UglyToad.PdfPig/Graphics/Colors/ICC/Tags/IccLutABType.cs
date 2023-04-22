using System;
using System.Collections.Generic;
using System.Linq;

namespace IccProfileNet.Tags
{
    /*
     * It is possible to use any or all of these processing elements. At least one processing element shall be included.
     * Only the following combinations are permitted:
     * - B;
     * - M, Matrix, B;
     * - A, CLUT, B;
     * - A, CLUT, M, Matrix, B.
     * Other combinations can be achieved by setting processing element values to identity transforms.
     */
    internal sealed class IccLutABType : IccTagTypeBase, IIccClutType
    {
        public const int NumberOfInputChannelsOffset = 8;
        public const int NumberOfInputChannelsLength = 1;
        public const int NumberOfOutputChannelsOffset = 9;
        public const int NumberOfOutputChannelsLength = 1;
        public const int OffsetFirstBCurveOffset = 12;
        public const int OffsetFirstBCurveLength = 4;
        public const int OffsetMatrixOffset = 16;
        public const int OffsetMatrixLength = 4;
        public const int OffsetFirstMCurveOffset = 20;
        public const int OffsetFirstMCurveLength = 4;
        public const int OffsetClutOffset = 24;
        public const int OffsetClutLength = 4;
        public const int OffsetFirstACurveOffset = 28;
        public const int OffsetFirstACurveLength = 4;
        public const int NumberOfClutGridPointsOffset = 0;
        public const int NumberOfClutGridPointsLength = 16;
        public const int ClutPrecisionOffset = 16;
        public const int ClutPrecisionLength = 1;
        public const int ClutDataPointsOffset = 20;

        /// <summary>
        /// A to B or B to A
        /// </summary>
        public LutABType Type { get; }

        private readonly Lazy<byte> _numberOfInputChannels;
        /// <summary>
        /// Number of Input Channels (i).
        /// </summary>
        public int NumberOfInputChannels => _numberOfInputChannels.Value;

        private readonly Lazy<byte> _numberOfOutputChannels;
        /// <summary>
        /// Number of Output Channels (o).
        /// </summary>
        public int NumberOfOutputChannels => _numberOfOutputChannels.Value;

        private readonly Lazy<int> _offsetFirstBCurve;
        /// <summary>
        /// Offset to first “B” curve.
        /// </summary>
        public int OffsetFirstBCurve => _offsetFirstBCurve.Value;

        private readonly Lazy<IccBaseCurveType[]> _bCurves;
        public IccBaseCurveType[] BCurves => _bCurves.Value;

        private readonly Lazy<int> _offsetMatrix;
        public int OffsetMatrix => _offsetMatrix.Value;

        private readonly Lazy<double[]> _matrix;

        /// <summary>
        /// [e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12]
        /// </summary>
        public double[] Matrix => _matrix.Value;

        /// <summary>
        /// <c>true</c> if Matrix is defined.
        /// </summary>
        public bool HasMatrix => OffsetMatrix != 0;

        private readonly Lazy<int> _offsetFirstMCurve;
        /// <summary>
        /// Offset to first “M” curve.
        /// </summary>
        public int OffsetFirstMCurve => _offsetFirstMCurve.Value;

        private readonly Lazy<IccBaseCurveType[]> _mCurves;
        public IccBaseCurveType[] MCurves => _mCurves.Value;

        /// <summary>
        /// <c>true</c> if M curves are defined.
        /// </summary>
        public bool HasMCurves => OffsetFirstMCurve != 0;

        private readonly Lazy<byte[]> _clutGridPoints;
        public byte[] ClutGridPoints => _clutGridPoints.Value;

        private readonly Lazy<int> _offsetClut;
        public int OffsetClut => _offsetClut.Value;

        private readonly Lazy<double[][]> _clut;

        /// <summary>
        /// Multi-dimensional colour lookup table.
        /// </summary>
        public double[][] Clut => _clut.Value;

        /// <summary>
        /// <c>true</c> if CLUT is defined.
        /// </summary>
        public bool HasClut => OffsetClut != 0;

        private readonly Lazy<int> _offsetFirstACurve;
        /// <summary>
        /// Offset to first “A” curve.
        /// </summary>
        public int OffsetFirstACurve => _offsetFirstACurve.Value;

        private readonly Lazy<IccBaseCurveType[]> _aCurves;
        public IccBaseCurveType[] ACurves => _aCurves.Value;

        /// <summary>
        /// <c>true</c> if A curves are defined.
        /// </summary>
        public bool HasACurve => OffsetFirstACurve != 0;

        private readonly Lazy<double[]> _lookupWeights;

        public IccLutABType(byte[] data)
        {
            string typeSignature = IccHelper.GetString(data, TypeSignatureOffset, TypeSignatureLength);

            if (typeSignature != "mAB " && typeSignature != "mBA ")
            {
                throw new ArgumentException(nameof(typeSignature));
            }

            Type = typeSignature == "mAB " ? LutABType.AB : LutABType.BA;
            RawData = data;

            _numberOfInputChannels = new Lazy<byte>(() =>
            {
                return RawData
                .Skip(NumberOfInputChannelsOffset)
                .Take(NumberOfInputChannelsLength)
                .Single();
            });

            _numberOfOutputChannels = new Lazy<byte>(() =>
            {
                return RawData
                .Skip(NumberOfOutputChannelsOffset)
                .Take(NumberOfOutputChannelsLength)
                .Single();
            });

            _offsetFirstBCurve = new Lazy<int>(() =>
            {
                // Offset to first “B” curve
                // 12 to 15
                return (int)IccHelper.ReadUInt32(RawData
                    .Skip(OffsetFirstBCurveOffset)
                    .Take(OffsetFirstBCurveLength)
                    .ToArray());
            });

            _offsetMatrix = new Lazy<int>(() =>
            {
                // Offset to matrix
                // 16 to 19
                return (int)IccHelper.ReadUInt32(RawData
                    .Skip(OffsetMatrixOffset)
                    .Take(OffsetMatrixLength)
                    .ToArray());
            });

            _offsetFirstMCurve = new Lazy<int>(() =>
            {
                // Offset to first “M” curve
                // 20 to 23
                return (int)IccHelper.ReadUInt32(RawData
                    .Skip(OffsetFirstMCurveOffset)
                    .Take(OffsetFirstMCurveLength)
                    .ToArray());
            });

            _offsetClut = new Lazy<int>(() =>
            {
                // Offset to CLUT
                // 24 to 27
                return (int)IccHelper.ReadUInt32(RawData
                    .Skip(OffsetClutOffset)
                    .Take(OffsetClutLength)
                    .ToArray());
            });

            _offsetFirstACurve = new Lazy<int>(() =>
            {
                // Offset to first “A” curve
                // 28 to 31
                return (int)IccHelper.ReadUInt32(RawData
                    .Skip(OffsetFirstACurveOffset)
                    .Take(OffsetFirstACurveLength)
                    .ToArray());
            });

            _aCurves = new Lazy<IccBaseCurveType[]>(() =>
            {
                if (OffsetFirstACurve == 0)
                {
                    return null;
                }

                int offset = 0;
                IccBaseCurveType[] aCurves = new IccBaseCurveType[Type == LutABType.AB ? NumberOfInputChannels : NumberOfOutputChannels];
                for (byte a = 0; a < aCurves.Length; a++)
                {
                    var curve = IccBaseCurveType.Parse(RawData.Skip((int)OffsetFirstACurve + offset).ToArray());
                    aCurves[a] = curve;
                    offset += IccHelper.AdjustOffsetTo4ByteBoundary(curve.BytesRead);
                }
                return aCurves;
            });

            _bCurves = new Lazy<IccBaseCurveType[]>(() =>
            {
                int offset = 0;
                IccBaseCurveType[] bCurves = new IccBaseCurveType[Type == LutABType.AB ? NumberOfOutputChannels : NumberOfInputChannels];
                for (byte b = 0; b < bCurves.Length; b++)
                {
                    var curve = IccBaseCurveType.Parse(RawData.Skip((int)OffsetFirstBCurve + offset).ToArray());
                    bCurves[b] = curve;
                    offset += IccHelper.AdjustOffsetTo4ByteBoundary(curve.BytesRead);
                }
                return bCurves;
            });

            _mCurves = new Lazy<IccBaseCurveType[]>(() =>
            {
                if (OffsetFirstMCurve == 0)
                {
                    return null;
                }

                int offset = 0;
                IccBaseCurveType[] mCurves = new IccBaseCurveType[Type == LutABType.AB ? NumberOfOutputChannels : NumberOfInputChannels];
                for (byte m = 0; m < mCurves.Length; m++)
                {
                    var curve = IccBaseCurveType.Parse(RawData.Skip((int)OffsetFirstMCurve + offset).ToArray());
                    mCurves[m] = curve;
                    offset += IccHelper.AdjustOffsetTo4ByteBoundary(curve.BytesRead);
                }
                return mCurves;
            });

            _matrix = new Lazy<double[]>(() =>
            {
                if (OffsetMatrix == 0)
                {
                    return null;
                }

                double[] matrix = new double[3 * 4];
                byte[] matrixData = RawData.Skip((int)OffsetMatrix).Take(matrix.Length * IccHelper.S15Fixed16Length).ToArray();
                return IccHelper.Reads15Fixed16Array(matrixData);
            });

            _clutGridPoints = new Lazy<byte[]>(() =>
            {
                if (OffsetClut == 0)
                {
                    return null;
                }
                return RawData
                    .Skip(OffsetClut + NumberOfClutGridPointsOffset)
                    .Take(NumberOfInputChannels).ToArray();
            });

            _clut = new Lazy<double[][]>(() =>
            {
                // CLUT
                if (OffsetClut == 0)
                {
                    return null;
                }

                if (NumberOfInputChannels > NumberOfClutGridPointsLength)
                {
                    throw new ArgumentOutOfRangeException(nameof(NumberOfInputChannels));
                }

                byte precision = RawData
                    .Skip(OffsetClut + ClutPrecisionOffset)
                    .Take(ClutPrecisionLength)
                    .Single();

                byte[] clutData = RawData.Skip(OffsetClut + ClutDataPointsOffset).ToArray();

                Func<byte[], double> reader;
                if (precision == 1)
                {
                    reader = new Func<byte[], double>(array => IccHelper.ReadUInt8(array) / (double)byte.MaxValue);
                }
                else
                {
                    reader = new Func<byte[], double>(array => IccHelper.ReadUInt16(array) / (double)ushort.MaxValue);
                }

                int l = 0;
                int clutSize = 1;
                foreach (byte gridPoint in ClutGridPoints)
                {
                    clutSize *= gridPoint;
                }

                double[][] clut = new double[clutSize][];
                for (int i = 0; i < clut.Length; i++)
                {
                    double[] oArray = new double[NumberOfOutputChannels];
                    for (int o = 0; o < oArray.Length; o++)
                    {
                        oArray[o] = reader(clutData.Skip(l).Take(precision).ToArray());
                        l += precision;
                    }

                    clut[i] = oArray;
                }
                return clut;
            });

            _lookupWeights = new Lazy<double[]>(() =>
            {
                double[] weights = null;
                if (ClutGridPoints != null)
                {
                    // Pre compute Lookup weigth
                    // Can be optimised by setting _lookupWeigths = ClutGridPoints;
                    weights = new double[ClutGridPoints.Length];

                    for (int i = 0; i < weights.Length; i++)
                    {
                        double w = 1 * (ClutGridPoints[i]);
                        for (int j = i + 1; j < weights.Length; j++)
                        {
                            w *= ClutGridPoints[j];
                        }
                        weights[i] = w;
                    }
                }
                return weights;
            });

            _processorPipeline = new Lazy<Func<double[], IccProfileHeader, double[]>[]>(BuildProcessorPipeline);
        }

        public double[] ApplyMatrix(double[] values, IccProfileHeader header)
        {
            if (!HasMatrix)
            {
                throw new InvalidOperationException("No matrix.");
            }

            if (Matrix is null)
            {
                throw new InvalidOperationException("Missing Matrix.");
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
                x1 * Matrix[0] + x2 * Matrix[1] + x3 * Matrix[2] + Matrix[9],
                x1 * Matrix[3] + x2 * Matrix[4] + x3 * Matrix[5] + Matrix[10],
                x1 * Matrix[6] + x2 * Matrix[7] + x3 * Matrix[8] + Matrix[11]
            };
        }

        public double[] LookupClut(double[] input)
        {
            if (Clut == null || ClutGridPoints == null || _lookupWeights == null || _lookupWeights.Value == null)
            {
                throw new ArgumentNullException("No Clut.");
            }

            // https://stackoverflow.com/questions/35109195/how-do-the-the-different-parts-of-an-icc-file-work-together
            if (input.Length != ClutGridPoints.Length)
            {
                throw new ArgumentException("TODO");
            }

            // TODO - Need interpolation
            double index = 0;
            for (int i = 0; i < input.Length; i++)
            {
                index += input[i] * _lookupWeights.Value[i];
            }

            return Clut[(int)index];
        }

        private static double[] ProcessCurves(double[] input, IccBaseCurveType[] curves)
        {
            if (curves is null)
            {
                throw new InvalidOperationException("Missing curves.");
            }

            if (input.Length != curves.Length)
            {
                throw new InvalidOperationException("TODO");
            }

            // process
            double[] result = new double[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                result[i] = curves[i].Process(input[i]);
            }

            return result;
        }

        private readonly Lazy<Func<double[], IccProfileHeader, double[]>[]> _processorPipeline;

        public double[] Process(double[] values, IccProfileHeader header)
        {
            foreach (var func in _processorPipeline.Value)
            {
                values = func(values, header);
            }
            return values;

            //switch (Type)
            //{
            //    case LutABType.AB:
            //        return ProcessAtoB(values, header);

            //    case LutABType.BA:
            //        return ProcessBtoA(values, header);
            //}

            //throw new ArgumentOutOfRangeException("Invalid type.");
        }

        #region Build Processor Pipeline
        public Func<double[], IccProfileHeader, double[]>[] BuildProcessorPipeline()
        {
            switch (Type)
            {
                case LutABType.AB:
                    return BuildProcessorPipelineAtoB();

                case LutABType.BA:
                    return BuildProcessorPipelineBtoA();
            }

            throw new ArgumentOutOfRangeException("Invalid type.");
        }

        private Func<double[], IccProfileHeader, double[]>[] BuildProcessorPipelineAtoB()
        {
            var pipeline = new List<Func<double[], IccProfileHeader, double[]>>();

            if (HasACurve)
            {
                pipeline.Add((x, h) => ProcessCurves(x, ACurves));
            }

            if (HasClut)
            {
                pipeline.Add((x, h) => LookupClut(x));
            }

            if (HasMCurves)
            {
                pipeline.Add((x, h) => ProcessCurves(x, MCurves));
            }

            if (HasMatrix)
            {
                pipeline.Add(ApplyMatrix);
            }

            // BCurves
            pipeline.Add((x, h) => ProcessCurves(x, BCurves));
            return pipeline.ToArray();
        }

        private Func<double[], IccProfileHeader, double[]>[] BuildProcessorPipelineBtoA()
        {
            var pipeline = new List<Func<double[], IccProfileHeader, double[]>>
            {
                (x, h) => ProcessCurves(x, BCurves)
            };

            if (HasMatrix)
            {
                pipeline.Add(ApplyMatrix);
            }

            if (HasClut)
            {
                pipeline.Add((x, h) => LookupClut(x));
            }

            if (HasMCurves)
            {
                pipeline.Add((x, h) => ProcessCurves(x, MCurves));
            }

            if (HasACurve)
            {
                pipeline.Add((x, h) => ProcessCurves(x, ACurves));
            }

            return pipeline.ToArray();
        }
        #endregion

        public IccRenderingIntent GetRenderingIntent(IccProfileClass profileClass, string tag)
        {
            if (profileClass == IccProfileClass.Abstract ||
                profileClass ==  IccProfileClass.DeviceLink ||
                profileClass == IccProfileClass.NamedColor)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (Type == LutABType.AB && tag != IccTags.AToB0Tag &&
                tag != IccTags.AToB1Tag && tag != IccTags.AToB2Tag)
            {
                throw new ArgumentOutOfRangeException();
            }
            else if (Type == LutABType.AB && tag != IccTags.BToA0Tag &&
                tag != IccTags.BToA1Tag && tag != IccTags.BToA2Tag)
            {
                throw new ArgumentOutOfRangeException();
            }

            switch (tag)
            {
                case IccTags.AToB0Tag:
                case IccTags.BToA0Tag:
                    return IccRenderingIntent.Perceptual;

                case IccTags.AToB1Tag:
                case IccTags.BToA1Tag:
                    return IccRenderingIntent.MediaRelativeColorimetric; // TODO check

                case IccTags.AToB2Tag:
                case IccTags.BToA2Tag:
                    return IccRenderingIntent.Saturation;
            }

            throw new NotImplementedException();
        }

        public static string GetProfileTag(IccProfileClass profileClass, IccRenderingIntent renderingIntent, LutABType lutABType)
        {

            // See table 25
            switch (renderingIntent)
            {
                case IccRenderingIntent.Perceptual:
                    switch (profileClass)
                    {
                        case IccProfileClass.Input:
                        case IccProfileClass.Display:
                        case IccProfileClass.Output:
                        case IccProfileClass.ColorSpace:
                            return lutABType == LutABType.AB ? IccTags.AToB0Tag : IccTags.BToA0Tag;
                    }
                    break;

                case IccRenderingIntent.IccAbsoluteColorimetric:  // TODO check
                case IccRenderingIntent.MediaRelativeColorimetric:
                    switch (profileClass)
                    {
                        case IccProfileClass.Input:
                        case IccProfileClass.Display:
                        case IccProfileClass.Output:
                        case IccProfileClass.ColorSpace:
                            return lutABType == LutABType.AB ? IccTags.AToB1Tag : IccTags.BToA1Tag;
                    }
                    break;

                case IccRenderingIntent.Saturation:
                    switch (profileClass)
                    {
                        case IccProfileClass.Input:
                        case IccProfileClass.Display:
                        case IccProfileClass.Output:
                        case IccProfileClass.ColorSpace:
                            return lutABType == LutABType.AB ? IccTags.AToB2Tag : IccTags.BToA2Tag;
                    }
                    break;
            }
            throw new NotImplementedException();
        }

        public enum LutABType : byte
        {
            AB = 0,
            BA = 1
        }
    }
}
