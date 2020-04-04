namespace UglyToad.PdfPig.Tokenization
{
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// A pool for <see cref="StringBuilder"/>s to reduce allocations during tokenization.
    /// </summary>
    public class StringBuilderPool
    {
        private readonly int capacity;
        private readonly object locker = new object();
        private readonly Stack<StringBuilder> pool = new Stack<StringBuilder>();

        /// <summary>
        /// Create a new <see cref="StringBuilderPool"/> holding the number of items specified by the capacity.
        /// </summary>
        public StringBuilderPool(int capacity = 5)
        {
            this.capacity = capacity;

            for (var i = 0; i < capacity; i++)
            {
                pool.Push(new StringBuilder());
            }
        }

        /// <summary>
        /// Get an item from the pool, remember to return it using <see cref="Return"/> at the end.
        /// </summary>
        public StringBuilder Borrow()
        {
            lock (locker)
            {
                if (pool.Count == 0)
                {
                    return new StringBuilder();
                }

                return pool.Pop();
            }
        }

        /// <summary>
        /// Returns an item to the pool of available builders.
        /// </summary>
        public void Return(StringBuilder instance)
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
