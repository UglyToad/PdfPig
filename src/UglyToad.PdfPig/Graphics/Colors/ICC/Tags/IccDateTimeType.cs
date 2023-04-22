using System;
using System.Linq;

namespace IccProfileNet.Tags
{
    internal sealed class IccDateTimeType : IccTagTypeBase
    {
        public const int DateAndTimeOffset = 8;
        public const int DateAndTimeLength = 12;

        private readonly Lazy<DateTime> _dateTime;
        /// <summary>
        /// Value.
        /// </summary>
        public DateTime DateTime => _dateTime.Value;

        public IccDateTimeType(byte[] data)
        {
            string typeSignature = IccHelper.GetString(data, TypeSignatureOffset, TypeSignatureLength);

            if (typeSignature != "dtim")
            {
                throw new ArgumentException(nameof(typeSignature));
            }

            RawData = data;
            _dateTime = new Lazy<DateTime>(() =>
            {
                var dt = IccHelper.ReadDateTime(RawData.Skip(DateAndTimeOffset).Take(DateAndTimeLength).ToArray());
                if (!dt.HasValue)
                {
                    throw new ArgumentNullException(nameof(RawData), "Could not get date value.");
                }

                return dt.Value;
            });
        }
    }
}
