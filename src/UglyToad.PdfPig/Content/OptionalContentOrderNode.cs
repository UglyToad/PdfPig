namespace UglyToad.PdfPig.Content
{
    using System.Collections.Generic;

    /// <summary>
    /// An entry of the optional content presentation tree (the default configuration's <c>/Order</c>,
    /// ISO 32000-2 §8.11.4.3): either a group, possibly with sublayers, or a labelled collection of
    /// entries.
    /// </summary>
    public sealed class OptionalContentOrderNode
    {
        internal OptionalContentOrderNode(OptionalContentGroup? group, string? label,
            IReadOnlyList<OptionalContentOrderNode> children)
        {
            Group = group;
            Label = label;
            Children = children;
        }

        /// <summary>
        /// The group this entry presents, or <see langword="null"/> for a collection.
        /// </summary>
        public OptionalContentGroup? Group { get; }

        /// <summary>
        /// The text label of a collection; <see langword="null"/> for a group and for an unlabelled collection.
        /// </summary>
        public string? Label { get; }

        /// <summary>
        /// The group's sublayers, or the collection's entries.
        /// </summary>
        public IReadOnlyList<OptionalContentOrderNode> Children { get; }
    }
}
