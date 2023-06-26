namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Useful <see cref="ArraySegment{T}"/> extensions.
    /// </summary>
    public static class ArraySegmentExtensions
    {
        /// <summary>
        /// Returns a specified number of contiguous elements from the start of a sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <see name="source"/>.</typeparam>
        /// <param name="source">An <see cref="ArraySegment{T}"/> to return elements from.</param>
        /// <param name="count">The number of elements to return.</param>
        /// <returns>An <see cref="ArraySegment{T}"/> that contains the specified number of elements from the start of the input sequence.</returns>
        public static ArraySegment<T> Take<T>(this ArraySegment<T> source, int count)
        {
            return new ArraySegment<T>(source.Array, source.Offset, count);
        }

        /// <summary>
        /// Bypasses a specified number of elements in a sequence and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <see name="source"/>.</typeparam>
        /// <param name="source">An <see cref="ArraySegment{T}"/> to return elements from.</param>
        /// <param name="count">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>An <see cref="ArraySegment{T}"/> that contains the elements that occur after the specified index in the input sequence.</returns>
        public static ArraySegment<T> Skip<T>(this ArraySegment<T> source, int count)
        {
            return new ArraySegment<T>(source.Array, source.Offset + count, source.Count - count);
        }

        /// <summary>
        /// Sorts the elements in a <see cref="ArraySegment{T}"/> using the specified <see cref="IComparer{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <see name="source"/>.</typeparam>
        /// <param name="source">The <see cref="ArraySegment{T}"/> to sort.</param>
        /// <param name="comparer">The implementation to use when comparing elements.</param>
        public static void Sort<T>(this ArraySegment<T> source, IComparer<T> comparer)
        {
            Array.Sort(source.Array, source.Offset, source.Count, comparer);
        }

        /// <summary>
        /// Returns the element at a specified index in a sequence.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <see name="source"/>.</typeparam>
        /// <param name="source">The <see cref="ArraySegment{T}"/> to get the element from.</param>
        /// <param name="index">The index of the element to retrieve.</param>
        /// <returns>The element at the specified position in the <see name="source"/> sequence.</returns>
        public static T GetAt<T>(this ArraySegment<T> source, int index)
        {
            return source.Array[source.Offset + index];
        }
    }
}
