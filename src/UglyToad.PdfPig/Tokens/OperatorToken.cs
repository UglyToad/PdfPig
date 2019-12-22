namespace UglyToad.PdfPig.Tokens
{
    using System.Collections.Generic;

    internal class OperatorToken : IDataToken<string>
    {
        private static readonly object Lock = new object();
        private static readonly Dictionary<string, string> PooledNames = new Dictionary<string, string>();

        public static readonly OperatorToken Bt = new OperatorToken("BT");
        public static readonly OperatorToken Def = new OperatorToken("def");
        public static readonly OperatorToken Dict = new OperatorToken("dict");
        public static readonly OperatorToken Dup = new OperatorToken("dup");
        public static readonly OperatorToken Eexec = new OperatorToken("eexec");
        public static readonly OperatorToken EndObject = new OperatorToken("endobj");
        public static readonly OperatorToken EndStream = new OperatorToken("endstream");
        public static readonly OperatorToken Et = new OperatorToken("ET");
        public static readonly OperatorToken For = new OperatorToken("for");
        public static readonly OperatorToken N = new OperatorToken("n");
        public static readonly OperatorToken Put = new OperatorToken("put");
        public static readonly OperatorToken QPop = new OperatorToken("Q");
        public static readonly OperatorToken QPush = new OperatorToken("q");
        public static readonly OperatorToken R = new OperatorToken("R");
        public static readonly OperatorToken Re = new OperatorToken("re");
        public static readonly OperatorToken Readonly = new OperatorToken("readonly");
        public static readonly OperatorToken StartObject = new OperatorToken("obj");
        public static readonly OperatorToken StartStream = new OperatorToken("stream");
        public static readonly OperatorToken Tf = new OperatorToken("Tf");
        public static readonly OperatorToken WStar = new OperatorToken("W*");
        public static readonly OperatorToken Xref = new OperatorToken("xref");

        public string Data { get; }

        private OperatorToken(string data)
        {
            string stored;

            lock (Lock)
            {
                if (!PooledNames.TryGetValue(data, out stored))
                {
                    stored = data;
                    PooledNames[data] = stored;
                }
            }

            Data = stored;
        }

        public static OperatorToken Create(string data)
        {
            switch (data)
            {
                case "BT":
                    return Bt;
                case "eexec":
                    return Eexec;
                case "endobj":
                    return EndObject;
                case "endstream":
                    return EndStream;
                case "ET":
                    return Et;
                case "def":
                    return Def;
                case "dict":
                    return Dict;
                case "for":
                    return For;
                case "dup":
                    return Dup;
                case "n":
                    return N;
                case "obj":
                    return StartObject;
                case "put":
                    return Put;
                case "Q":
                    return QPop;
                case "q":
                    return QPush;
                case "R":
                    return R;
                case "re":
                    return Re;
                case "readonly":
                    return Readonly;
                case "stream":
                    return StartStream;
                case "Tf":
                    return Tf;
                case "W*":
                    return WStar;
                case "xref":
                    return Xref;
                default:
                    return new OperatorToken(data);
            }
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Data;
        }
    }
}
