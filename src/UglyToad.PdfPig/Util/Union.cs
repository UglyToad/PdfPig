// ReSharper disable InconsistentNaming
namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Diagnostics;

    internal abstract class Union<A, B>
    {
        public abstract void Match(Action<A> first, Action<B> second);

        public abstract TResult Match<TResult>(Func<A, TResult> first, Func<B, TResult> second);

        private Union() { }

        public static Case1 One(A item)
        {
            return new Case1(item);
        }

        public static Case2 Two(B item)
        {
            return new Case2(item);
        }

        public sealed class Case1 : Union<A, B>
        {
            public readonly A Item;

            public Case1(A item)
            {
                Item = item;
            }

            [DebuggerStepThrough]
            public override void Match(Action<A> first, Action<B> second)
            {
                first(Item);
            }

            [DebuggerStepThrough]
            public override TResult Match<TResult>(Func<A, TResult> first, Func<B, TResult> second)
            {
                return first(Item);
            }

            public override string ToString()
            {
                return Item?.ToString() ?? string.Empty;
            }
        }

        public sealed class Case2 : Union<A, B>
        {
            public readonly B Item;

            public Case2(B item)
            {
                Item = item;
            }

            [DebuggerStepThrough]
            public override void Match(Action<A> first, Action<B> second)
            {
                second(Item);
            }

            [DebuggerStepThrough]
            public override TResult Match<TResult>(Func<A, TResult> first, Func<B, TResult> second)
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
