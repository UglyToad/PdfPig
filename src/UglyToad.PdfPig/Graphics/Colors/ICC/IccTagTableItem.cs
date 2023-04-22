namespace IccProfileNet
{
    internal readonly struct IccTagTableItem
    {
        #region ICC Profile tags constants
        public const int TagCountOffset = 0;
        public const int TagCountLength = 4;
        public const int TagSignatureOffset = 4;
        public const int TagSignatureLength = 4;
        public const int TagOffsetOffset = 8;
        public const int TagOffsetLength = 4;
        public const int TagSizeOffset = 12;
        public const int TagSizeLength = 4;
        #endregion

        /// <summary>
        /// Tag Signature.
        /// </summary>
        public string Signature { get; }

        /// <summary>
        /// Offset to beginning of tag data element.
        /// </summary>
        public uint Offset { get; }

        /// <summary>
        /// Size of tag data element.
        /// </summary>
        public uint Size { get; }

        /// <summary>
        /// TODO
        /// </summary>
        public IccTagTableItem(string signature, uint offset, uint size)
        {
            Signature = signature;
            Offset = offset;
            Size = size;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Signature}: offset={Offset}, size={Size}";
        }
    }
}
