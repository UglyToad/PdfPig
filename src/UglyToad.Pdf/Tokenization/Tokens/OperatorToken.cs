namespace UglyToad.Pdf.Tokenization.Tokens
{
    using System.Collections.Generic;

    public class OperatorToken : IDataToken<string>
    {
        private static readonly Dictionary<string, string> PooledNames = new Dictionary<string, string>();

        public string Data { get; }

        public OperatorToken(string data)
        {
            if (!PooledNames.TryGetValue(data, out var stored))
            {
                stored = data;
                PooledNames[data] = stored;
            }

            Data = stored;
        }
    }
}
