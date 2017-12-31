namespace UglyToad.Pdf.ContentStream
{
    using System;
    using System.Collections.Generic;
    using System.Collections;
    using System.Text;
    using Cos;
    using Util.JetBrains.Annotations;

    internal class PdfDictionary : CosBase, IReadOnlyDictionary<CosName, CosBase>
    {
        private readonly Dictionary<CosName, CosBase> inner = new Dictionary<CosName, CosBase>();

        [CanBeNull]
        public CosName GetName(CosName key)
        {
            if (!inner.TryGetValue(key, out CosBase obj) || !(obj is CosName name))
            {
                return null;
            }

            return name;
        }

        public bool TryGetName(CosName key, out CosName value)
        {
            value = GetName(key);

            return value != null;
        }

        public bool IsType(CosName expectedType)
        {
            if (!inner.TryGetValue(CosName.TYPE, out CosBase obj) || obj == null)
            {
                return false;
            }

            switch (obj)
            {
                case CosName name:
                    return expectedType.Equals(name);
                case CosString str:
                    return string.Equals(expectedType.Name, str.GetString());
            }

            return false;
        }

        [CanBeNull]
        public CosBase GetItemOrDefault(CosName key)
        {
            if (inner.TryGetValue(key, out var value))
            {
                return value;
            }

            return null;
        }

        public bool TryGetItemOfType<T>(CosName key, out T item) where T : CosBase
        {
            item = null;
            if (inner.TryGetValue(key, out var value) && value is T t)
            {
                item = t;
                return true;
            }

            return false;
        }

        public void Set(CosName key, CosBase value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            inner[key] = value ?? throw new ArgumentNullException(nameof(value));
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var cosBase in inner)
            {
                builder.Append($"({cosBase.Key}, {cosBase.Value}) ");
            }

            return builder.ToString();
        }

        #region Interface Members
        public int Count => inner.Count;
        public CosBase this[CosName key] => inner[key];
        public IEnumerable<CosName> Keys => inner.Keys;
        public IEnumerable<CosBase> Values => inner.Values;
        public bool ContainsKey(CosName key) => inner.ContainsKey(key);
        public bool TryGetValue(CosName key, out CosBase value) => inner.TryGetValue(key, out value);
        public IEnumerator<KeyValuePair<CosName, CosBase>> GetEnumerator() => inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public override object Accept(ICosVisitor visitor)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
