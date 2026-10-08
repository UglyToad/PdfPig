namespace UglyToad.PdfPig.Content
{
    /// <summary>
    /// An optional content group (OCG) or membership dictionary (OCMD) resolved against the document's groups:
    /// every reference followed and every group replaced by its index, so evaluating it against an
    /// <see cref="OptionalContentState"/> reads only the state's ON/OFF data. Immutable, never touches the token
    /// scanner, and therefore safe to evaluate from any thread.
    /// <para>
    /// Built by <see cref="OptionalContentState.Resolve"/>, whose evaluation it reproduces exactly.
    /// </para>
    /// </summary>
    internal abstract class OptionalContentVisibility
    {
        /// <summary>
        /// Anything that has no effect on visibility: not optional content, an OCMD without usable groups, an
        /// invalid visibility expression.
        /// </summary>
        public static readonly OptionalContentVisibility Visible = new Constant();

        public abstract bool IsVisible(OptionalContentState state);

        /// <summary>
        /// The number of nodes evaluation visits when it cannot short-circuit: the node counted once per path to
        /// it, so a shared subexpression counts once per use. Saturates at <see cref="int.MaxValue"/>.
        /// </summary>
        public int Size { get; protected set; } = 1;

        protected static int SizeOf(params OptionalContentVisibility[] operands)
        {
            long size = 1;
            foreach (var operand in operands)
            {
                size += operand.Size;
            }

            return size > int.MaxValue ? int.MaxValue : (int)size;
        }

        private sealed class Constant : OptionalContentVisibility
        {
            public override bool IsVisible(OptionalContentState state) => true;
        }

        /// <summary>
        /// A single group, as the indices of the groups it may be: one when it was found by identity, those with
        /// the same content when it was reached through another instance (visible when any of them is), none when
        /// it is not listed in /OCGs (visible).
        /// </summary>
        public sealed class Group : OptionalContentVisibility
        {
            private readonly int[] candidates;

            public Group(int[] candidates)
            {
                this.candidates = candidates;
            }

            public override bool IsVisible(OptionalContentState state)
            {
                if (candidates.Length == 0)
                {
                    return true;
                }

                foreach (int index in candidates)
                {
                    if (state.IsEffectivelyOn(index))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public enum Policy
        {
            AnyOn,
            AllOn,
            AnyOff,
            AllOff
        }

        /// <summary>
        /// An OCMD's /P policy over its /OCGs (at least one group).
        /// </summary>
        public sealed class Membership : OptionalContentVisibility
        {
            private readonly Policy policy;
            private readonly Group[] groups;

            public Membership(Policy policy, Group[] groups)
            {
                this.policy = policy;
                this.groups = groups;
            }

            public override bool IsVisible(OptionalContentState state)
            {
                switch (policy)
                {
                    case Policy.AllOn:
                        foreach (var group in groups)
                        {
                            if (!group.IsVisible(state))
                            {
                                return false;
                            }
                        }

                        return true;

                    case Policy.AnyOff:
                        foreach (var group in groups)
                        {
                            if (!group.IsVisible(state))
                            {
                                return true;
                            }
                        }

                        return false;

                    case Policy.AllOff:
                        foreach (var group in groups)
                        {
                            if (group.IsVisible(state))
                            {
                                return false;
                            }
                        }

                        return true;

                    default:
                        foreach (var group in groups)
                        {
                            if (group.IsVisible(state))
                            {
                                return true;
                            }
                        }

                        return false;
                }
            }
        }

        /// <summary>
        /// A /VE <c>/Not</c> expression.
        /// </summary>
        public sealed class Not : OptionalContentVisibility
        {
            private readonly OptionalContentVisibility operand;

            public Not(OptionalContentVisibility operand)
            {
                this.operand = operand;
                Size = SizeOf(operand);
            }

            public override bool IsVisible(OptionalContentState state) => !operand.IsVisible(state);
        }

        /// <summary>
        /// A /VE <c>/And</c> or <c>/Or</c> expression (at least one operand).
        /// </summary>
        public sealed class Combination : OptionalContentVisibility
        {
            private readonly bool isAnd;
            private readonly OptionalContentVisibility[] operands;

            public Combination(bool isAnd, OptionalContentVisibility[] operands)
            {
                this.isAnd = isAnd;
                this.operands = operands;
                Size = SizeOf(operands);
            }

            public override bool IsVisible(OptionalContentState state)
            {
                foreach (var operand in operands)
                {
                    bool visible = operand.IsVisible(state);
                    if (isAnd && !visible)
                    {
                        return false;
                    }

                    if (!isAnd && visible)
                    {
                        return true;
                    }
                }

                return isAnd;
            }
        }
    }
}
