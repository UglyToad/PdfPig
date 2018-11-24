namespace UglyToad.PdfPig.Tokens
{
    using System;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// A name object is an atomic symbol uniquely defined by a sequence of characters.
    /// Each name is considered identical if it has the same sequence of characters. Names are used in
    /// PDF documents to identify dictionary keys and other elements of a PDF document.
    /// </summary>
    public partial class NameToken : IDataToken<string>
    {
        /// <summary>
        /// The string representation of the name.
        /// </summary>
        [NotNull]
        public string Data { get; }
        
        private NameToken(string text)
        {
            NameMap[text] = this;

            Data = text;
        }

        /// <summary>
        /// Creates a new <see cref="NameToken"/> with the given name, ensuring only one instance of each
        /// <see cref="NameToken"/> can exist.
        /// </summary>
        /// <param name="name">The string representation of the name for the token to create.</param>
        /// <returns>The created or existing token.</returns>
        public static NameToken Create([NotNull] string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!NameMap.TryGetValue(name, out var value))
            {
                return new NameToken(name);
            }

            return value;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as NameToken);
        }

        /// <summary>
        /// Are these names identical?
        /// </summary>
        public bool Equals(NameToken other)
        {
            return string.Equals(Data, other?.Data);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }

        /// <summary>
        /// Convert the name token to a string implicitly.
        /// </summary>
        /// <param name="name">The name token to convert.</param>
        public static implicit operator string(NameToken name)
        {
            return name?.Data;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"/{Data}";
        }
    }
}