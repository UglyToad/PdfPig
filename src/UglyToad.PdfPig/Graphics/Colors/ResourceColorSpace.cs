namespace UglyToad.PdfPig.Graphics.Colors
{
    using Tokens;

    /// <summary>
    /// A color space definition from a resource dictionary.
    /// </summary>
    internal readonly struct ResourceColorSpace
    {
        public NameToken Name { get; }

        public IToken Data { get; }

        public ResourceColorSpace(NameToken name, IToken data)
        {
            Name = name;
            Data = data;
        }

        public ResourceColorSpace(NameToken name) : this(name, null) { }
    }
}
