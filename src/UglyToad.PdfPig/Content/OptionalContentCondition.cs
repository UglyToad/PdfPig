namespace UglyToad.PdfPig.Content
{
    using System;

    /// <summary>
    /// The optional content (8.11) in effect for a piece of content: every optional content group or membership
    /// dictionary that encloses it — <c>BDC /OC</c> sequences and the <c>/OC</c> entries of enclosing form XObjects,
    /// image XObjects and annotations. The content is visible when all of them are (an outer hidden layer hides
    /// inner ones, 8.11.2.1).
    /// <para>
    /// Conditions are created by stream processors and interned per processor: the same chain gives the same
    /// instance, so consumers can evaluate each distinct condition once per state and cache the result by reference.
    /// </para>
    /// <para>
    /// Each group or membership dictionary is resolved when the condition is created, so a condition is
    /// immutable and evaluating it never reads the PDF: it can be evaluated from any thread, concurrently with
    /// the document being processed.
    /// </para>
    /// </summary>
    public sealed class OptionalContentCondition
    {
        private readonly OptionalContentCondition? parent;
        private readonly OptionalContentVisibility? part;
        private readonly OptionalContentState? owner;

        /// <summary>
        /// No optional content in effect: always visible.
        /// </summary>
        public static OptionalContentCondition Always { get; } = new OptionalContentCondition(null, null, null);

        internal OptionalContentCondition(OptionalContentCondition? parent, OptionalContentVisibility? part, OptionalContentState? owner)
        {
            this.parent = parent;
            this.part = part;
            this.owner = owner;
        }

        /// <summary>
        /// Whether no optional content is in effect.
        /// </summary>
        public bool IsAlways => part is null;

        /// <summary>
        /// Whether the content is visible in <paramref name="state"/>: every enclosing group or membership
        /// dictionary is visible.
        /// <para>
        /// Thread-safe, and reads only <paramref name="state"/>'s ON/OFF data: no access to the PDF.
        /// </para>
        /// </summary>
        /// <exception cref="ArgumentException">The state was built for another document.</exception>
        public bool IsVisible(OptionalContentState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (IsAlways)
            {
                return true;
            }

            if (!owner!.IsFromSameDocument(state))
            {
                throw new ArgumentException("The optional content state was built for another document.", nameof(state));
            }

            for (var condition = this; !condition.IsAlways; condition = condition.parent!)
            {
                if (!condition.part!.IsVisible(state))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
