namespace UglyToad.PdfPig.Tokenization.Tokens
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
                    result[name.Data] = keyValuePair.Value;
                }
                else
                {
                    // For now:
                    throw new InvalidOperationException("Key for dictionary token was not a string! " + keyValuePair.Key);
                }
            }

            Data = result;
        }

        private DictionaryToken(IReadOnlyDictionary<string, IToken> data)
        {
            Data = data;
        }
        
        public bool TryGet(NameToken name, out IToken token)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return Data.TryGetValue(name.Data, out token);
        }

        public bool ContainsKey(NameToken name)
        {
            return Data.ContainsKey(name.Data);
        }

        public DictionaryToken With(NameToken key, IToken value) => With(key.Data, value);
        public DictionaryToken With(string key, IToken value)
        {
            var result = new Dictionary<string, IToken>(Data.Count + 1);

            foreach (var keyValuePair in Data)
            {
                result[keyValuePair.Key] = keyValuePair.Value;
            }

            result[key] = value;

            return new DictionaryToken(result);
        }
        
        public override string ToString()
        {
            return string.Join(", ", Data.Select(x => $"<{x.Key}, {x.Value}>"));
        }
    }
}
