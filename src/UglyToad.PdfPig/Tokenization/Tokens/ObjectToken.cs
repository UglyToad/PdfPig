namespace UglyToad.PdfPig.Tokenization.Tokens
{
    using ContentStream;

    internal class ObjectToken : IDataToken<IToken>
    {
        /// <summary>
        /// The offset of the start of the object number in the file bytes.
        /// </summary>
        public long Position { get; set; }

        /// <summary>
        /// The object and generation number of the object.
        /// </summary>
        public IndirectReference Number { get; }

        /// <summary>
        /// The inner data of the object.
        /// </summary>
        public IToken Data { get; }

        public ObjectToken(long position, IndirectReference number, IToken data)
        {
            Position = position;
            Number = number;
            Data = data;
        }
    }
}
