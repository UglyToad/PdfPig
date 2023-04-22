using System;
using System.Linq;

namespace IccProfileNet.Tags
{
    internal sealed class IccSignatureType : IccTagTypeBase
    {
        public const int SignatureOffset = 8;
        public const int SignatureLength = 4;

        private readonly Lazy<string> _signature;
        /// <summary>
        /// TODO
        /// </summary>
        public string Signature => _signature.Value;

        public IccSignatureType(byte[] rawData)
        {
            // TODO - check signature

            RawData = rawData;

            _signature = new Lazy<string>(() =>
            {
                // Encoded value for standard observer
                // 8 to 11
                byte[] signatureBytes = RawData
                    .Skip(SignatureOffset)
                    .Take(SignatureLength)
                    .ToArray();

                return IccHelper.GetString(signatureBytes);
            });
        }
    }
}
