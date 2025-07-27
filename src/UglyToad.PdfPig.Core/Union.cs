// ReSharper disable InconsistentNaming
namespace UglyToad.PdfPig.Core
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Defines a type which is a union of two types.
    /// </summary>
    public abstract class Union<A, B>
    {
        /// <summary>
        /// Take an action for the item of the given type.
        /// </summary>
        public abstract void Match(Action<A> first, Action<B> second);

        /// <summary>
        /// Run a func against the item of the given type.
        /// </summary>
        public abstract TResult Match<TResult>(Func<A, TResult> first, Func<B, TResult> second);

        /// <summary>
        /// Get the item if it is of the specific type.
        /// </summary>
        public abstract bool TryGetFirst(out A a);

        /// <summary>
        /// Get the item if it is of the specific type.
        /// </summary>
        public abstract bool TryGetSecond(out B b);

        private Union() { }

        /// <summary>
        /// Create a value of the first type.
        /// </summary>
        public static Case1 One(A item)
        {
            return new Case1(item);
        }

        /// <summary>
        /// Create a value of the second type.
        /// </summary>
        public static Case2 Two(B item)
        {
            return new Case2(item);
        }

        /// <summary>
        /// The type representing items of the first type in a union.
        /// </summary>
        public sealed class Case1 : Union<A, B>
        {
            /// <summary>
            /// The item.
            /// </summary>
            public readonly A Item;

            /// <summary>
            /// Create first type.
            /// </summary>
            public Case1(A item)
            {
                Item = item;
            }

            /// <inheritdoc />
            [DebuggerStepThrough]
            public override void Match(Action<A> first, Action<B> second)
            {
                first(Item);
            }

            /// <inheritdoc />
            [DebuggerStepThrough]
            public override TResult Match<TResult>(Func<A, TResult> first, Func<B, TResult> second)
            {
                return first(Item);
            }

            /// <inheritdoc />
            public override bool TryGetFirst(out A a)
            {
                a = Item;
                return true;
            }

            /// <inheritdoc />
            public override bool TryGetSecond(out B b)
            {
                b = default!;
                return false;
            }

            /// <inheritdoc />
            public override string ToString()
            {
                return Item?.ToString() ?? string.Empty;
            }
        }

        /// <summary>
        /// The type representing items of the second type in a union.
        /// </summary>
        public sealed class Case2 : Union<A, B>
        {
            /// <summary>
            /// The item.
            /// </summary>
            public readonly B Item;

            /// <summary>
            /// Create second type.
            /// </summary>
            public Case2(B item)
            {
                Item = item;
            }

            /// <inheritdoc />
            [DebuggerStepThrough]
            public override void Match(Action<A> first, Action<B> second)
            {
                second(Item);
            }

            /// <inheritdoc />
            [DebuggerStepThrough]
            public override TResult Match<TResult>(Func<A, TResult> first, Func<B, TResult> second)
            {
                return second(Item);
            }

            /// <inheritdoc />
            public override bool TryGetFirst(out A a)
            {
                a = default!;
                return false;
            }

            /// <inheritdoc />
            public override bool TryGetSecond(out B b)
            {
                b = Item;
                return true;
            }

            /// <inheritdoc />
            public override string ToString()
            {
                return Item?.ToString() ?? string.Empty;
            }
        }
    }
}
