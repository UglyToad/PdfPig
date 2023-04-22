namespace IccProfileNet.Tags
{
    /// <summary>
    /// Interface for ICC tage type.
    /// </summary>
    internal abstract class IccTagTypeBase
    {
        public const int TypeSignatureOffset = 0;
        public const int TypeSignatureLength = 4;
        public const int ReservedOffset = 4;
        public const int ReservedLength = 4;

        /*
        /// <summary>
        /// Tag Signature.
        /// </summary>
        public string Signature { get; }
        */

        /// <summary>
        /// Tag raw data.
        /// </summary>
        public byte[] RawData { get; protected set; }
    }
}
