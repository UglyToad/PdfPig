namespace UglyToad.PdfPig.Fonts.Type1.Parser
{
    using System;
    using System.Globalization;

    internal sealed class Type1DataToken : Type1Token
    {
        public ReadOnlyMemory<byte> Data { get; }

        public override bool IsPrivateDictionary { get; } = false;

        public Type1DataToken(TokenType type, ReadOnlyMemory<byte> data) : base(string.Empty, type)
        {
            if (type != TokenType.Charstring)
            {
                throw new ArgumentException($"Invalid token type for type 1 token receiving bytes, expected Charstring, got {type}.");
            }

            Data = data;
        }

        public override string ToString()
        {
            return $"Token[type = {Type}, data = {Data.Length} bytes]";
        }
    }

    internal class Type1Token
    {
        public TokenType Type { get; }
        public string Text { get; }

        public virtual bool IsPrivateDictionary => Type == TokenType.Literal && string.Equals(Text, "Private", StringComparison.OrdinalIgnoreCase);

        public Type1Token(char c, TokenType type) : this(c.ToString(), type) { }
        public Type1Token(string text, TokenType type)
        {
            Text = text;
            Type = type;
        }

        public int AsInt()
        {
            return (int)AsDouble();
        }

        public double AsDouble()
        {
            return double.Parse(Text, CultureInfo.InvariantCulture);
        }

        public bool AsBool()
        {
            return string.Equals(Text, "true", StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return $"Token[type={Type}, text={Text}]";
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
