// ReSharper disable InconsistentNaming
namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    internal sealed class StackDictionary<K, V> where K : notnull
    {
        private readonly List<StackItem> values = new List<StackItem>();

        public V this[K key]
        {
            get
            {
                if (values.Count == 0)
                {
                    throw new InvalidOperationException($"Cannot get item from empty stack, call {nameof(Push)} before use.");
                }

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

                var index = values.Count - 1;
                var level = values[index];

                if (level.IsShared)
                {
                    // Because this level in the stack is shared (i.e. the Dictionary<> came
                    // from Push(Dictionary<> level)), this Dictionary<> can be used outside
                    // here. As a defensive measure we clone it before modifying it.
                    level = new StackItem(new Dictionary<K, V>(level.Values), false);
                    values[index] = level;
                }

                level.Values[key] = value;
            }
        }

        public bool TryGetValue(K key, [NotNullWhen(true)] out V result)
        {
            if (values.Count == 0)
            {
                result = default!;
                return false;
            }

            for (var i = values.Count - 1; i >= 0; i--)
            {
                if (values[i].Values.TryGetValue(key, out result!))
                {
                    return true;
                }
            }

            result = default!;

            return false;
        }

        /// <summary>
        /// Collapses every level currently on the stack into a single dictionary, with entries from
        /// higher (more recently pushed) levels shadowing those from lower ones.
        /// </summary>
        public IReadOnlyDictionary<K, V> Flatten()
        {
            var result = new Dictionary<K, V>();

            foreach (var v in values)
            {
                foreach (var pair in v.Values)
                {
                    result[pair.Key] = pair.Value;
                }
            }

            return result;
        }

        public void Push()
        {
            values.Add(new StackItem(new Dictionary<K, V>(), false));
        }

        /// <summary>
        /// Pushes a pre-computed level. The dictionary is not copied, so the caller may keep and re-push it;
        /// any write to this level copies it first.
        /// </summary>
        public void Push(Dictionary<K, V> level)
        {
            if (level is null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            values.Add(new StackItem(level, true));
        }

        public void Pop()
        {
            if (values.Count == 0)
            {
                throw new InvalidOperationException("Cannot pop empty stacked dictionary.");
            }

            values.RemoveAt(values.Count - 1);
        }

        private readonly struct StackItem
        {
            public readonly Dictionary<K, V> Values;

            /// <summary>
            /// <see langword="true"/> if the caller handed the <see cref="Values"/>, and hence the stack does
            /// NOT own it, and we need to be careful when modifying it.
            /// <para>
            /// <see langword="false"/> if the stack allocated the dictionary, hence owns it (safe to modify).
            /// </para>
            /// </summary>
            public readonly bool IsShared;

            public StackItem(Dictionary<K, V> values, bool isShared)
            {
                Values = values;
                IsShared = isShared;
            }
        }
    }
}
