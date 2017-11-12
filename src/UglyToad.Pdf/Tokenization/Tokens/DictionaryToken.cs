namespace UglyToad.Pdf.Tokenization.Tokens
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class DictionaryToken : IDataToken<IReadOnlyDictionary<IToken, IToken>>
    {
        public IReadOnlyDictionary<IToken, IToken> Data { get; }

        public DictionaryToken(IReadOnlyDictionary<IToken, IToken> data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public override string ToString()
        {
            return string.Join(", ", Data.Select(x => $"<{x.Key}, {x.Value}>"));
        }
    }
}
