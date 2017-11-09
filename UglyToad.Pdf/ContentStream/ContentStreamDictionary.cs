namespace UglyToad.Pdf.ContentStream
{
    using System;
    using System.Collections.Generic;
    using System.Collections;
    using Cos;
    using Util.JetBrains.Annotations;

    public class ContentStreamDictionary : CosBase, IReadOnlyDictionary<CosName, CosBase>
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

        public void Set(CosName key, CosBase value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            inner[key] = value ?? throw new ArgumentNullException(nameof(value));
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
