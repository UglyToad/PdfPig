namespace UglyToad.Pdf.Tokenization.Tokens
{
    using System.Collections.Generic;

    public class OperatorToken : IDataToken<string>
    {
        private static readonly Dictionary<string, string> PooledNames = new Dictionary<string, string>();

        public static readonly OperatorToken R = new OperatorToken("R");
        public static readonly OperatorToken StartObject = new OperatorToken("obj");
        public static readonly OperatorToken EndObject = new OperatorToken("endobj");
        public static readonly OperatorToken StartStream = new OperatorToken("stream");
        public static readonly OperatorToken EndStream = new OperatorToken("endstream");

        public string Data { get; }

        private OperatorToken(string data)
        {
            if (!PooledNames.TryGetValue(data, out var stored))
            {
                stored = data;
                PooledNames[data] = stored;
            }

            Data = stored;
        }

        public static OperatorToken Create(string data)
        {
            switch (data)
            {
                case "R":
                    return R;
                case "obj":
                    return StartObject;
                case "endobj":
                    return EndObject;
                case "stream":
                    return StartStream;
                case "endstream":
                    return EndStream;
                default:
                    return new OperatorToken(data);
            }
        }

        public override string ToString()
        {
            return Data;
        }
    }
}
