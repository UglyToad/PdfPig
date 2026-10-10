namespace UglyToad.PdfPig.Graphics
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Content;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// Tracks the marked-content sequences (14.6) open while content streams are processed, and the
    /// state they carry: whether the content is hidden optional content (8.11.3) and the replacement
    /// text in effect (14.9.4).
    /// </summary>
    internal sealed class MarkedContentTracker
    {
        /// <summary>
        /// The document's optional content, or null when it has none: optional content is then ignored (8.11.4.1)
        /// and every condition is <see cref="OptionalContentCondition.Always"/>.
        /// </summary>
        private readonly OptionalContentState? documentState;

        /// <summary>
        /// The state hidden content is evaluated against, or null when nothing is hidden.
        /// </summary>
        private readonly OptionalContentState? visibility;

        /// <summary>
        /// The condition in effect before each open BMC/BDC sequence and each <see cref="Enter"/> scope,
        /// restored when it ends.
        /// </summary>
        private readonly Stack<OptionalContentCondition> enclosing = new();

        private readonly Dictionary<(OptionalContentCondition Parent, DictionaryToken Part), OptionalContentCondition> interned =
            new(ChainComparer.Instance);

        private readonly Dictionary<OptionalContentCondition, bool> hiddenCache = new();

        private readonly bool useActualText;
        private readonly IPdfTokenScanner scanner;

        /// <summary>
        /// Number of open marked-content sequences (BMC/BDC).
        /// </summary>
        private int depth;

        /// <summary>
        /// Number of sequences opened by the streams enclosing the current content stream.
        /// Sequences are balanced within a content stream (14.6), so an EMC cannot end one of those.
        /// </summary>
        private int floor;

        /// <summary>
        /// The replacement text the next glyph receives, null when none is in effect.
        /// It stands for the content of its whole sequence, nested sequences included,
        /// so the first glyph takes it and it becomes empty for the rest.
        /// </summary>
        private string? actualText;

        /// <summary>
        /// Depth of the sequence that brought actualText, 0 when none is in effect.
        /// </summary>
        private int actualTextDepth;

        public MarkedContentTracker(OptionalContentState? documentState, OptionalContentState? visibility,
            bool useActualText, IPdfTokenScanner scanner)
        {
            this.documentState = documentState;
            this.visibility = visibility;
            this.useActualText = useActualText;
            this.scanner = scanner;
        }

        /// <summary>
        /// The optional content in effect for what is being emitted now.
        /// </summary>
        public OptionalContentCondition Current { get; private set; } = OptionalContentCondition.Always;

        /// <summary>
        /// Whether the current content is hidden in the visibility state; always false without one.
        /// </summary>
        public bool IsHidden
        {
            get
            {
                if (visibility is null || Current.IsAlways)
                {
                    return false;
                }

                if (!hiddenCache.TryGetValue(Current, out bool hidden))
                {
                    hidden = !Current.IsVisible(visibility);
                    hiddenCache[Current] = hidden;
                }

                return hidden;
            }
        }

        /// <summary>
        /// Whether an EMC would end a sequence opened by the current content stream.
        /// </summary>
        public bool CanEnd => depth > floor;

        /// <summary>
        /// A marked-content sequence begins (BMC/BDC).
        /// </summary>
        /// <param name="tag">The marked-content tag.</param>
        /// <param name="properties">The property list, already resolved from the resources when given by name.</param>
        public void Begin(NameToken tag, DictionaryToken? properties)
        {
            depth++;
            enclosing.Push(Current);

            if (properties is not null && tag.Equals(NameToken.Oc))
            {
                Current = Extend(Current, properties);
            }

            if (actualTextDepth == 0 && useActualText && properties is not null &&
                properties.TryGet(NameToken.ActualText, scanner, out IDataToken<string>? actualTextToken))
            {
                // Strip soft hyphens (U+00AD): in replacement text these are conditional hyphens marking
                // potential line-break points and are meant to be invisible when not broken. Keeping them
                // would inject invisible characters mid-word and corrupt extracted text.
                actualText = actualTextToken.Data.Replace("\u00ad", string.Empty);
                actualTextDepth = depth;
            }
        }

        /// <summary>
        /// A marked-content sequence ends (EMC).
        /// </summary>
        /// <returns><see langword="false"/> when the EMC has no matching BMC/BDC in the current content
        /// stream, in which case it is ignored.</returns>
        public bool End()
        {
            if (!CanEnd)
            {
                return false;
            }

            if (depth == actualTextDepth)
            {
                actualText = null;
                actualTextDepth = 0;
            }

            Current = enclosing.Pop();
            depth--;
            return true;
        }

        /// <summary>
        /// The text a glyph is extracted as: the replacement text in effect for the first glyph of its
        /// sequence, empty for the following ones, otherwise the glyph's own <paramref name="unicode"/>.
        /// </summary>
        public string ApplyActualText(string unicode)
        {
            if (actualText is null)
            {
                return unicode;
            }

            unicode = actualText;
            actualText = string.Empty;
            return unicode;
        }

        /// <summary>
        /// Adds the <c>/OC</c> entry of an XObject or annotation dictionary (8.11.3.3) to the current condition,
        /// until <see cref="Exit"/>. Returns false, and changes nothing, when the dictionary has no <c>/OC</c>
        /// entry or the document has no optional content; <see cref="Exit"/> must then not be called.
        /// </summary>
        public bool Enter(DictionaryToken dictionary)
        {
            if (documentState is null || !dictionary.TryGet(NameToken.Oc, scanner, out DictionaryToken? oc))
            {
                return false;
            }

            enclosing.Push(Current);
            Current = Extend(Current, oc);
            return true;
        }

        public void Exit()
        {
            Current = enclosing.Pop();
        }

        /// <summary>
        /// A content stream (the page's or a form XObject's) starts. Returns the enclosing floor, to pass to
        /// <see cref="ExitStream"/> once every sequence the stream left open has been ended.
        /// </summary>
        public int EnterStream()
        {
            int enclosingFloor = floor;
            floor = depth;
            return enclosingFloor;
        }

        /// <summary>
        /// A content stream ends.
        /// </summary>
        public void ExitStream(int enclosingFloor)
        {
            floor = enclosingFloor;
        }

        private OptionalContentCondition Extend(OptionalContentCondition parent, DictionaryToken part)
        {
            if (documentState is null)
            {
                return parent;
            }

            if (!interned.TryGetValue((parent, part), out var condition))
            {
                // Resolved now, on the processing thread: the condition is then evaluated without the scanner.
                condition = new OptionalContentCondition(parent, documentState.Resolve(part), documentState);
                interned[(parent, part)] = condition;
            }

            return condition;
        }

        // Reference identity for both halves: distinct groups can have identical dictionaries.
        private sealed class ChainComparer : IEqualityComparer<(OptionalContentCondition Parent, DictionaryToken Part)>
        {
            public static readonly ChainComparer Instance = new();

            public bool Equals((OptionalContentCondition Parent, DictionaryToken Part) x,
                (OptionalContentCondition Parent, DictionaryToken Part) y)
                => ReferenceEquals(x.Parent, y.Parent) && ReferenceEquals(x.Part, y.Part);

            public int GetHashCode((OptionalContentCondition Parent, DictionaryToken Part) obj)
                => (RuntimeHelpers.GetHashCode(obj.Parent) * 397) ^ RuntimeHelpers.GetHashCode(obj.Part);
        }
    }
}