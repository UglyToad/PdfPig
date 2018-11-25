namespace UglyToad.PdfPig.Tokens
{
    using Util.JetBrains.Annotations;

    /// <summary>
    /// The boolean object either <see cref="True"/> (<see langword="true"/>) or <see cref="False"/> (<see langword="true"/>).
    /// </summary>
    public sealed class BooleanToken : IDataToken<bool>
    {
        /// <summary>
        /// The boolean token corresponding to <see langword="true"/>.
        /// </summary>
        [NotNull]
        public static BooleanToken True { get; } = new BooleanToken(true);

        /// <summary>
        /// The boolean token corresponding to <see langword="false"/> 
        /// </summary>
        [NotNull]
        public static BooleanToken False { get; } = new BooleanToken(false);

        /// <summary>
        /// The value true/false of this boolean token.
        /// </summary>
        public bool Data { get; }

        /// <summary>
        /// Create a new <see cref="BooleanToken"/>.
        /// </summary>
        /// <param name="data">The value of the boolean.</param>
        private BooleanToken(bool data)
        {
            Data = data;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (!(obj is BooleanToken other))
            {
                return false;
            }

            return other.Data == Data;
        }
        
        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Data.ToString();
        }
    }
}