namespace UglyToad.PdfPig.Core
{
    /// <summary>
    /// Provides a guard for tracking and limiting the depth of nested stack operations, such as recursive calls or
    /// nested parsing.
    /// </summary>
    /// <remarks>Use this class to prevent excessive stack usage by enforcing a maximum nesting depth. This is
    /// particularly useful in scenarios where untrusted or deeply nested input could cause stack overflows or
    /// performance issues.</remarks>
    public sealed class StackDepthGuard
    {
        /// <summary>
        /// Represents a stack depth guard with no effective limit on the allowed depth.
        /// </summary>
        /// <remarks>Use this instance when stack depth restrictions are not required.</remarks>
        public static readonly StackDepthGuard Infinite = new StackDepthGuard(int.MaxValue);

        private readonly int maxStackDepth;

        private int depth;

        /// <summary>
        /// Initializes a new instance of the StackDepthGuard class with the specified maximum stack depth.
        /// </summary>
        /// <param name="maxStackDepth">The maximum allowed stack depth for guarded operations. Must be a positive integer.</param>
        public StackDepthGuard(int maxStackDepth)
        {
            if (maxStackDepth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStackDepth));
            }
            this.maxStackDepth = maxStackDepth;
        }

        /// <summary>
        /// Increments the current nesting depth and checks against the maximum allowed stack depth.
        /// </summary>
        /// <exception cref="PdfDocumentFormatException">Thrown if the maximum allowed nesting depth is exceeded.</exception>
        public void Enter()
        {
            if (++depth > maxStackDepth)
            {
                depth--; // Decrement so Exit remains balanced if someone catches this
                throw new PdfDocumentFormatException($"Exceeded maximum nesting depth of {maxStackDepth}.");
            }
        }

        /// <summary>
        /// Decreases the current depth level by one, ensuring that the depth does not become negative.
        /// </summary>
        /// <remarks>If the current depth is already zero, calling this method has no effect. This method
        /// is typically used to track or manage nested operations or scopes where depth must remain
        /// non-negative.</remarks>
        public void Exit()
        {
            depth--;
            if (depth < 0)
            {
                depth = 0;
            }
        }
    }
}
