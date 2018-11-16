namespace UglyToad.PdfPig.Tokens
{
    internal class NullToken : IDataToken<object>
    {
        public static NullToken Instance { get; } = new NullToken();

        public object Data { get; } = null;

        private NullToken() { }
    }
}
