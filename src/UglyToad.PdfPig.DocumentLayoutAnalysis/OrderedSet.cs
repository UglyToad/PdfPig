namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    internal class OrderedSet<T>
    {
        private readonly HashSet<T> _set;
        private readonly List<T> _list;

        public OrderedSet() : this(EqualityComparer<T>.Default)
        {

        }

        public OrderedSet(IEqualityComparer<T> comparer)
        {
            _set = new HashSet<T>(comparer);
            _list = new List<T>();
        }

        public int Count => _set.Count;

        public bool TryAdd(T item)
        {
            if (_set.Contains(item)) return false;

            _list.Add(item);
            _set.Add(item);

            return true;
        }

        public void Clear()
        {
            _list.Clear();
            _set.Clear();
        }

        public bool Contains(T item)
        {
            return item is not null && _set.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }
        public List<T> GetList()
        {
            return _list;
        }
    }
}
