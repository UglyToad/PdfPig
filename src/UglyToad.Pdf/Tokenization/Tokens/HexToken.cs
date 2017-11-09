namespace UglyToad.Pdf.Tokenization.Tokens
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class HexToken : IDataToken<string>
    {
        public string Data { get; }

        public IReadOnlyList<byte> Bytes { get; }

        public HexToken(string characters)
        {
            if (characters.Length % 2 != 0)
            {
                characters += "0";
            }

            var builder = new StringBuilder();
            byte[] raw = new byte[characters.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
               builder.Append((char)Convert.ToByte(characters.Substring(i * 2, 2), 16));
            }

            Bytes = raw;
            Data = builder.ToString();
        }
    }
}