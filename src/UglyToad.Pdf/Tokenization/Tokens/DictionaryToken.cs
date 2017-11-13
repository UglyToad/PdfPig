namespace UglyToad.Pdf.Tokenization.Tokens
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cos;
    using Util.JetBrains.Annotations;

    public class DictionaryToken : IDataToken<IReadOnlyDictionary<IToken, IToken>>
    {
        [NotNull]
        public IReadOnlyDictionary<IToken, IToken> Data { get; }

        public DictionaryToken([NotNull]IReadOnlyDictionary<IToken, IToken> data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }
        
        public bool TryGetByName(CosName name, out IToken token)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            token = null;

            foreach (var keyValuePair in Data)
            {
                if (keyValuePair.Key is NameToken nameToken && nameToken.Data.Equals(name))
                {
                    token = keyValuePair.Value;

                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            return string.Join(", ", Data.Select(x => $"<{x.Key}, {x.Value}>"));
        }
    }
}
