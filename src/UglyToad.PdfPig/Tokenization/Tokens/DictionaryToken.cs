namespace UglyToad.PdfPig.Tokenization.Tokens
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cos;
    using Util.JetBrains.Annotations;

    internal class DictionaryToken : IDataToken<IReadOnlyDictionary<string, IToken>>
    {
        [NotNull]
        public IReadOnlyDictionary<string, IToken> Data { get; }

        public DictionaryToken([NotNull]IReadOnlyDictionary<IToken, IToken> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            
            var result = new Dictionary<string, IToken>(data.Count);

            foreach (var keyValuePair in data)
            {
                if (keyValuePair.Key is NameToken name)
                {
                    result[name.Data.Name] = keyValuePair.Value;
                }
                else
                {
                    // For now:
                    throw new InvalidOperationException("Key for dictionary token was not a string! " + keyValuePair.Key);
                }
            }

            Data = result;
        }
        
        public bool TryGetByName(CosName name, out IToken token)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return Data.TryGetValue(name.Name, out token);
        }

        public override string ToString()
        {
            return string.Join(", ", Data.Select(x => $"<{x.Key}, {x.Value}>"));
        }
    }
}
