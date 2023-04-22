using System;
using System.Linq;

namespace IccProfileNet.Tags
{
    internal sealed class IccLut16Type : IccBaseLutType
    {
        public const int NumberOfInputEntriesOffset = 48;
        public const int NumberOfInputEntriesLength = 2;
        public const int NumberOfOutputEntriesOffset = 50;
        public const int NumberOfOutputEntriesLength = 2;
        public const int InputTablesOffset = 52;

        public IccLut16Type(byte[] rawData) : base(rawData)
        {
            string typeSignature = IccHelper.GetString(rawData, TypeSignatureOffset, TypeSignatureLength);

            if (typeSignature != "mft2")
            {
                throw new ArgumentException(nameof(typeSignature));
            }

            _numberOfInputEntries = new Lazy<int>(() =>
            {
                // Number of input table entries (n)
                // 48 to 49
                return IccHelper.ReadUInt16(RawData
                    .Skip(NumberOfInputEntriesOffset)
                    .Take(NumberOfInputEntriesLength)
                    .ToArray());
            });

            _numberOfOutputEntries = new Lazy<int>(() =>
            {
                // Number of output table entries (m)
                // 50 to 51
                return IccHelper.ReadUInt16(RawData
                    .Skip(NumberOfOutputEntriesOffset)
                    .Take(NumberOfOutputEntriesLength)
                    .ToArray());
            });

            _inputTable = new Lazy<double[][]>(() =>
            {
                // Input tables
                // 52 to 51+(2ni)
                int l = 0;
                int inputTableBytesL = IccHelper.UInt16Length * NumberOfInputEntries * NumberOfInputChannels;
                double[][] inputTable = new double[NumberOfInputChannels][];
                var tableBytes = RawData.Skip(InputTablesOffset).Take(inputTableBytesL).ToArray();
                for (int i = 0; i < inputTable.Length; i++)
                {
                    double[] domain = new double[NumberOfInputEntries];
                    for (int j = 0; j < domain.Length; j++)
                    {
                        domain[j] = IccHelper.ReadUInt16(tableBytes.Skip(l).Take(IccHelper.UInt16Length).ToArray()) / (double)ushort.MaxValue;
                        l += IccHelper.UInt16Length;
                    }
                    inputTable[i] = domain;
                }
                return inputTable;
            });

            _clut = new Lazy<double[][]>(() =>
            {
                // CLUT values
                int l = 0;
                int clutValuesBytesL = IccHelper.UInt16Length * (int)Math.Pow(NumberOfClutPoints, NumberOfInputChannels) * NumberOfOutputChannels;
                int inputTableBytesL = IccHelper.UInt16Length * NumberOfInputEntries * NumberOfInputChannels;
                var tableBytes = RawData.Skip(InputTablesOffset + inputTableBytesL).Take(clutValuesBytesL).ToArray();

                int clutSize = (int)Math.Pow(NumberOfClutPoints, NumberOfInputChannels);
                double[][] clut = new double[clutSize][];
                for (int i = 0; i < clut.Length; i++)
                {
                    double[] oArray = new double[NumberOfOutputChannels];
                    for (int o = 0; o < oArray.Length; o++)
                    {
                        oArray[o] = IccHelper.ReadUInt16(tableBytes.Skip(l).Take(IccHelper.UInt16Length).ToArray()) / (double)ushort.MaxValue;
                        l += IccHelper.UInt16Length;
                    }

                    clut[i] = oArray;
                }
                return clut;
            });

            _outputTable = new Lazy<double[][]>(() =>
            {
                // Output tables
                int l = 0;
                //int outputBytesL = IccHelper.UInt16Length * outputTableEntries * output;
                double[][] outputTable = new double[NumberOfOutputChannels][];
                int clutValuesBytesL = IccHelper.UInt16Length * (int)Math.Pow(NumberOfClutPoints, NumberOfInputChannels) * NumberOfOutputChannels;
                int inputTableBytesL = IccHelper.UInt16Length * NumberOfInputEntries * NumberOfInputChannels;
                var tableBytes = RawData.Skip(InputTablesOffset + inputTableBytesL + clutValuesBytesL).ToArray();
                for (int i = 0; i < outputTable.Length; i++)
                {
                    double[] domain = new double[NumberOfOutputEntries];
                    for (int j = 0; j < domain.Length; j++)
                    {
                        domain[j] = IccHelper.ReadUInt16(tableBytes.Skip(l).Take(IccHelper.UInt16Length).ToArray()) / (double)ushort.MaxValue;
                        l += IccHelper.UInt16Length;
                    }
                    outputTable[i] = domain;
                }
                return outputTable;
            });
        }
    }
}
