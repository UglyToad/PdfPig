using System;
using System.Linq;

namespace IccProfileNet.Tags
{
    /// <summary>
    /// TODO
    /// </summary>
    internal sealed class IccViewingConditionsType : IccTagTypeBase
    {
        public const int IlluminantOffset = 8;
        public const int IlluminantLength = 12;

        public const int SurroundOffset = 20;
        public const int SurroundLength = 12;

        public const int IlluminantTypeOffset = 32;
        public const int IlluminantTypeLength = 4;

        private readonly Lazy<IccXyz> illuminant;
        /// <summary>
        /// Un-normalized CIEXYZ values for illuminant (in which Y is in cd/m2).
        /// </summary>
        public IccXyz Illuminant => illuminant.Value;

        private readonly Lazy<IccXyz> surround;
        /// <summary>
        /// Un-normalized CIEXYZ values for surround (in which Y is in cd/m2).
        /// </summary>
        public IccXyz Surround => surround.Value;

        private readonly Lazy<byte[]> illuminantType;
        /// <summary>
        /// Illuminant type.
        /// </summary>
        public byte[] IlluminantType => illuminantType.Value;

        public IccViewingConditionsType(byte[] rawData)
        {
            string typeSignature = IccHelper.GetString(rawData, TypeSignatureOffset, TypeSignatureLength);

            if (typeSignature != IccTags.ViewingConditionsTag)
            {
                throw new ArgumentException(nameof(typeSignature));
            }

            RawData = rawData;

            illuminant = new Lazy<IccXyz>(() =>
            {
                // Un-normalized CIEXYZ values for illuminant (in which Y is in cd/m2)
                // 8 to 19
                return IccHelper.ReadXyz(RawData
                    .Skip(IlluminantOffset)
                    .Take(IlluminantLength)
                    .ToArray());
            });

            surround = new Lazy<IccXyz>(() =>
            {
                // Un-normalized CIEXYZ values for surround (in which Y is in cd/m2)
                // 20 to 31
                return IccHelper.ReadXyz(RawData
                    .Skip(SurroundOffset)
                    .Take(SurroundLength)
                    .ToArray());
            });

            illuminantType = new Lazy<byte[]>(() =>
            {
                // Illuminant type
                // 32 to 35
                // As described in measurementType
                return RawData
                    .Skip(IlluminantTypeOffset)
                    .Take(IlluminantTypeLength)
                    .ToArray(); // TODO
            });
        }
    }
}
