namespace UglyToad.PdfPig.Content
{
    using System.Collections.Generic;
    using Tokens;

    /// <summary>
    /// An optional content group (a layer, ISO 32000-2 §8.11.2) of a document, as listed in the
    /// catalog's <c>/OCProperties /OCGs</c> array. Use it with <see cref="OptionalContentState"/> to
    /// query and change the group's state. A group belongs to one document: every state snapshot of
    /// that document shares the same group instances.
    /// </summary>
    public sealed class OptionalContentGroup
    {
        internal OptionalContentGroup(int index, string name, IReadOnlyList<string> intents,
            DictionaryToken dictionary, object owner)
        {
            Index = index;
            Name = name;
            Intents = intents;
            Dictionary = dictionary;
            Owner = owner;
        }

        /// <summary>
        /// The group's <c>/Name</c>, for presentation in a user interface; empty when missing.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The group's <c>/Intent</c> names (e.g. <c>View</c>, <c>Design</c>); <c>View</c> when absent.
        /// </summary>
        public IReadOnlyList<string> Intents { get; }

        /// <summary>
        /// Position in <see cref="OptionalContentState.Groups"/>.
        /// </summary>
        internal int Index { get; }

        internal DictionaryToken Dictionary { get; }

        /// <summary>
        /// The per-document definition this group belongs to.
        /// </summary>
        internal object Owner { get; }

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
