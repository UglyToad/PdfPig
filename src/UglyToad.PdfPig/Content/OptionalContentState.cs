namespace UglyToad.PdfPig.Content
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// The on/off state of the document's optional content groups, as set by the default viewing
    /// configuration (the <c>/D</c> entry of the catalog's <c>/OCProperties</c>, ISO 32000-2 §8.11.4).
    /// </summary>
    public sealed class OptionalContentState
    {
        /// <summary>
        /// Visibility evaluation recurses through /VE expressions; bound it against malicious nesting.
        /// </summary>
        private const int MaxVisibilityExpressionDepth = 32;

        private readonly IPdfTokenScanner _scanner;

        // Keyed by the resolved OCG dictionary instance. The token scanner caches non-stream objects, so
        // an OCG reached through a page's /Properties resource is the same instance as the one listed in
        // /OCGs. Identity rather than DictionaryToken's structural equality, because distinct groups can
        // have identical dictionaries (e.g. two layers named "Layer 1") and must keep their own state.
        //
        // Keying by IndirectReference instead would be simpler, as every group shall be an indirect object
        // (8.11.3.2), and would need neither the comparer nor the content fallback in IsGroupVisible. It is
        // not done because a BDC /OC property list reaches the tracker already resolved by the resource
        // store, which loses the reference: the store would have to carry it through.
        private readonly Dictionary<DictionaryToken, bool> _groupStates = new(ReferenceComparer.Instance);

        private OptionalContentState(IPdfTokenScanner scanner)
        {
            _scanner = scanner;
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

            var state = new OptionalContentState(scanner);

            // The /D configuration is required; without one every group keeps its default ON state.
            ocProperties.TryGet(NameToken.D, scanner, out DictionaryToken? config);

            bool baseState = true;
            if (config is not null &&
                config.TryGet(NameToken.BaseState, scanner, out NameToken? baseStateName) &&
                baseStateName.Equals(NameToken.Off))
            {
                baseState = false;
            }
            // 'Unchanged' is only meaningful for alternate configurations applied on top of /D; for
            // /D itself it is equivalent to the default, ON.

            var configIntents = GetIntents(config, scanner);

            foreach (var ocgToken in ocgs.Data)
            {
                if (DirectObjectFinder.TryGet(ocgToken, scanner, out DictionaryToken? ocg))
                {
                    state._groupStates[ocg] = baseState;
                }
            }

            if (config is not null)
            {
                // §8.11.4.5 b): only the array opposite to BaseState adjusts the states; the other is
                // redundant. A group listed in both arrays (not allowed, Table 99) therefore takes the
                // state opposite to BaseState.
                if (baseState)
                {
                    state.SetStates(config, NameToken.Off, false);
                }
                else
                {
                    state.SetStates(config, NameToken.On, true);
                }
            }

            // §8.11.2.3: a group whose /Intent shares nothing with the configuration's /Intent has no
            // effect on visibility. An empty configuration /Intent array matches no group, so all
            // content is visible.
            if (!configIntents.Contains(NameToken.All))
            {
                foreach (var ocg in new List<DictionaryToken>(state._groupStates.Keys))
                {
                    bool intersects = false;
                    foreach (var intent in GetIntents(ocg, scanner))
                    {
                        if (intent.Equals(NameToken.All) || configIntents.Contains(intent))
                        {
                            intersects = true;
                            break;
                        }
                    }

                    if (!intersects)
                    {
                        state._groupStates[ocg] = true;
                    }
                }
            }

            return state;
        }

        private void SetStates(DictionaryToken config, NameToken key, bool isOn)
        {
            if (!config.TryGet(key, _scanner, out ArrayToken? groups))
            {
                return;
            }

            foreach (var ocgToken in groups.Data)
            {
                if (DirectObjectFinder.TryGet(ocgToken, _scanner, out DictionaryToken? ocg))
                {
                    _groupStates[ocg] = isOn;
                }
            }
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
            if (optionalContent is null)
            {
                return true;
            }

            if (optionalContent.TryGet(NameToken.Type, _scanner, out NameToken? type) &&
                type.Equals(NameToken.Ocmd))
            {
                return IsMembershipVisible(optionalContent);
            }

            return IsGroupVisible(optionalContent);
        }

        /// <summary>
        /// Visibility of a single OCG. A group that is not listed in /OCGs is not under the
        /// document's control and stays visible.
        /// </summary>
        private bool IsGroupVisible(DictionaryToken ocg)
        {
            if (_groupStates.TryGetValue(ocg, out bool isOn))
            {
                return isOn;
            }

            // Reached through another instance: the scanner does not cache an object it found at another
            // offset than the cross-reference table gives, so each read returns a new one. Match it by
            // content instead. It cannot be told which of several groups with the same content is meant, so
            // it is hidden only when every one of them is off. Rare, and there are few groups: a scan will do.
            bool listed = false;
            foreach (var pair in _groupStates)
            {
                if (pair.Key.Equals(ocg))
                {
                    if (pair.Value)
                    {
                        return true;
                    }

                    listed = true;
                }
            }

            return !listed;
        }

        /// <summary>
        /// Visibility of an optional content membership dictionary (§8.11.2.2, Table 99): the /VE
        /// visibility expression when present, else the /P policy applied over /OCGs.
        /// </summary>
        private bool IsMembershipVisible(DictionaryToken ocmd)
        {
            if (ocmd.TryGet(NameToken.VE, _scanner, out ArrayToken? visibilityExpression))
            {
                return EvaluateVisibilityExpression(visibilityExpression, 0);
            }

            var groups = new List<DictionaryToken>();
            if (ocmd.TryGet(NameToken.Ocgs, _scanner, out IToken? ocgsToken))
            {
                if (DirectObjectFinder.TryGet(ocgsToken, _scanner, out ArrayToken? ocgArray))
                {
                    foreach (var item in ocgArray.Data)
                    {
                        // Null and missing entries are ignored.
                        if (DirectObjectFinder.TryGet(item, _scanner, out DictionaryToken? ocg))
                        {
                            groups.Add(ocg);
                        }
                    }
                }
                else if (DirectObjectFinder.TryGet(ocgsToken, _scanner, out DictionaryToken? singleOcg))
                {
                    groups.Add(singleOcg);
                }
            }

            // An OCMD with no usable groups has no effect on visibility.
            if (groups.Count == 0)
            {
                return true;
            }

            ocmd.TryGet(NameToken.P, _scanner, out NameToken? policy);

            if (policy is not null && policy.Equals(NameToken.AllOn))
            {
                foreach (var ocg in groups)
                {
                    if (!IsGroupVisible(ocg))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (policy is not null && policy.Equals(NameToken.AnyOff))
            {
                foreach (var ocg in groups)
                {
                    if (!IsGroupVisible(ocg))
                    {
                        return true;
                    }
                }

                return false;
            }

            if (policy is not null && policy.Equals(NameToken.AllOff))
            {
                foreach (var ocg in groups)
                {
                    if (IsGroupVisible(ocg))
                    {
                        return false;
                    }
                }

                return true;
            }

            // AnyOn, the default.
            foreach (var ocg in groups)
            {
                if (IsGroupVisible(ocg))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Evaluates a visibility expression: <c>[/And e1 e2 ...]</c>, <c>[/Or e1 e2 ...]</c> or
        /// <c>[/Not e]</c>, where each operand is an OCG or a nested expression.
        /// </summary>
        private bool EvaluateVisibilityExpression(ArrayToken expression, int depth)
        {
            if (depth > MaxVisibilityExpressionDepth || expression.Data.Count < 2 ||
                !DirectObjectFinder.TryGet(expression.Data[0], _scanner, out NameToken? op))
            {
                return true;
            }

            if (op.Equals(NameToken.Not))
            {
                return !EvaluateOperand(expression.Data[1], depth);
            }

            bool isAnd = op.Equals(NameToken.And);
            if (!isAnd && !op.Equals(NameToken.Or))
            {
                return true;
            }

            for (int i = 1; i < expression.Data.Count; ++i)
            {
                bool operand = EvaluateOperand(expression.Data[i], depth);
                if (isAnd && !operand)
                {
                    return false;
                }

                if (!isAnd && operand)
                {
                    return true;
                }
            }

            return isAnd;
        }

        private bool EvaluateOperand(IToken operand, int depth)
        {
            if (DirectObjectFinder.TryGet(operand, _scanner, out ArrayToken? nested))
            {
                return EvaluateVisibilityExpression(nested, depth + 1);
            }

            if (DirectObjectFinder.TryGet(operand, _scanner, out DictionaryToken? ocg))
            {
                return IsGroupVisible(ocg);
            }

            return true;
        }

        // System.Collections.Generic.ReferenceEqualityComparer is not available on every target framework.
        private sealed class ReferenceComparer : IEqualityComparer<DictionaryToken>
        {
            public static readonly ReferenceComparer Instance = new();

            public bool Equals(DictionaryToken? x, DictionaryToken? y) => ReferenceEquals(x, y);

            public int GetHashCode(DictionaryToken obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}