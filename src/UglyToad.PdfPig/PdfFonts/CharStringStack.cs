namespace UglyToad.PdfPig.PdfFonts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// The stack of numeric operands currently active in a CharString.
    /// </summary>
    internal class CharStringStack
    {
        private readonly List<double> stack = new List<double>();

        /// <summary>
        /// The current size of the stack.
        /// </summary>
        public int Length => stack.Count;

        /// <summary>
        /// Whether it's possible to pop a value from either end of the stack.
        /// </summary>
        public bool CanPop => stack.Count > 0;

        /// <summary>
        /// Remove and return the value from the top of the stack.
        /// </summary>
        /// <returns>The value from the top of the stack.</returns>
        public double PopTop()
        {
            if (stack.Count == 0)
            {
                throw new InvalidOperationException("Cannot pop from the top of an empty stack, invalid charstring parsed.");
            }

            var result = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            return result;
        }

        /// <summary>
        /// Remove and return the value from the bottom of the stack.
        /// </summary>
        /// <returns>The value from the bottom of the stack.</returns>
        public double PopBottom()
        {
            if (stack.Count == 0)
            {
                throw new InvalidOperationException("Cannot pop from the bottom of an empty stack, invalid charstring parsed.");
            }

            var result = stack[0];
            stack.RemoveAt(0);
            return result;
        }

        /// <summary>
        /// Adds the value to the top of the stack.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void Push(double value)
        {
            stack.Add(value);
        }

        public double CopyElementAt(int index)
        {
            if (index < 0)
            {
                return stack[stack.Count - 1];
            }

            return stack[index];
        }

        /// <summary>
        /// Removes all values from the stack.
        /// </summary>
        public void Clear()
        {
            stack.Clear();
        }

        public override string ToString()
        {
            return string.Join(" ", stack.Select(x => x.ToString(CultureInfo.InvariantCulture)));
        }
    }
}