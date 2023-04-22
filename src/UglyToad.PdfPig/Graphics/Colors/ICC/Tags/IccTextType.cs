using System;
using System.Linq;

namespace IccProfileNet.Tags
{
    /// <summary>
    /// TODO
    /// </summary>
    internal sealed class IccTextType : IccTagTypeBase
    {
        public const int TextOffset = 8;

        private readonly Lazy<string> _text;
        /// <summary>
        /// TODO
        /// </summary>
        public string Text => _text.Value;

        public IccTextType(byte[] rawData)
        {
            string typeSignature = IccHelper.GetString(rawData, TypeSignatureOffset, TypeSignatureLength);

            if (typeSignature != "text" && typeSignature != IccTags.ProfileDescriptionTag)
            {
                throw new ArgumentException(nameof(typeSignature));
            }

            RawData = rawData;

            _text = new Lazy<string>(() => IccHelper.GetString(RawData.Skip(TextOffset).ToArray()));
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
