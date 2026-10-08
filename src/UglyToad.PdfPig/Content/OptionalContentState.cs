namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// An immutable snapshot of the on/off state of the document's optional content groups. The document's
    /// initial state is its default viewing configuration's (the <c>/D</c> entry of the catalog's
    /// <c>/OCProperties</c>, ISO 32000-2 §8.11.4); <see cref="WithGroupState"/> returns a new snapshot with a
    /// group turned on or off.
    /// </summary>
    public sealed class OptionalContentState
    {
        /// <summary>
        /// Resolution recurses through /VE expressions; bound it against malicious nesting.
        /// </summary>
        private const int MaxVisibilityExpressionDepth = 32;

        /// <summary>
        /// Evaluating a /VE visits its shared subexpressions once per use, which is exponential in the nesting for
        /// a crafted expression however compact. An expression that would visit more nodes than this is treated
        /// as malformed (visible), as at the depth limit.
        /// </summary>
        private const int MaxVisibilityExpressionSize = 1 << 16;

        /// <summary>
        /// /Order nests arrays; bound the recursion against malicious nesting.
        /// </summary>
        private const int MaxOrderDepth = 32;

        private readonly Definition _definition;

        private readonly IPdfTokenScanner _scanner;

        /// <summary>
        /// ON/OFF state per group, indexed by <see cref="OptionalContentGroup.Index"/>. Never changed
        /// after construction: a snapshot is immutable, so a page renders with one consistent state.
        /// </summary>
        private readonly bool[] _states;

        private OptionalContentState(Definition definition, bool[] states)
        {
            _definition = definition;
            _scanner = definition.Scanner;
            _states = states;
        }

        /// <summary>
        /// The document's optional content groups, in <c>/OCGs</c> order.
        /// </summary>
        public IReadOnlyList<OptionalContentGroup> Groups => _definition.Groups;

        /// <summary>
        /// The presentation tree of the groups, from the default configuration's <c>/Order</c>. Groups not
        /// listed there are not presented. When <c>/Order</c> is absent, empty or has no usable entry, every
        /// group is presented instead, as a flat list in <see cref="Groups"/> order (the specification would
        /// present none, leaving the layers unreachable).
        /// </summary>
        public IReadOnlyList<OptionalContentOrderNode> Order => _definition.Order;

        /// <summary>
        /// Whether the group is ON in this snapshot. This is the group's state only: a group whose
        /// <c>/Intent</c> does not match the configuration's has no effect on visibility whatever its
        /// state (§8.11.2.3).
        /// </summary>
        public bool IsOn(OptionalContentGroup group)
        {
            CheckOwner(group);
            return _states[group.Index];
        }

        /// <summary>
        /// A new snapshot with the group set to <paramref name="on"/>; this snapshot is unchanged.
        /// Turning a group ON turns OFF every other member of each <c>/RBGroups</c> array it belongs to
        /// (radio-button behaviour, §8.11.4.3); turning a group OFF forces no other group ON.
        /// </summary>
        public OptionalContentState WithGroupState(OptionalContentGroup group, bool on)
        {
            CheckOwner(group);

            var states = (bool[])_states.Clone();
            states[group.Index] = on;

            if (on)
            {
                foreach (int[] radioGroup in _definition.RadioGroups)
                {
                    if (Array.IndexOf(radioGroup, group.Index) < 0)
                    {
                        continue;
                    }

                    foreach (int other in radioGroup)
                    {
                        if (other != group.Index)
                        {
                            states[other] = false;
                        }
                    }
                }
            }

            return new OptionalContentState(_definition, states);
        }

        /// <summary>
        /// Whether both snapshots were built for the same document.
        /// </summary>
        internal bool IsFromSameDocument(OptionalContentState other) => ReferenceEquals(_definition, other._definition);

        private void CheckOwner(OptionalContentGroup group)
        {
            if (group is null)
            {
                throw new ArgumentNullException(nameof(group));
            }

            if (!ReferenceEquals(group.Owner, _definition))
            {
                throw new ArgumentException("The optional content group belongs to another document.", nameof(group));
            }
        }

        /// <summary>
        /// Builds the optional content state from the document catalog.
        /// </summary>
        /// <returns><c>null</c> when the document declares no optional content, in which case
        /// optional content markers are ignored and everything is drawn (§8.11.4.1).</returns>
        internal static OptionalContentState? Create(DictionaryToken? catalogDictionary, IPdfTokenScanner scanner)
        {
            if (catalogDictionary is null ||
                !catalogDictionary.TryGet(NameToken.Ocproperties, scanner, out DictionaryToken? ocProperties) ||
                !ocProperties.TryGet(NameToken.Ocgs, scanner, out ArrayToken? ocgs))
            {
                return null;
            }

            var definition = new Definition(scanner);

            foreach (var ocgToken in ocgs.Data)
            {
                if (DirectObjectFinder.TryGet(ocgToken, scanner, out DictionaryToken? ocg) &&
                    !definition.IndexByDictionary.ContainsKey(ocg))
                {
                    int index = definition.Groups.Count;
                    definition.IndexByDictionary[ocg] = index;
                    definition.Groups.Add(new OptionalContentGroup(index, GetName(ocg, scanner),
                        GetIntentNames(ocg, scanner), ocg, definition));
                }
            }

            // The /D configuration is required; without one every group keeps its default ON state.
            ocProperties.TryGet(NameToken.D, scanner, out DictionaryToken? config);

            if (config is not null && config.TryGet(NameToken.RbGroups, scanner, out ArrayToken? rbGroups))
            {
                foreach (var rbGroupToken in rbGroups.Data)
                {
                    if (!DirectObjectFinder.TryGet(rbGroupToken, scanner, out ArrayToken? rbGroup))
                    {
                        continue;
                    }

                    var members = new List<int>();
                    foreach (var memberToken in rbGroup.Data)
                    {
                        if (DirectObjectFinder.TryGet(memberToken, scanner, out DictionaryToken? member) &&
                            definition.IndexByDictionary.TryGetValue(member, out int memberIndex) &&
                            !members.Contains(memberIndex))
                        {
                            members.Add(memberIndex);
                        }
                    }

                    if (members.Count > 1)
                    {
                        definition.RadioGroups.Add(members.ToArray());
                    }
                }
            }

            bool baseState = true;
            if (config is not null &&
                config.TryGet(NameToken.BaseState, scanner, out NameToken? baseStateName) &&
                baseStateName.Equals(NameToken.Off))
            {
                baseState = false;
            }
            // 'Unchanged' is only meaningful for alternate configurations applied on top of /D; for
            // /D itself it is equivalent to the default, ON.

            var states = new bool[definition.Groups.Count];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = baseState;
            }

            if (config is not null)
            {
                // §8.11.4.5 b): only the array opposite to BaseState adjusts the states; the other is
                // redundant. A group listed in both arrays (not allowed, Table 99) therefore takes the
                // state opposite to BaseState.
                if (baseState)
                {
                    SetStates(states, definition, config, NameToken.Off, false);
                }
                else
                {
                    SetStates(states, definition, config, NameToken.On, true);
                }
            }

            // §8.11.2.3: a group whose /Intent shares nothing with the configuration's /Intent has no
            // effect on visibility. An empty configuration /Intent array matches no group, so all
            // content is visible.
            var configIntents = GetIntents(config, scanner);
            definition.IgnoredByIntent = new bool[definition.Groups.Count];
            if (!configIntents.Contains(NameToken.All))
            {
                foreach (var group in definition.Groups)
                {
                    bool intersects = false;
                    foreach (var intent in GetIntents(group.Dictionary, scanner))
                    {
                        if (intent.Equals(NameToken.All) || configIntents.Contains(intent))
                        {
                            intersects = true;
                            break;
                        }
                    }

                    definition.IgnoredByIntent[group.Index] = !intersects;
                }
            }

            definition.Order = ParseOrder(config, definition);

            definition.GroupVisibilities = new OptionalContentVisibility.Group[definition.Groups.Count];
            for (int i = 0; i < definition.GroupVisibilities.Length; i++)
            {
                definition.GroupVisibilities[i] = new OptionalContentVisibility.Group([i]);
            }

            return new OptionalContentState(definition, states);
        }

        private static void SetStates(bool[] states, Definition definition, DictionaryToken config, NameToken key, bool isOn)
        {
            if (!config.TryGet(key, definition.Scanner, out ArrayToken? groups))
            {
                return;
            }

            foreach (var ocgToken in groups.Data)
            {
                if (DirectObjectFinder.TryGet(ocgToken, definition.Scanner, out DictionaryToken? ocg) &&
                    definition.IndexByDictionary.TryGetValue(ocg, out int index))
                {
                    states[index] = isOn;
                }
            }
        }

        private static string GetName(DictionaryToken ocg, IPdfTokenScanner scanner)
        {
            return ocg.TryGet(NameToken.Name, scanner, out IDataToken<string>? name) ? name.Data : string.Empty;
        }

        private static IReadOnlyList<string> GetIntentNames(DictionaryToken ocg, IPdfTokenScanner scanner)
        {
            var names = new List<string>();
            foreach (var intent in GetIntents(ocg, scanner))
            {
                names.Add(intent.Data);
            }

            return names;
        }

        /// <summary>
        /// The /Intent of a configuration or group dictionary: a single name or an array of names,
        /// defaulting to View when absent (or not a name or array). An empty array gives an empty set.
        /// </summary>
        private static HashSet<NameToken> GetIntents(DictionaryToken? dictionary, IPdfTokenScanner scanner)
        {
            var intents = new HashSet<NameToken>();

            if (dictionary is not null && dictionary.TryGet(NameToken.Intent, scanner, out IToken? intentToken))
            {
                if (DirectObjectFinder.TryGet(intentToken, scanner, out NameToken? name))
                {
                    intents.Add(name);
                    return intents;
                }

                if (DirectObjectFinder.TryGet(intentToken, scanner, out ArrayToken? array))
                {
                    foreach (var item in array.Data)
                    {
                        if (DirectObjectFinder.TryGet(item, scanner, out NameToken? itemName))
                        {
                            intents.Add(itemName);
                        }
                    }

                    return intents;
                }
            }

            intents.Add(NameToken.View);
            return intents;
        }

        /// <summary>
        /// Whether content governed by the given optional content group (OCG) or membership
        /// dictionary (OCMD) is visible. Anything that is neither is treated as visible.
        /// </summary>
        public bool IsVisible(DictionaryToken? optionalContent)
        {
            return Resolve(optionalContent).IsVisible(this);
        }

        /// <summary>
        /// Resolves an optional content group (OCG) or membership dictionary (OCMD) into a form that is evaluated
        /// against any state of this document without reading the PDF (the token scanner is not thread-safe).
        /// The result depends only on what all snapshots of the document share, not on this snapshot's states.
        /// Anything that is neither an OCG nor an OCMD resolves to always visible.
        /// </summary>
        internal OptionalContentVisibility Resolve(DictionaryToken? optionalContent)
        {
            if (optionalContent is null)
            {
                return OptionalContentVisibility.Visible;
            }

            if (optionalContent.TryGet(NameToken.Type, _scanner, out NameToken? type) &&
                type.Equals(NameToken.Ocmd))
            {
                return ResolveMembership(optionalContent);
            }

            return ResolveGroup(optionalContent);
        }

        /// <summary>
        /// A single OCG. A group that is not listed in /OCGs is not under the document's control and stays
        /// visible.
        /// </summary>
        private OptionalContentVisibility.Group ResolveGroup(DictionaryToken ocg)
        {
            if (_definition.IndexByDictionary.TryGetValue(ocg, out int index))
            {
                return _definition.GroupVisibilities[index];
            }

            // Reached through another instance: the scanner does not cache an object it found at another
            // offset than the cross-reference table gives, so each read returns a new one. Match it by
            // content instead. It cannot be told which of several groups with the same content is meant, so
            // it is hidden only when every one of them is off. Rare, and there are few groups: a scan will do.
            var candidates = new List<int>();
            foreach (var group in _definition.Groups)
            {
                if (group.Dictionary.Equals(ocg))
                {
                    candidates.Add(group.Index);
                }
            }

            return new OptionalContentVisibility.Group(candidates.ToArray());
        }

        /// <summary>
        /// Whether the group lets its content show: ON, or ignored because of its intent.
        /// </summary>
        internal bool IsEffectivelyOn(int index) => _definition.IgnoredByIntent[index] || _states[index];

        /// <summary>
        /// What every state snapshot of a document shares: the groups and how to find them.
        /// </summary>
        private sealed class Definition
        {
            public Definition(IPdfTokenScanner scanner)
            {
                Scanner = scanner;
            }

            public IPdfTokenScanner Scanner { get; }

            public List<OptionalContentGroup> Groups { get; } = new();

            // Keyed by the resolved OCG dictionary instance. The token scanner caches non-stream objects, so
            // an OCG reached through a page's /Properties resource is the same instance as the one listed in
            // /OCGs. Identity rather than DictionaryToken's structural equality, because distinct groups can
            // have identical dictionaries (e.g. two layers named "Layer 1") and must keep their own state.
            //
            // Keying by IndirectReference instead would be simpler, as every group shall be an indirect object
            // (8.11.3.2), and would need neither the comparer nor the content fallback in IsGroupVisible. It is
            // not done because a BDC /OC property list reaches the tracker already resolved by the resource
            // store, which loses the reference: the store would have to carry it through.
            public Dictionary<DictionaryToken, int> IndexByDictionary { get; } = new(ReferenceComparer<DictionaryToken>.Instance);

            /// <summary>
            /// Per group: its intent does not match the configuration's, so it has no effect on visibility.
            /// </summary>
            public bool[] IgnoredByIntent { get; set; } = [];

            /// <summary>
            /// Per group: the resolved form of the group itself, shared by every condition that refers to it.
            /// </summary>
            public OptionalContentVisibility.Group[] GroupVisibilities { get; set; } = [];

            /// <summary>
            /// The /D /RBGroups arrays, as group indices; groups not in /OCGs are left out.
            /// </summary>
            public List<int[]> RadioGroups { get; } = new();

            public IReadOnlyList<OptionalContentOrderNode> Order { get; set; } = [];
        }

        /// <summary>
        /// An optional content membership dictionary (§8.11.2.2, Table 99): the /VE visibility expression when
        /// present, else the /P policy applied over /OCGs.
        /// </summary>
        private OptionalContentVisibility ResolveMembership(DictionaryToken ocmd)
        {
            if (ocmd.TryGet(NameToken.VE, _scanner, out ArrayToken? visibilityExpression))
            {
                // Allocated only for an OCMD that has a /VE.
                var resolved = ResolveVisibilityExpression(visibilityExpression, 0, new ExpressionMemo());
                return resolved.Size > MaxVisibilityExpressionSize ? OptionalContentVisibility.Visible : resolved;
            }

            var groups = new List<OptionalContentVisibility.Group>();
            if (ocmd.TryGet(NameToken.Ocgs, _scanner, out IToken? ocgsToken))
            {
                if (DirectObjectFinder.TryGet(ocgsToken, _scanner, out ArrayToken? ocgArray))
                {
                    foreach (var item in ocgArray.Data)
                    {
                        // Null and missing entries are ignored.
                        if (DirectObjectFinder.TryGet(item, _scanner, out DictionaryToken? ocg))
                        {
                            groups.Add(ResolveGroup(ocg));
                        }
                    }
                }
                else if (DirectObjectFinder.TryGet(ocgsToken, _scanner, out DictionaryToken? singleOcg))
                {
                    groups.Add(ResolveGroup(singleOcg));
                }
            }

            // An OCMD with no usable groups has no effect on visibility.
            if (groups.Count == 0)
            {
                return OptionalContentVisibility.Visible;
            }

            ocmd.TryGet(NameToken.P, _scanner, out NameToken? policyName);

            var policy = OptionalContentVisibility.Policy.AnyOn; // The default.
            if (policyName is not null && policyName.Equals(NameToken.AllOn))
            {
                policy = OptionalContentVisibility.Policy.AllOn;
            }
            else if (policyName is not null && policyName.Equals(NameToken.AnyOff))
            {
                policy = OptionalContentVisibility.Policy.AnyOff;
            }
            else if (policyName is not null && policyName.Equals(NameToken.AllOff))
            {
                policy = OptionalContentVisibility.Policy.AllOff;
            }

            return new OptionalContentVisibility.Membership(policy, groups.ToArray());
        }

        /// <summary>
        /// A visibility expression: <c>[/And e1 e2 ...]</c>, <c>[/Or e1 e2 ...]</c> or <c>[/Not e]</c>, where each
        /// operand is an OCG or a nested expression. An invalid expression is visible.
        /// </summary>
        private OptionalContentVisibility ResolveVisibilityExpression(ArrayToken expression, int depth, ExpressionMemo memo)
        {
            if (depth > MaxVisibilityExpressionDepth || expression.Data.Count < 2 ||
                !DirectObjectFinder.TryGet(expression.Data[0], _scanner, out NameToken? op))
            {
                return OptionalContentVisibility.Visible;
            }

            // An expression reached again while it is being resolved is a cycle. The back-reference counts as
            // visible, as at the depth limit, and the expression around it is evaluated normally: a self-cycle
            // [/Not 5 0 R] resolves to Not(visible), hence hidden.
            if (memo.InProgress.Contains(expression))
            {
                return OptionalContentVisibility.Visible;
            }

            // A shared subexpression is resolved once. The depth is part of the key because the depth limit
            // truncates a subexpression differently at each depth.
            if (memo.Resolved.TryGetValue((expression, depth), out var known))
            {
                return known;
            }

            memo.InProgress.Add(expression);
            try
            {
                var resolved = ResolveVisibilityExpressionBody(expression, op, depth, memo);
                memo.Resolved[(expression, depth)] = resolved;
                return resolved;
            }
            finally
            {
                memo.InProgress.Remove(expression);
            }
        }

        private OptionalContentVisibility ResolveVisibilityExpressionBody(ArrayToken expression, NameToken op, int depth, ExpressionMemo memo)
        {
            if (op.Equals(NameToken.Not))
            {
                return new OptionalContentVisibility.Not(ResolveOperand(expression.Data[1], depth, memo));
            }

            bool isAnd = op.Equals(NameToken.And);
            if (!isAnd && !op.Equals(NameToken.Or))
            {
                return OptionalContentVisibility.Visible;
            }

            // And and Or are idempotent: an operand that is the same node as an earlier one adds nothing. Dropping
            // it keeps evaluation linear when operands share a subexpression, which evaluation cannot
            // short-circuit (e.g. [/And X X] repeated down many levels).
            var operands = new List<OptionalContentVisibility>(expression.Data.Count - 1);
            var seen = new HashSet<OptionalContentVisibility>(ReferenceComparer<OptionalContentVisibility>.Instance);
            for (int i = 1; i < expression.Data.Count; ++i)
            {
                var operand = ResolveOperand(expression.Data[i], depth, memo);
                if (seen.Add(operand))
                {
                    operands.Add(operand);
                }
            }

            return new OptionalContentVisibility.Combination(isAnd, operands.ToArray());
        }

        private OptionalContentVisibility ResolveOperand(IToken operand, int depth, ExpressionMemo memo)
        {
            if (DirectObjectFinder.TryGet(operand, _scanner, out ArrayToken? nested))
            {
                return ResolveVisibilityExpression(nested, depth + 1, memo);
            }

            if (DirectObjectFinder.TryGet(operand, _scanner, out DictionaryToken? ocg))
            {
                return ResolveGroup(ocg);
            }

            return OptionalContentVisibility.Visible;
        }

        private static IReadOnlyList<OptionalContentOrderNode> ParseOrder(DictionaryToken? config, Definition definition)
        {
            if (config is not null && config.TryGet(NameToken.Order, definition.Scanner, out ArrayToken? order))
            {
                var nodes = ParseOrderArray(order.Data, definition, 0);
                if (nodes.Count > 0)
                {
                    return nodes;
                }
            }

            var flat = new List<OptionalContentOrderNode>(definition.Groups.Count);
            foreach (var group in definition.Groups)
            {
                flat.Add(new OptionalContentOrderNode(group, null, []));
            }

            return flat;
        }

        private static List<OptionalContentOrderNode> ParseOrderArray(IReadOnlyList<IToken> items, Definition definition, int depth)
        {
            var nodes = new List<OptionalContentOrderNode>();
            if (depth > MaxOrderDepth)
            {
                return nodes;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (DirectObjectFinder.TryGet(items[i], definition.Scanner, out DictionaryToken? ocg))
                {
                    if (!definition.IndexByDictionary.TryGetValue(ocg, out int index))
                    {
                        // Not a group listed in /OCGs.
                        continue;
                    }

                    // A nested array right after a group, without a label, holds the group's sublayers.
                    var children = new List<OptionalContentOrderNode>();
                    if (i + 1 < items.Count &&
                        DirectObjectFinder.TryGet(items[i + 1], definition.Scanner, out ArrayToken? sublayers) &&
                        GetLabel(sublayers, definition.Scanner) is null)
                    {
                        children = ParseOrderArray(sublayers.Data, definition, depth + 1);
                        i++;
                    }

                    nodes.Add(new OptionalContentOrderNode(definition.Groups[index], null, children));
                }
                else if (DirectObjectFinder.TryGet(items[i], definition.Scanner, out ArrayToken? collection))
                {
                    string? label = GetLabel(collection, definition.Scanner);
                    var entries = new List<IToken>(collection.Data);
                    if (label is not null)
                    {
                        entries.RemoveAt(0);
                    }

                    var children = ParseOrderArray(entries, definition, depth + 1);
                    if (children.Count > 0)
                    {
                        nodes.Add(new OptionalContentOrderNode(null, label, children));
                    }
                }
            }

            return nodes;
        }

        /// <summary>
        /// The text label of an /Order collection: its first element when that is a text string
        /// (literal or hexadecimal). A name is not a label.
        /// </summary>
        private static string? GetLabel(ArrayToken collection, IPdfTokenScanner scanner)
        {
            if (collection.Data.Count == 0)
            {
                return null;
            }

            if (DirectObjectFinder.TryGet(collection.Data[0], scanner, out StringToken? text))
            {
                return text.Data;
            }

            return DirectObjectFinder.TryGet(collection.Data[0], scanner, out HexToken? hex) ? hex.Data : null;
        }

        // System.Collections.Generic.ReferenceEqualityComparer is not available on every target framework.
        private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
        {
            public static readonly ReferenceComparer<T> Instance = new();

            public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

            public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
        }

        /// <summary>
        /// Bookkeeping for resolving one /VE: the scanner returns the same <see cref="ArrayToken"/> instance for
        /// every reference to a shared subexpression, so identity finds them. Without it an expression whose
        /// operands share a subexpression, or refer back to themselves, is expanded once per path through it.
        /// </summary>
        private sealed class ExpressionMemo
        {
            public HashSet<ArrayToken> InProgress { get; } = new(ReferenceComparer<ArrayToken>.Instance);

            public Dictionary<(ArrayToken Expression, int Depth), OptionalContentVisibility> Resolved { get; } = new(ExpressionKeyComparer.Instance);
        }

        private sealed class ExpressionKeyComparer : IEqualityComparer<(ArrayToken Expression, int Depth)>
        {
            public static readonly ExpressionKeyComparer Instance = new();

            public bool Equals((ArrayToken Expression, int Depth) x, (ArrayToken Expression, int Depth) y)
                => ReferenceEquals(x.Expression, y.Expression) && x.Depth == y.Depth;

            public int GetHashCode((ArrayToken Expression, int Depth) obj)
                => (RuntimeHelpers.GetHashCode(obj.Expression) * 397) ^ obj.Depth;
        }
    }
}