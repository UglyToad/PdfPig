namespace UglyToad.PdfPig.Fonts.Type1.Parser
{
    using System;
    using System.Collections.Generic;

    internal class Type1DataToken : Type1Token
    {
        public IReadOnlyList<byte> Data { get; }

        public Type1DataToken(TokenType type, IReadOnlyList<byte> data) : base(type)
        {
            if (type != TokenType.Charstring)
            {
                throw new ArgumentException($"Invalid token type for type 1 token receiving bytes, expected Charstring, got {type}.");
            }

            Data = data;
        }

        public override string ToString()
        {
            return $"Token[type = {Type}, data = {Data.Count} bytes]";

        }
    }

    internal class Type1TextToken : Type1Token
    {
        public string Text { get; }

        public Type1TextToken(char c, TokenType type) : this(c.ToString(), type) { }
        public Type1TextToken(string text, TokenType type) : base(type)
        {
            Text = text;
        }

        public int AsInt()
        {
            return (int)AsFloat();
        }

        public float AsFloat()
        {
            return float.Parse(Text);
        }

        public bool AsBool()
        {
            return string.Equals(Text, "true", StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return $"Token[type={Type}, text={Text}]";
        }
    }

    internal class Type1Token
    {
        public TokenType Type { get; }

        public Type1Token(TokenType type)
        {
            Type = type;
        }

        public enum TokenType
        {
            None,
            String,
            Name,
            Literal,
            Real,
            Integer,
            /// <summary>
            /// An array must begin with either '[' or '{'. 
            /// </summary>
            StartArray,
            /// <summary>
            /// An array must end with either ']' or '}'. 
            /// </summary>
            EndArray,
            StartProc,
            EndProc,
            StartDict,
            EndDict,
            Charstring
        }
    }
}
