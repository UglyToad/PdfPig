namespace UglyToad.PdfPig.Util
{
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    internal static class EnumerableExtensions
    {
        public static List<T> ToRecursiveOrderList<T>(this IEnumerable<T> collection,
            Expression<Func<T, IEnumerable<T>>> childCollection)
        {
            var resultList = new List<T>();
            var currentItems = new Queue<(int Index, T Item, int Depth)>(collection.Select(i => (0, i, 0)));
            var depthItemCounter = 0;
            var previousItemDepth = 0;
            var childProperty = (PropertyInfo)((MemberExpression)childCollection.Body).Member;
            while (currentItems.Count > 0)
            {
                var currentItem = currentItems.Dequeue();
                // Reset counter for number of items at this depth when the depth changes.
                if (currentItem.Depth != previousItemDepth)
                {
                    depthItemCounter = 0;
                }

                var resultIndex = currentItem.Index + depthItemCounter++;
                resultList.Insert(resultIndex, currentItem.Item);

                var childItems = childProperty.GetValue(currentItem.Item) as IEnumerable<T> ?? Enumerable.Empty<T>();
                foreach (var childItem in childItems)
                {
                    currentItems.Enqueue((resultIndex + 1, childItem, currentItem.Depth + 1));
                }

                previousItemDepth = currentItem.Depth;
            }

            return resultList;
        }
    }
}