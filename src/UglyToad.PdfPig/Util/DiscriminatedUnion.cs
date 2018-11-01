using System;

namespace UglyToad.PdfPig.Util
{
    // ReSharper disable once InconsistentNaming
    internal abstract class DiscriminatedUnion<A, B>
    {
        public abstract T Match<T>(Func<A, T> first, Func<B, T> second);

        private DiscriminatedUnion() { }

        public sealed class Case1 : DiscriminatedUnion<A, B>
        {
            public readonly A Item;

            public Case1(A item)
            {
                Item = item;
            }

            public override T Match<T>(Func<A, T> first, Func<B, T> second)
            {
                return first(Item);
            }

            public override string ToString()
            {
                return Item?.ToString() ?? string.Empty;
            }
        }

        public sealed class Case2 : DiscriminatedUnion<A, B>
        {
            public readonly B Item;

            public Case2(B item)
            {
                Item = item;
            }

            public override T Match<T>(Func<A, T> first, Func<B, T> second)
            {
                return second(Item);
            }

            public override string ToString()
            {
                return Item?.ToString() ?? string.Empty;
            }
        }
    }
}
