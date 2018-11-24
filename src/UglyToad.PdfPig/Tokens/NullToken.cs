namespace UglyToad.PdfPig.Tokens
{
    /// <summary>
    /// The null object has a type and value that are unequal to those of any other object.
    /// There is only one object of type null, denoted by the keyword null.
    /// An indirect object reference to a nonexistent object is treated the same as the null object. 
    /// Specifying the null object as the value of a dictionary entry is equivalent to omitting the entry entirely.
    /// </summary>
    public class NullToken : IDataToken<object>
    {
        /// <summary>
        /// The single instance of the <see cref="NullToken"/>.
        /// </summary>
        public static NullToken Instance { get; } = new NullToken();

        /// <summary>
        /// <see langword="null"/>.
        /// </summary>
        public object Data { get; } = null;

        private NullToken() { }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is NullToken;
        }

        /// <summary>
        /// Whether two null tokens are equal.
        /// </summary>
        protected bool Equals(NullToken other)
        {
            return Equals(Data, other.Data);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "null";
        }
    }
}
