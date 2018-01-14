namespace UglyToad.PdfPig.Tokenization.Tokens
{
    using System.Collections.Generic;

    internal class OperatorToken : IDataToken<string>
    {
        private static readonly Dictionary<string, string> PooledNames = new Dictionary<string, string>();

        public static readonly OperatorToken R = new OperatorToken("R");
        public static readonly OperatorToken StartObject = new OperatorToken("obj");
        public static readonly OperatorToken EndObject = new OperatorToken("endobj");
        public static readonly OperatorToken StartStream = new OperatorToken("stream");
        public static readonly OperatorToken EndStream = new OperatorToken("endstream");
        public static readonly OperatorToken Eexec = new OperatorToken("eexec");
        public static readonly OperatorToken Def = new OperatorToken("def");
        public static readonly OperatorToken Dict = new OperatorToken("dict");
        public static readonly OperatorToken Readonly = new OperatorToken("readonly");
        public static readonly OperatorToken Dup = new OperatorToken("dup");
        public static readonly OperatorToken For = new OperatorToken("for");
        public static readonly OperatorToken Put = new OperatorToken("put");

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
                case "eexec":
                    return Eexec;
                case "def":
                    return Def;
                case "dict":
                    return Dict;
                case "readonly":
                    return Readonly;
                case "dup":
                    return Dup;
                case "for":
                    return For;
                case "put":
                    return Put;
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
