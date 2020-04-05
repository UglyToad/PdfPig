using System.Collections.Generic;

namespace UglyToad.PdfPig.Tokenization
{
    /// <summary>
    /// An object pool for lists.
    /// </summary>
    public class ListPool<T>
    {
        private readonly int capacity;
        private readonly object locker = new object();
        private readonly Stack<List<T>> pool = new Stack<List<T>>();

        /// <summary>
        /// Create a new <see cref="List{T}"/> holding the number of items specified by the capacity.
        /// </summary>
        public ListPool(int capacity = 5)
        {
            this.capacity = capacity;

            for (var i = 0; i < capacity; i++)
            {
                pool.Push(new List<T>(10));
            }
        }

        /// <summary>
        /// Get an item from the pool, remember to return it using <see cref="Return"/> at the end.
        /// </summary>
        public List<T> Borrow()
        {
            lock (locker)
            {
                if (pool.Count == 0)
                {
                    return new List<T>();
                }

                return pool.Pop();
            }
        }

        /// <summary>
        /// Returns an item to the pool of available lists..
        /// </summary>
        public void Return(List<T> instance)
        {
            if (instance == null)
            {
                return;
            }

            instance.Clear();

            lock (locker)
            {
                if (pool.Count < capacity)
                {
                    pool.Push(instance);
                }
            }
        }
    }
}