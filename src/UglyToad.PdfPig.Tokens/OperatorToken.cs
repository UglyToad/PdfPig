namespace UglyToad.PdfPig.Tokens
{
    using System;
    using System.Collections.Generic;
    using Core;

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

        /// <summary>
        /// Shared instances of the content stream operators.
        /// </summary>
        private static class Content
        {
            public static readonly OperatorToken b = new OperatorToken("b");
            public static readonly OperatorToken B = new OperatorToken("B");
            public static readonly OperatorToken bStar = new OperatorToken("b*");
            public static readonly OperatorToken BStar = new OperatorToken("B*");
            public static readonly OperatorToken BDC = new OperatorToken("BDC");
            public static readonly OperatorToken BI = new OperatorToken("BI");
            public static readonly OperatorToken BMC = new OperatorToken("BMC");
            public static readonly OperatorToken BX = new OperatorToken("BX");
            public static readonly OperatorToken c = new OperatorToken("c");
            public static readonly OperatorToken cm = new OperatorToken("cm");
            public static readonly OperatorToken CS = new OperatorToken("CS");
            public static readonly OperatorToken cs = new OperatorToken("cs");
            public static readonly OperatorToken d = new OperatorToken("d");
            public static readonly OperatorToken d0 = new OperatorToken("d0");
            public static readonly OperatorToken d1 = new OperatorToken("d1");
            public static readonly OperatorToken Do = new OperatorToken("Do");
            public static readonly OperatorToken DP = new OperatorToken("DP");
            public static readonly OperatorToken EI = new OperatorToken("EI");
            public static readonly OperatorToken EMC = new OperatorToken("EMC");
            public static readonly OperatorToken EX = new OperatorToken("EX");
            public static readonly OperatorToken f = new OperatorToken("f");
            public static readonly OperatorToken F = new OperatorToken("F");
            public static readonly OperatorToken fStar = new OperatorToken("f*");
            public static readonly OperatorToken G = new OperatorToken("G");
            public static readonly OperatorToken g = new OperatorToken("g");
            public static readonly OperatorToken gs = new OperatorToken("gs");
            public static readonly OperatorToken h = new OperatorToken("h");
            public static readonly OperatorToken i = new OperatorToken("i");
            public static readonly OperatorToken ID = new OperatorToken("ID");
            public static readonly OperatorToken j = new OperatorToken("j");
            public static readonly OperatorToken J = new OperatorToken("J");
            public static readonly OperatorToken K = new OperatorToken("K");
            public static readonly OperatorToken k = new OperatorToken("k");
            public static readonly OperatorToken l = new OperatorToken("l");
            public static readonly OperatorToken m = new OperatorToken("m");
            public static readonly OperatorToken M = new OperatorToken("M");
            public static readonly OperatorToken MP = new OperatorToken("MP");
            public static readonly OperatorToken RG = new OperatorToken("RG");
            public static readonly OperatorToken rg = new OperatorToken("rg");
            public static readonly OperatorToken ri = new OperatorToken("ri");
            public static readonly OperatorToken s = new OperatorToken("s");
            public static readonly OperatorToken S = new OperatorToken("S");
            public static readonly OperatorToken SC = new OperatorToken("SC");
            public static readonly OperatorToken sc = new OperatorToken("sc");
            public static readonly OperatorToken SCN = new OperatorToken("SCN");
            public static readonly OperatorToken scn = new OperatorToken("scn");
            public static readonly OperatorToken sh = new OperatorToken("sh");
            public static readonly OperatorToken TStar = new OperatorToken("T*");
            public static readonly OperatorToken Tc = new OperatorToken("Tc");
            public static readonly OperatorToken Td = new OperatorToken("Td");
            public static readonly OperatorToken TD = new OperatorToken("TD");
            public static readonly OperatorToken Tj = new OperatorToken("Tj");
            public static readonly OperatorToken TJ = new OperatorToken("TJ");
            public static readonly OperatorToken TL = new OperatorToken("TL");
            public static readonly OperatorToken Tm = new OperatorToken("Tm");
            public static readonly OperatorToken Tr = new OperatorToken("Tr");
            public static readonly OperatorToken Ts = new OperatorToken("Ts");
            public static readonly OperatorToken Tw = new OperatorToken("Tw");
            public static readonly OperatorToken Tz = new OperatorToken("Tz");
            public static readonly OperatorToken v = new OperatorToken("v");
            public static readonly OperatorToken w = new OperatorToken("w");
            public static readonly OperatorToken W = new OperatorToken("W");
            public static readonly OperatorToken y = new OperatorToken("y");
            public static readonly OperatorToken Quote = new OperatorToken("'");
            public static readonly OperatorToken DoubleQuote = new OperatorToken("\"");
        }

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
            if (data.Length <= 16)
            {
                Span<byte> bytes = stackalloc byte[data.Length];
                var isAscii = true;
                for (var i = 0; i < data.Length; i++)
                {
                    var c = data[i];
                    if (c > 0x7F)
                    {
                        isAscii = false;
                        break;
                    }

                    bytes[i] = (byte)c;
                }

                if (isAscii)
                {
                    return Create(bytes);
                }
            }

            return new OperatorToken(data.ToString());
        }

        /// <summary>
        /// Create a new <see cref="OperatorToken"/> from the bytes of the operator, returning a shared instance for known operators.
        /// </summary>
        public static OperatorToken Create(ReadOnlySpan<byte> data)
        {
            switch (data.Length)
            {
                case 1:
                    switch (data[0])
                    {
                        case (byte)'b': return Content.b;
                        case (byte)'B': return Content.B;
                        case (byte)'c': return Content.c;
                        case (byte)'d': return Content.d;
                        case (byte)'f': return Content.f;
                        case (byte)'F': return Content.F;
                        case (byte)'g': return Content.g;
                        case (byte)'G': return Content.G;
                        case (byte)'h': return Content.h;
                        case (byte)'i': return Content.i;
                        case (byte)'j': return Content.j;
                        case (byte)'J': return Content.J;
                        case (byte)'k': return Content.k;
                        case (byte)'K': return Content.K;
                        case (byte)'l': return Content.l;
                        case (byte)'m': return Content.m;
                        case (byte)'M': return Content.M;
                        case (byte)'n': return N;
                        case (byte)'q': return QPush;
                        case (byte)'Q': return QPop;
                        case (byte)'R': return R;
                        case (byte)'s': return Content.s;
                        case (byte)'S': return Content.S;
                        case (byte)'v': return Content.v;
                        case (byte)'w': return Content.w;
                        case (byte)'W': return Content.W;
                        case (byte)'y': return Content.y;
                        case (byte)'\'': return Content.Quote;
                        case (byte)'"': return Content.DoubleQuote;
                    }

                    break;
                case 2:
                    switch ((data[0] << 8) | data[1])
                    {
                        case ('B' << 8) | 'T': return Bt;
                        case ('E' << 8) | 'T': return Et;
                        case ('r' << 8) | 'e': return Re;
                        case ('T' << 8) | 'f': return Tf;
                        case ('W' << 8) | '*': return WStar;
                        case ('b' << 8) | '*': return Content.bStar;
                        case ('B' << 8) | '*': return Content.BStar;
                        case ('B' << 8) | 'I': return Content.BI;
                        case ('B' << 8) | 'X': return Content.BX;
                        case ('c' << 8) | 'm': return Content.cm;
                        case ('C' << 8) | 'S': return Content.CS;
                        case ('c' << 8) | 's': return Content.cs;
                        case ('d' << 8) | '0': return Content.d0;
                        case ('d' << 8) | '1': return Content.d1;
                        case ('D' << 8) | 'o': return Content.Do;
                        case ('D' << 8) | 'P': return Content.DP;
                        case ('E' << 8) | 'I': return Content.EI;
                        case ('E' << 8) | 'X': return Content.EX;
                        case ('f' << 8) | '*': return Content.fStar;
                        case ('g' << 8) | 's': return Content.gs;
                        case ('I' << 8) | 'D': return Content.ID;
                        case ('M' << 8) | 'P': return Content.MP;
                        case ('R' << 8) | 'G': return Content.RG;
                        case ('r' << 8) | 'g': return Content.rg;
                        case ('r' << 8) | 'i': return Content.ri;
                        case ('S' << 8) | 'C': return Content.SC;
                        case ('s' << 8) | 'c': return Content.sc;
                        case ('s' << 8) | 'h': return Content.sh;
                        case ('T' << 8) | '*': return Content.TStar;
                        case ('T' << 8) | 'c': return Content.Tc;
                        case ('T' << 8) | 'd': return Content.Td;
                        case ('T' << 8) | 'D': return Content.TD;
                        case ('T' << 8) | 'j': return Content.Tj;
                        case ('T' << 8) | 'J': return Content.TJ;
                        case ('T' << 8) | 'L': return Content.TL;
                        case ('T' << 8) | 'm': return Content.Tm;
                        case ('T' << 8) | 'r': return Content.Tr;
                        case ('T' << 8) | 's': return Content.Ts;
                        case ('T' << 8) | 'w': return Content.Tw;
                        case ('T' << 8) | 'z': return Content.Tz;
                    }

                    break;
                case 3:
                    switch ((data[0] << 16) | (data[1] << 8) | data[2])
                    {
                        case ('d' << 16) | ('e' << 8) | 'f': return Def;
                        case ('f' << 16) | ('o' << 8) | 'r': return For;
                        case ('d' << 16) | ('u' << 8) | 'p': return Dup;
                        case ('o' << 16) | ('b' << 8) | 'j': return StartObject;
                        case ('p' << 16) | ('u' << 8) | 't': return Put;
                        case ('B' << 16) | ('D' << 8) | 'C': return Content.BDC;
                        case ('B' << 16) | ('M' << 8) | 'C': return Content.BMC;
                        case ('E' << 16) | ('M' << 8) | 'C': return Content.EMC;
                        case ('S' << 16) | ('C' << 8) | 'N': return Content.SCN;
                        case ('s' << 16) | ('c' << 8) | 'n': return Content.scn;
                    }

                    break;
                case 4:
                    if (data.SequenceEqual("dict"u8))
                    {
                        return Dict;
                    }

                    if (data.SequenceEqual("xref"u8))
                    {
                        return Xref;
                    }

                    break;
                case 5:
                    if (data.SequenceEqual("eexec"u8))
                    {
                        return Eexec;
                    }

                    break;
                case 6:
                    if (data.SequenceEqual("endobj"u8))
                    {
                        return EndObject;
                    }

                    if (data.SequenceEqual("stream"u8))
                    {
                        return StartStream;
                    }

                    break;
                case 8:
                    if (data.SequenceEqual("readonly"u8))
                    {
                        return Readonly;
                    }

                    break;
                case 9:
                    if (data.SequenceEqual("endstream"u8))
                    {
                        return EndStream;
                    }

                    if (data.SequenceEqual("startxref"u8))
                    {
                        return StartXref;
                    }

                    break;
            }

            return new OperatorToken(OtherEncodings.BytesAsLatin1String(data));
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is IToken token && Equals(token);
        }

        /// <inheritdoc />
        public bool Equals(IToken obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is not OperatorToken other)
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
