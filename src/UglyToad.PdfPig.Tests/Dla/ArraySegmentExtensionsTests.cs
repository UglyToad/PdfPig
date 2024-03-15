namespace UglyToad.PdfPig.Tests.Dla
{
    using UglyToad.PdfPig.DocumentLayoutAnalysis;

    public class ArraySegmentExtensionsTests
    {
        [Fact]
        public void TakeGetAt()
        {
            ArraySegment<int> array = new ArraySegment<int>(Enumerable.Range(0, 10).ToArray());
            Assert.Equal(10, array.Count);

            // Take first 5
            ArraySegment<int> arrayFirst5 = array.Take(5);
            Assert.Equal(5, arrayFirst5.Count);
            Assert.Equal(0, arrayFirst5.GetAt(0));
            Assert.Equal(1, arrayFirst5.GetAt(1));
            Assert.Equal(2, arrayFirst5.GetAt(2));
            Assert.Equal(3, arrayFirst5.GetAt(3));
            Assert.Equal(4, arrayFirst5.GetAt(4));

            // Take first 2 of first 5
            ArraySegment<int> arrayFirst2of5 = arrayFirst5.Take(2);
            Assert.Equal(2, arrayFirst2of5.Count);
            Assert.Equal(0, arrayFirst2of5.GetAt(0));
            Assert.Equal(1, arrayFirst2of5.GetAt(1));
        }

        [Fact]
        public void SkipGetAt()
        {
            ArraySegment<int> array = new ArraySegment<int>(Enumerable.Range(0, 10).ToArray());
            Assert.Equal(10, array.Count);

            // Skip first 5
            ArraySegment<int> arrayFirst5 = array.Skip(5);
            Assert.Equal(5, arrayFirst5.Count);
            Assert.Equal(5, arrayFirst5.GetAt(0));
            Assert.Equal(6, arrayFirst5.GetAt(1));
            Assert.Equal(7, arrayFirst5.GetAt(2));
            Assert.Equal(8, arrayFirst5.GetAt(3));
            Assert.Equal(9, arrayFirst5.GetAt(4));

            // Skip first 2 of first 5
            ArraySegment<int> arrayFirst2of5 = arrayFirst5.Skip(2);
            Assert.Equal(3, arrayFirst2of5.Count);
            Assert.Equal(7, arrayFirst2of5.GetAt(0));
            Assert.Equal(8, arrayFirst2of5.GetAt(1));
            Assert.Equal(9, arrayFirst2of5.GetAt(2));
        }

        [Fact]
        public void SkipTakeGetAt()
        {
            ArraySegment<int> array = new ArraySegment<int>(Enumerable.Range(0, 10).ToArray());
            Assert.Equal(10, array.Count);

            // Skip first 5
            ArraySegment<int> arrayFirst5 = array.Skip(5);
            Assert.Equal(5, arrayFirst5.Count);
            Assert.Equal(5, arrayFirst5.GetAt(0));
            Assert.Equal(6, arrayFirst5.GetAt(1));
            Assert.Equal(7, arrayFirst5.GetAt(2));
            Assert.Equal(8, arrayFirst5.GetAt(3));
            Assert.Equal(9, arrayFirst5.GetAt(4));

            // Skip first 2 of first 5
            ArraySegment<int> arrayFirst2of5 = arrayFirst5.Take(2);
            Assert.Equal(2, arrayFirst2of5.Count);
            Assert.Equal(5, arrayFirst2of5.GetAt(0));
            Assert.Equal(6, arrayFirst2of5.GetAt(1));
            Assert.Equal(7, arrayFirst2of5.GetAt(2));
        }

        [Fact]
        public void TakeSkipGetAt()
        {
            ArraySegment<int> array = new ArraySegment<int>(Enumerable.Range(0, 10).ToArray());
            Assert.Equal(10, array.Count);

            // Take first 5
            ArraySegment<int> arrayFirst5 = array.Take(5);
            Assert.Equal(5, arrayFirst5.Count);
            Assert.Equal(0, arrayFirst5.GetAt(0));
            Assert.Equal(1, arrayFirst5.GetAt(1));
            Assert.Equal(2, arrayFirst5.GetAt(2));
            Assert.Equal(3, arrayFirst5.GetAt(3));
            Assert.Equal(4, arrayFirst5.GetAt(4));

            // Take first 2 of first 5
            ArraySegment<int> arrayFirst2of5 = arrayFirst5.Skip(2);
            Assert.Equal(3, arrayFirst2of5.Count);
            Assert.Equal(2, arrayFirst2of5.GetAt(0));
            Assert.Equal(3, arrayFirst2of5.GetAt(1));
            Assert.Equal(4, arrayFirst2of5.GetAt(2));
        }

        [Fact]
        public void Sort()
        {
            IntInverseComparer intInverseComparer = new IntInverseComparer();
            IntComparer intComparer = new IntComparer();

            int[] originalArray = Enumerable.Range(0, 10).ToArray();

            ArraySegment<int> array = new ArraySegment<int>(originalArray);
            Assert.Equal(10, array.Count);

            array.Sort(intInverseComparer);
            Assert.Equal(10, array.Count);
            Assert.Equal(9, array.GetAt(0));
            Assert.Equal(8, array.GetAt(1));
            Assert.Equal(7, array.GetAt(2));
            Assert.Equal(6, array.GetAt(3));
            Assert.Equal(5, array.GetAt(4));
            Assert.Equal(4, array.GetAt(5));
            Assert.Equal(3, array.GetAt(6));
            Assert.Equal(2, array.GetAt(7));
            Assert.Equal(1, array.GetAt(8));
            Assert.Equal(0, array.GetAt(9));

            ArraySegment<int> skip1Take7 = array.Skip(1).Take(7);
            Assert.Equal(7, skip1Take7.Count);
            Assert.Equal(8, skip1Take7.GetAt(0));
            Assert.Equal(7, skip1Take7.GetAt(1));
            Assert.Equal(6, skip1Take7.GetAt(2));
            Assert.Equal(5, skip1Take7.GetAt(3));
            Assert.Equal(4, skip1Take7.GetAt(4));
            Assert.Equal(3, skip1Take7.GetAt(5));
            Assert.Equal(2, skip1Take7.GetAt(6));

            skip1Take7.Sort(intComparer);
            Assert.Equal(7, skip1Take7.Count);
            Assert.Equal(2, skip1Take7.GetAt(0));
            Assert.Equal(3, skip1Take7.GetAt(1));
            Assert.Equal(4, skip1Take7.GetAt(2));
            Assert.Equal(5, skip1Take7.GetAt(3));
            Assert.Equal(6, skip1Take7.GetAt(4));
            Assert.Equal(7, skip1Take7.GetAt(5));
            Assert.Equal(8, skip1Take7.GetAt(6));

            Assert.Equal(10, array.Count);
            Assert.Equal(9, array.GetAt(0));
            Assert.Equal(2, array.GetAt(1));
            Assert.Equal(3, array.GetAt(2));
            Assert.Equal(4, array.GetAt(3));
            Assert.Equal(5, array.GetAt(4));
            Assert.Equal(6, array.GetAt(5));
            Assert.Equal(7, array.GetAt(6));
            Assert.Equal(8, array.GetAt(7));
            Assert.Equal(1, array.GetAt(8));
            Assert.Equal(0, array.GetAt(9));

            Assert.Equal(9, originalArray[0]);
            Assert.Equal(2, originalArray[1]);
            Assert.Equal(3, originalArray[2]);
            Assert.Equal(4, originalArray[3]);
            Assert.Equal(5, originalArray[4]);
            Assert.Equal(6, originalArray[5]);
            Assert.Equal(7, originalArray[6]);
            Assert.Equal(8, originalArray[7]);
            Assert.Equal(1, originalArray[8]);
            Assert.Equal(0, originalArray[9]);
        }

        private class IntInverseComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return -x.CompareTo(y);
            }
        }

        private class IntComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return x.CompareTo(y);
            }
        }
    }
}
