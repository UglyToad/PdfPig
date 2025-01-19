// ReSharper disable InconsistentNaming
namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    internal class StackDictionary<K, V> where K : notnull
    {
        private readonly List<Dictionary<K, V>> values = new List<Dictionary<K, V>>();

        public V this[K key]
        {
            get
            {
                if (TryGetValue(key, out var result))
                {
                    return result;
                }

                throw new KeyNotFoundException($"No item with key {key} in stack.");
            }
            set
            {
                if (values.Count == 0)
                {
                    throw new InvalidOperationException($"Cannot set item in empty stack, call {nameof(Push)} before use.");
                }

                values[values.Count - 1][key] = value;
            }
        }

        public bool TryGetValue(K key, [NotNullWhen(true)] out V result)
        {
            if (values.Count == 0)
            {
                throw new InvalidOperationException($"Cannot get item from empty stack, call {nameof(Push)} before use.");
            }

            for (var i = values.Count - 1; i >= 0; i--)
            {
                if (values[i].TryGetValue(key, out result!))
                {
                    return true;
                }
            }

            result = default!;

            return false;
        }

        public void Push()
        {
            values.Add(new Dictionary<K, V>());
        }

        public void Pop()
        {
            if (values.Count == 0)
            {
                throw new InvalidOperationException("Cannot pop empty stacked dictionary.");
            }

            values.RemoveAt(values.Count - 1);
        }
    }
}
