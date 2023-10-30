namespace UglyToad.PdfPig.Functions.Type4
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Provides the stack operators such as "Pop" and "dup".
    /// </summary>
    internal sealed class StackOperators
    {
        private StackOperators()
        {
            // Private constructor.
        }

        /// <summary>
        /// Implements the "copy" operator.
        /// </summary>
        internal sealed class Copy : Operator
        {
            public void Execute(ExecutionContext context)
            {
                // Duplicate top n stack items
                int n = (int)context.Stack.Pop();
                if (n > 0)
                {
                    // Need to copy to a new list to avoid ConcurrentModificationException
                    context.AddAllToStack(context.Stack.ToList().Take(n));

                    /* For reference, the PdfBox code:
                     * https://github.com/apache/pdfbox/blob/18a1931bae5b8c1766cd4976d0fb0e28649190b5/pdfbox/src/main/java/org/apache/pdfbox/pdmodel/common/function/type4/StackOperators.java#L41C1-L50C14
                     * int size = stack.size();
                       //Need to copy to a new list to avoid ConcurrentModificationException
                       List<Object> copy = new java.util.ArrayList<>(
                       stack.subList(size - n, size));
                       stack.addAll(copy);
                     */
                }
            }
        }

        /// <summary>
        /// Implements the "dup" operator.
        /// </summary>
        internal sealed class Dup : Operator
        {
            public void Execute(ExecutionContext context)
            {
                context.Stack.Push(context.Stack.Peek());
            }
        }

        /// <summary>
        /// Implements the "exch" operator.
        /// </summary>
        internal sealed class Exch : Operator
        {
            public void Execute(ExecutionContext context)
            {
                object any2 = context.Stack.Pop();
                object any1 = context.Stack.Pop();
                context.Stack.Push(any2);
                context.Stack.Push(any1);
            }
        }

        /// <summary>
        /// Implements the "index" operator.
        /// </summary>
        internal sealed class Index : Operator
        {
            public void Execute(ExecutionContext context)
            {
                int n = Convert.ToInt32(context.Stack.Pop());
                if (n < 0)
                {
                    throw new ArgumentException("rangecheck: " + n);
                }
                context.Stack.Push(context.Stack.ElementAt(n));
            }
        }

        /// <summary>
        /// Implements the "Pop" operator.
        /// </summary>
        internal sealed class Pop : Operator
        {
            public void Execute(ExecutionContext context)
            {
                context.Stack.Pop();
            }
        }

        /// <summary>
        /// Implements the "roll" operator.
        /// </summary>
        internal sealed class Roll : Operator
        {
            public void Execute(ExecutionContext context)
            {
                int j = (int)context.Stack.Pop();
                int n = (int)context.Stack.Pop();
                if (j == 0)
                {
                    return; //Nothing to do
                }
                if (n < 0)
                {
                    throw new ArgumentException("rangecheck: " + n);
                }

                var rolled = new List<object>();
                var moved = new List<object>();
                if (j < 0)
                {
                    //negative roll
                    int n1 = n + j;
                    for (int i = 0; i < n1; i++)
                    {
                        moved.Add(context.Stack.Pop());
                    }
                    for (int i = j; i < 0; i++)
                    {
                        rolled.Add(context.Stack.Pop());
                    }

                    context.AddAllToStack(moved);
                    context.AddAllToStack(rolled);
                }
                else
                {
                    //positive roll
                    int n1 = n - j;
                    for (int i = j; i > 0; i--)
                    {
                        rolled.Add(context.Stack.Pop());
                    }
                    for (int i = 0; i < n1; i++)
                    {
                        moved.Add(context.Stack.Pop());
                    }

                    context.AddAllToStack(rolled);
                    context.AddAllToStack(moved);
                }
            }
        }
    }
}
