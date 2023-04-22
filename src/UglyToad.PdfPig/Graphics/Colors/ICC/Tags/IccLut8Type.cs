using System;
using System.Linq;

namespace IccProfileNet.Tags
{
    /// <summary>
    /// TODO
    /// </summary>
    internal sealed class IccLut8Type : IccBaseLutType
    {
        /// <summary>
         /// TODO
         /// </summary>
        public const int InputTablesOffset = 48;

        /// <summary>
        /// TODO
        /// </summary>
        public IccLut8Type(byte[] rawData) : base(rawData)
        {
            string typeSignature = IccHelper.GetString(rawData, TypeSignatureOffset, TypeSignatureLength);

            if (typeSignature != "mft1")
            {
                throw new ArgumentException(nameof(typeSignature));
            }

            RawData = rawData;

            _numberOfInputEntries = new Lazy<int>(() => 256);
            _numberOfOutputEntries = new Lazy<int>(() => 256);

            _inputTable = new Lazy<double[][]>(() =>
            {
                // Input tables
                int inputTableBytesL = NumberOfInputEntries * NumberOfInputChannels;
                double[][] inputTable = new double[NumberOfInputChannels][];
                var tableBytes = RawData.Skip(InputTablesOffset).Take(inputTableBytesL).ToArray();
                for (int i = 0; i < inputTable.Length; i++)
                {
                    inputTable[i] = tableBytes.Skip(i * NumberOfInputEntries).Take(NumberOfInputEntries).Select(v => v / (double)byte.MaxValue).ToArray();
                }
                return inputTable;
            });

            _clut = new Lazy<double[][]>(() =>
            {
                // CLUT values
                int l = 0;
                int clutValuesBytesL = (int)Math.Pow(NumberOfClutPoints, NumberOfInputChannels) * NumberOfOutputChannels;
                int inputTableBytesL = NumberOfInputEntries * NumberOfInputChannels;
                var tableBytes = RawData.Skip(InputTablesOffset + inputTableBytesL).Take(clutValuesBytesL).ToArray();

                int clutSize = (int)Math.Pow(NumberOfClutPoints, NumberOfInputChannels);
                double[][] clut = new double[clutSize][];
                for (int i = 0; i < clut.Length; i++)
                {
                    double[] oArray = new double[NumberOfOutputChannels];
                    for (int o = 0; o < oArray.Length; o++)
                    {
                        oArray[o] = tableBytes.Skip(l).Take(1).Single() / (double)byte.MaxValue;
                        l++;
                    }

                    clut[i] = oArray;
                }

                return clut;
            });

            _outputTable = new Lazy<double[][]>(() =>
            {
                // Output tables
                int outputTableByteL = NumberOfOutputEntries * NumberOfOutputChannels;
                int clutValuesBytesL = (int)Math.Pow(NumberOfClutPoints, NumberOfInputChannels) * NumberOfOutputChannels;
                int inputTableBytesL = NumberOfInputEntries * NumberOfInputChannels;
                double[][] outputTable = new double[NumberOfOutputChannels][];
                var tableBytes = RawData.Skip(InputTablesOffset + inputTableBytesL + clutValuesBytesL).ToArray();
                for (int i = 0; i < outputTable.Length; i++)
                {
                    outputTable[i] = tableBytes.Skip(i * NumberOfOutputEntries).Take(NumberOfOutputEntries).Select(v => v / (double)byte.MaxValue).ToArray();
                }
                return outputTable;
            });
        }
    }
}
