namespace UglyToad.PdfPig.Tokens
{
    using System;
    using System.Collections.Generic;

    /// <inheritdoc />
    /// <summary>
    /// An operator token encountered in a page content or Adobe Type 1 font stream.
    /// </summary>
    public class OperatorToken : IDataToken<string>
    {
        private static readonly object Lock = new object();
        private static readonly Dictionary<string, string> PooledNames = new Dictionary<string, string>();

        /// <summary>
        /// Begin text.
        /// </summary>
        public static readonly OperatorToken Bt = new OperatorToken("BT");

        /// <summary>
        /// Def.
        /// </summary>
        public static readonly OperatorToken Def = new OperatorToken("def");

        /// <summary>
        /// Dict.
        /// </summary>
        public static readonly OperatorToken Dict = new OperatorToken("dict");

        /// <summary>
        /// Dup.
        /// </summary>
        public static readonly OperatorToken Dup = new OperatorToken("dup");

        /// <summary>
        /// Eexec.
        /// </summary>
        public static readonly OperatorToken Eexec = new OperatorToken("eexec");

        /// <summary>
        /// End object.
        /// </summary>
        public static readonly OperatorToken EndObject = new OperatorToken("endobj");

        /// <summary>
        /// End stream.
        /// </summary>
        public static readonly OperatorToken EndStream = new OperatorToken("endstream");

        /// <summary>
        /// End text.
        /// </summary>
        public static readonly OperatorToken Et = new OperatorToken("ET");

        /// <summary>
        /// For.
        /// </summary>
        public static readonly OperatorToken For = new OperatorToken("for");

        /// <summary>
        /// N.
        /// </summary>
        public static readonly OperatorToken N = new OperatorToken("n");

        /// <summary>
        /// Put.
        /// </summary>
        public static readonly OperatorToken Put = new OperatorToken("put");

        /// <summary>
        /// Pop.
        /// </summary>
        public static readonly OperatorToken QPop = new OperatorToken("Q");

        /// <summary>
        /// Push.
        /// </summary>
        public static readonly OperatorToken QPush = new OperatorToken("q");

        /// <summary>
        /// R.
        /// </summary>
        public static readonly OperatorToken R = new OperatorToken("R");

        /// <summary>
        /// Rectangle.
        /// </summary>
        public static readonly OperatorToken Re = new OperatorToken("re");

        /// <summary>
        /// Readonly.
        /// </summary>
        public static readonly OperatorToken Readonly = new OperatorToken("readonly");

        /// <summary>
        /// Object.
        /// </summary>
        public static readonly OperatorToken StartObject = new OperatorToken("obj");

        /// <summary>
        /// Stream.
        /// </summary>
        public static readonly OperatorToken StartStream = new OperatorToken("stream");

        /// <summary>
        /// Set font and size.
        /// </summary>
        public static readonly OperatorToken Tf = new OperatorToken("Tf");

        /// <summary>
        /// Modify clipping.
        /// </summary>
        public static readonly OperatorToken WStar = new OperatorToken("W*");

        /// <summary>
        /// Cross reference.
        /// </summary>
        public static readonly OperatorToken Xref = new OperatorToken("xref");

        /// <summary>
        /// Cross reference section offset.
        /// </summary>
        public static readonly OperatorToken StartXref = new OperatorToken("startxref");

        /// <inheritdoc />
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

        /// <summary>
        /// Create a new <see cref="OperatorToken"/>.
        /// </summary>
        public static OperatorToken Create(ReadOnlySpan<char> data)
        {
            return data switch {
                "BT" => Bt,
                "eexec" => Eexec,
                "endobj" => EndObject,
                "endstream" => EndStream,
                "ET" => Et,
                "def" => Def,
                "dict" => Dict,
                "for" => For,
                "dup" => Dup,
                "n" => N,
                "obj" => StartObject,
                "put" => Put,
                "Q" => QPop,
                "q" => QPush,
                "R" => R,
                "re" => Re,
                "readonly" => Readonly,
                "stream" => StartStream,
                "Tf" => Tf,
                "W*" => WStar,
                "xref" => Xref,
                "startxref" => StartXref,
                _ => new OperatorToken(data.ToString())
            };
        }

        /// <inheritdoc />
        public bool Equals(IToken obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is OperatorToken other))
            {
                return false;
            }

            return Data == other.Data;
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
