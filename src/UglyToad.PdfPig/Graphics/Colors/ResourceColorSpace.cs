namespace UglyToad.PdfPig.Graphics.Colors
{
    using Tokens;

    /// <summary>
    /// A color space definition from a resource dictionary.
    /// </summary>
    public readonly struct ResourceColorSpace
    {
        /// <summary>
        /// The color space name.
        /// </summary>
        public NameToken Name { get; }

        /// <summary>
        /// The color space data.
        /// </summary>
        public IToken Data { get; }

        internal ResourceColorSpace(NameToken name, IToken data)
        {
            Name = name;
            Data = data;
        }

        internal ResourceColorSpace(NameToken name) : this(name, null) { }
    }
}
