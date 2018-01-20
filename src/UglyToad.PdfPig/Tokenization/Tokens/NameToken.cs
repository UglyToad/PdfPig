namespace UglyToad.PdfPig.Tokenization.Tokens
{
    internal partial class NameToken : IDataToken<string>
    {
        public string Data { get; }
        
        private NameToken(string text)
        {
            NameMap[text] = this;

            Data = text;
        }

        public static NameToken Create(string name)
        {
            if (!NameMap.TryGetValue(name, out var value))
            {
                return new NameToken(name);
            }

            return value;
        }
        
        public override bool Equals(object obj)
        {
            return Equals(obj as NameToken);
        }

        public bool Equals(NameToken other)
        {
            return string.Equals(Data, other?.Data);
        }

        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }

        public static implicit operator string(NameToken name)
        {
            return name?.Data;
        }

        public override string ToString()
        {
            return $"/{Data}";
        }
    }
}