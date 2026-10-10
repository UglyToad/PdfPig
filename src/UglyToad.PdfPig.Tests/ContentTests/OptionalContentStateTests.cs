namespace UglyToad.PdfPig.Tests.ContentTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.Graphics;
    using PdfPig.Tokens;
    using PdfPig.Tests.Tokens;
    using Xunit;

    /// <summary>
    /// Covers <see cref="OptionalContentState"/>: the default configuration (8.11.4.3) and the visibility
    /// of optional content groups and membership dictionaries (8.11.2).
    /// </summary>
    public class OptionalContentStateTests
    {
        private readonly TestPdfTokenScanner scanner = new TestPdfTokenScanner();

        private readonly IndirectReferenceToken on;
        private readonly IndirectReferenceToken off;

        public OptionalContentStateTests()
        {
            on = AddGroup(1, "On");
            off = AddGroup(2, "Off");
        }

        [Fact]
        public void NoOptionalContentPropertiesGivesNoState()
        {
            Assert.Null(OptionalContentState.Create(Dictionary(("Type", NameToken.Catalog)), scanner));
            Assert.Null(OptionalContentState.Create(null, scanner));
        }

        [Fact]
        public void BaseStateOnHidesOnlyOffGroups()
        {
            var state = Create(Dictionary(("OFF", Array(off))));

            Assert.True(state.IsVisible(Resolve(on)));
            Assert.False(state.IsVisible(Resolve(off)));
        }

        [Fact]
        public void BaseStateOffShowsOnlyOnGroups()
        {
            var state = Create(Dictionary(("BaseState", NameToken.Off), ("ON", Array(on))));

            Assert.True(state.IsVisible(Resolve(on)));
            Assert.False(state.IsVisible(Resolve(off)));
        }

        [Fact]
        public void GroupInBothArraysTakesTheStateOppositeToBaseState()
        {
            // Not allowed (Table 99), but 8.11.4.5 b) only applies the array opposite to BaseState.
            var baseOn = Create(Dictionary(("ON", Array(off)), ("OFF", Array(off))));
            Assert.False(baseOn.IsVisible(Resolve(off)));

            var baseOff = Create(Dictionary(("BaseState", NameToken.Off), ("ON", Array(on)), ("OFF", Array(on))));
            Assert.True(baseOff.IsVisible(Resolve(on)));
        }

        [Fact]
        public void BaseStateOnIgnoresOnArray()
        {
            var state = Create(Dictionary(("ON", Array(on))));

            Assert.True(state.IsVisible(Resolve(on)));
            Assert.True(state.IsVisible(Resolve(off)));
        }

        [Fact]
        public void MissingDefaultConfigurationShowsEverything()
        {
            var catalog = Dictionary(("OCProperties", Dictionary(("OCGs", Array(on, off)))));
            var state = OptionalContentState.Create(catalog, scanner)!;

            Assert.True(state.IsVisible(Resolve(off)));
        }

        [Fact]
        public void GroupNotListedInOcgsIsVisible()
        {
            var unlisted = AddGroup(3, "Unlisted");
            var state = Create(Dictionary(("BaseState", NameToken.Off)));

            Assert.True(state.IsVisible(Resolve(unlisted)));
        }

        [Fact]
        public void GroupWithIntentOutsideConfigurationIsIgnored()
        {
            var design = AddGroup(3, "Design only", NameToken.Create("Design"));
            var state = Create(Dictionary(("OFF", Array(off, design))), on, off, design);

            // The configuration's intent defaults to View, so a Design-only group cannot hide content...
            Assert.True(state.IsVisible(Resolve(design)));
            Assert.False(state.IsVisible(Resolve(off)));

            // ...unless the configuration asks for every intent.
            var all = Create(Dictionary(("OFF", Array(off, design)), ("Intent", NameToken.All)), on, off, design);
            Assert.False(all.IsVisible(Resolve(design)));
        }

        [Fact]
        public void EmptyConfigurationIntentShowsEverything()
        {
            // 8.11.2.3: no groups are used in determining visibility, so all content is visible.
            var state = Create(Dictionary(("OFF", Array(off)), ("Intent", Array())));

            Assert.True(state.IsVisible(Resolve(off)));
            Assert.True(state.IsVisible(Membership(("OCGs", Array(off)), ("P", NameToken.AllOn))));
        }

        [Fact]
        public void GroupWithEmptyIntentIsIgnored()
        {
            var noIntent = AddGroup(3, "No intent", Array());
            var state = Create(Dictionary(("OFF", Array(off, noIntent))), on, off, noIntent);

            Assert.True(state.IsVisible(Resolve(noIntent)));
            Assert.False(state.IsVisible(Resolve(off)));
        }

        [Fact]
        public void GroupsWithIdenticalDictionariesKeepTheirOwnState()
        {
            // Distinct objects with the same content, e.g. two layers both named "Off".
            var twin = AddGroup(3, "Off");
            var state = Create(Dictionary(("OFF", Array(off))), on, off, twin);

            Assert.False(state.IsVisible(Resolve(off)));
            Assert.True(state.IsVisible(Resolve(twin)));
        }

        [Fact]
        public void GroupReachedThroughAnotherInstanceFallsBackToItsContent()
        {
            // As when the scanner does not cache an object found at another offset than the
            // cross-reference table gives, so each read returns a new instance.
            var state = Create(Dictionary(("OFF", Array(off))));
            Assert.False(state.IsVisible(Copy(off)));
            Assert.True(state.IsVisible(Copy(on)));

            // No listed group has that content: not under the document's control, so visible.
            Assert.True(state.IsVisible(Dictionary(("Type", NameToken.Ocg), ("Name", new StringToken("Unlisted")))));

            // Groups with that content disagree: which one is meant is unknown, so stay visible.
            var twin = AddGroup(3, "Off");
            var ambiguous = Create(Dictionary(("OFF", Array(off))), on, off, twin);
            Assert.True(ambiguous.IsVisible(Copy(off)));

            // Groups with that content are all off: hidden whichever one is meant.
            var bothOff = Create(Dictionary(("OFF", Array(off, twin))), on, off, twin);
            Assert.False(bothOff.IsVisible(Copy(off)));
        }

        [Fact]
        public void NullAndNonOptionalContentDictionariesAreVisible()
        {
            var state = Create(Dictionary(("OFF", Array(off))));

            Assert.True(state.IsVisible(null));
            Assert.True(state.IsVisible(Dictionary(("Type", NameToken.Font))));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("AnyOn", true)]
        [InlineData("AllOn", false)]
        [InlineData("AnyOff", true)]
        [InlineData("AllOff", false)]
        public void MembershipPolicyOverOneOnAndOneOffGroup(string? policy, bool expected)
        {
            var state = Create(Dictionary(("OFF", Array(off))));

            var ocmd = Membership(("OCGs", Array(on, off)));
            if (policy is not null)
            {
                ocmd = Membership(("OCGs", Array(on, off)), ("P", NameToken.Create(policy)));
            }

            Assert.Equal(expected, state.IsVisible(ocmd));
        }

        [Theory]
        [InlineData("AnyOn", false)]
        [InlineData("AllOn", false)]
        [InlineData("AnyOff", true)]
        [InlineData("AllOff", true)]
        public void MembershipPolicyOverOffGroupsOnly(string policy, bool expected)
        {
            var state = Create(Dictionary(("OFF", Array(off))));

            Assert.Equal(expected, state.IsVisible(Membership(("OCGs", Array(off)), ("P", NameToken.Create(policy)))));
        }

        [Fact]
        public void MembershipWithSingleGroupDictionary()
        {
            var state = Create(Dictionary(("OFF", Array(off))));

            Assert.False(state.IsVisible(Membership(("OCGs", off))));
            Assert.True(state.IsVisible(Membership(("OCGs", on))));
        }

        [Fact]
        public void MembershipWithoutUsableGroupsIsVisible()
        {
            var state = Create(Dictionary(("OFF", Array(off))));

            Assert.True(state.IsVisible(Membership()));
            Assert.True(state.IsVisible(Membership(("OCGs", Array(NullToken.Instance)), ("P", NameToken.Create("AllOn")))));
        }

        [Fact]
        public void VisibilityExpressionTakesPrecedenceOverPolicy()
        {
            var state = Create(Dictionary(("OFF", Array(off))));

            // AllOn over 'on' alone would be visible: /VE must win.
            var ocmd = Membership(
                ("OCGs", Array(on)),
                ("P", NameToken.Create("AllOn")),
                ("VE", Array(NameToken.Create("Not"), on)));

            Assert.False(state.IsVisible(ocmd));
        }

        [Fact]
        public void VisibilityExpressions()
        {
            var state = Create(Dictionary(("OFF", Array(off))));
            var and = NameToken.Create("And");
            var or = NameToken.Create("Or");
            var not = NameToken.Create("Not");

            Assert.True(state.IsVisible(Membership(("VE", Array(and, on, Array(not, off))))));
            Assert.False(state.IsVisible(Membership(("VE", Array(and, on, off)))));
            Assert.True(state.IsVisible(Membership(("VE", Array(or, off, on)))));
            Assert.False(state.IsVisible(Membership(("VE", Array(or, off, Array(not, on))))));
            Assert.True(state.IsVisible(Membership(("VE", Array(not, off)))));
        }

        [Fact]
        public void SelfReferentialOrExpressionResolvesQuicklyAsVisible()
        {
            var state = Create(Dictionary(("OFF", Array(off))));
            var ocmd = Membership(("VE", AddSelfReferencingExpression(5, "Or")));

            Assert.True(CompletesInTime(() => state.IsVisible(ocmd)));
        }

        [Fact]
        public void SelfReferentialAndExpressionResolvesQuicklyAsVisible()
        {
            var state = Create(Dictionary(("OFF", Array(off))));
            var ocmd = Membership(("VE", AddSelfReferencingExpression(5, "And")));

            Assert.True(CompletesInTime(() => state.IsVisible(ocmd)));
        }

        [Fact]
        public void SharedVisibilityExpressionSubtreesResolveAndEvaluateInTime()
        {
            // 30 levels (the nesting limit is 32), each [/And next next] with both operands the same indirect array: 2^30 paths if every
            // operand were expanded again.
            const int levels = 30;
            var and = NameToken.Create("And");
            for (int i = 0; i < levels; i++)
            {
                IToken next = i == levels - 1 ? on : new IndirectReferenceToken(new IndirectReference(100 + i + 1, 0));
                var reference = new IndirectReference(100 + i, 0);
                scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference, Array(and, next, next));
            }

            var ocmd = Membership(("VE", new IndirectReferenceToken(new IndirectReference(100, 0))));

            var stateOn = Create(Dictionary(("OFF", Array(off))));
            Assert.True(CompletesInTime(() => stateOn.IsVisible(ocmd)));

            var stateOff = Create(Dictionary(("OFF", Array(on))));
            Assert.False(CompletesInTime(() => stateOff.IsVisible(ocmd)));
        }

        [Fact]
        public void DistinctButEquivalentSharedSubtreesEvaluateInTimeAsVisible()
        {
            // Two distinct indirect arrays per level, each referring to both arrays of the next level: identity
            // de-duplication cannot collapse them, and walking the DAG as a tree visits 2^33 nodes.
            const int levels = 33;
            var and = NameToken.Create("And");
            for (int i = 0; i < levels; i++)
            {
                for (int j = 1; j <= 2; j++)
                {
                    IToken first = i == levels - 1 ? on : new IndirectReferenceToken(new IndirectReference(200 + 2 * (i + 1), 0));
                    IToken second = i == levels - 1 ? on : new IndirectReferenceToken(new IndirectReference(200 + 2 * (i + 1) + 1, 0));
                    var reference = new IndirectReference(200 + 2 * i + (j - 1), 0);
                    scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference, Array(and, first, second));
                }
            }

            var ocmd = Membership(("VE", new IndirectReferenceToken(new IndirectReference(200, 0))));
            var state = Create(Dictionary(("OFF", Array(off))));

            Assert.True(CompletesInTime(() => state.IsVisible(ocmd)));
        }

        [Fact]
        public void SharedExpressionReachedAtDifferentDepthsIsTruncatedAtEach()
        {
            // X = [/And M], M = [/Not on] is cut by the depth limit (32) when reached at depth 32 (M at 33, so
            // X is visible) but not at 31 (Not(on), hidden). Both are reached from one /And: the shared X must
            // not take the result of whichever depth was resolved first.
            var and = NameToken.Create("And");
            var xRef = new IndirectReference(300, 0);
            var mRef = new IndirectReference(301, 0);
            scanner.Objects[mRef] = new ObjectToken(XrefLocation.File(0), mRef, Array(NameToken.Create("Not"), on));
            scanner.Objects[xRef] = new ObjectToken(XrefLocation.File(0), xRef, Array(and, new IndirectReferenceToken(mRef)));
            IToken x = new IndirectReferenceToken(xRef);

            // Wrappers [/And next] add one level each; the chain's last wrapper holds X.
            static IToken Chain(IToken inner, int wrappers)
            {
                for (int i = 0; i < wrappers; i++)
                {
                    inner = Array(NameToken.Create("And"), inner);
                }

                return inner;
            }

            var state = Create(Dictionary(("OFF", Array(off))));
            // Root at depth 0: the chain of w wrappers puts X at depth w + 1.
            var ocmd = Membership(("VE", Array(and, Chain(x, 31), Chain(x, 30))));

            Assert.False(state.IsVisible(ocmd));
        }

        private ArrayToken AddSelfReferencingExpression(long number, string op)
        {
            var reference = new IndirectReference(number, 0);
            var self = new IndirectReferenceToken(reference);
            var expression = Array(NameToken.Create(op), self, self);
            scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference, expression);
            return expression;
        }

        private static bool CompletesInTime(Func<bool> evaluate)
        {
            // When the guard fires the background work keeps running (it cannot be cancelled), which is
            // acceptable for a failing run only.
            var task = System.Threading.Tasks.Task.Run(evaluate);
            Assert.True(task.Wait(TimeSpan.FromSeconds(5)), "Resolving the visibility expression did not complete.");
            return task.Result;
        }

        [Fact]
        public void ConditionsEvaluateLikeIsVisibleWithoutTheScanner()
        {
            // A condition resolves its dictionary when it is created, then must agree with IsVisible in every
            // state without reading the PDF again.
            var design = AddGroup(3, "Design only", NameToken.Create("Design"));
            var twin = AddGroup(4, "Off");
            var state = Create(Dictionary(("OFF", Array(off))), on, off, design, twin);
            var and = NameToken.Create("And");
            var or = NameToken.Create("Or");
            var not = NameToken.Create("Not");

            var dictionaries = new List<DictionaryToken>
            {
                Resolve(on),
                Resolve(off),
                Resolve(design),
                Copy(off),
                Dictionary(("Type", NameToken.Font)),
                Membership(),
                Membership(("OCGs", Array(NullToken.Instance)), ("P", NameToken.Create("AllOn"))),
                Membership(("OCGs", off)),
                Membership(("OCGs", Copy(off))),
                Membership(("OCGs", Array(on, off, design)), ("P", NameToken.Create("AllOn"))),
                Membership(("VE", Array(not, on)), ("OCGs", Array(on)), ("P", NameToken.Create("AllOn"))),
                Membership(("VE", Array(and, on, Array(or, off, Array(not, twin))))),
                Membership(("VE", Array(or, Array(and, off, design), Array(not, Array(or, on, twin))))),
                Membership(("VE", Array(and, on, NameToken.Create("Junk"), Array(and)))),
                Membership(("VE", Array(NameToken.Create("Xor"), on, off))),
                Membership(("VE", Array(not)))
            };

            foreach (string policy in new[] { "AnyOn", "AllOn", "AnyOff", "AllOff", "Unknown" })
            {
                dictionaries.Add(Membership(("OCGs", Array(on, off, twin)), ("P", NameToken.Create(policy))));
                dictionaries.Add(Membership(("OCGs", Array(off, twin)), ("P", NameToken.Create(policy))));
            }

            // Every combination of group states.
            var states = new List<OptionalContentState>();
            for (int mask = 0; mask < 1 << state.Groups.Count; mask++)
            {
                var combination = state;
                foreach (var group in state.Groups)
                {
                    combination = combination.WithGroupState(group, (mask & (1 << group.Index)) != 0);
                }

                states.Add(combination);
            }

            var tracker = new MarkedContentTracker(state, null, false, scanner);
            var conditions = new List<OptionalContentCondition>();
            var expected = new List<bool[]>();
            foreach (var dictionary in dictionaries)
            {
                tracker.Begin(NameToken.Oc, dictionary);
                conditions.Add(tracker.Current);
                tracker.End();
                expected.Add(states.Select(s => s.IsVisible(dictionary)).ToArray());
            }

            int reads = scanner.GetCallCount;

            for (int i = 0; i < conditions.Count; i++)
            {
                Assert.Equal(expected[i], states.Select(s => conditions[i].IsVisible(s)).ToArray());
            }

            Assert.Equal(reads, scanner.GetCallCount);
        }

        [Fact]
        public void GroupsAreListedInOcgsOrderWithNameAndIntents()
        {
            var design = AddGroup(3, "Design only", NameToken.Create("Design"));
            var state = Create(Dictionary(("OFF", Array(off))), on, off, design, on);

            // Listed once each, in /OCGs order, even when /OCGs repeats a group.
            Assert.Equal(new[] { "On", "Off", "Design only" }, state.Groups.Select(g => g.Name));
            Assert.Equal(new[] { "View" }, state.Groups[0].Intents);
            Assert.Equal(new[] { "Design" }, state.Groups[2].Intents);
        }

        [Fact]
        public void IsOnIsTheRawStateEvenForAGroupIgnoredByIntent()
        {
            var design = AddGroup(3, "Design only", NameToken.Create("Design"));
            var state = Create(Dictionary(("OFF", Array(off, design))), on, off, design);

            Assert.True(state.IsOn(state.Groups[0]));
            Assert.False(state.IsOn(state.Groups[1]));

            // OFF, but its intent does not match /D's: it has no effect on visibility.
            Assert.False(state.IsOn(state.Groups[2]));
            Assert.True(state.IsVisible(Resolve(design)));
        }

        [Fact]
        public void IsOnRejectsAGroupOfAnotherDocument()
        {
            var first = Create(Dictionary(("OFF", Array(off))));
            var second = Create(Dictionary(("OFF", Array(off))));

            Assert.Throws<ArgumentException>(() => first.IsOn(second.Groups[0]));
        }

        [Fact]
        public void WithGroupStateReturnsANewSnapshotAndLeavesTheOriginalUnchanged()
        {
            var state = Create(Dictionary(("OFF", Array(off))));
            var offGroup = state.Groups[1];

            var turnedOn = state.WithGroupState(offGroup, true);

            Assert.True(turnedOn.IsOn(offGroup));
            Assert.True(turnedOn.IsVisible(Resolve(off)));
            Assert.False(state.IsOn(offGroup));
            Assert.False(state.IsVisible(Resolve(off)));
        }

        [Fact]
        public void TurningOnARadioGroupMemberTurnsTheOthersOff()
        {
            var third = AddGroup(3, "Third");
            var config = Dictionary(("OFF", Array(off, third)), ("RBGroups", Array(Array(on, off, third))));
            var state = Create(config, on, off, third);

            var switched = state.WithGroupState(state.Groups[1], true);

            Assert.False(switched.IsOn(state.Groups[0]));
            Assert.True(switched.IsOn(state.Groups[1]));
            Assert.False(switched.IsOn(state.Groups[2]));
        }

        [Fact]
        public void TurningOffARadioGroupMemberForcesNothingOn()
        {
            var config = Dictionary(("OFF", Array(off)), ("RBGroups", Array(Array(on, off))));
            var state = Create(config);

            var allOff = state.WithGroupState(state.Groups[0], false);

            Assert.False(allOff.IsOn(state.Groups[0]));
            Assert.False(allOff.IsOn(state.Groups[1]));
        }

        [Fact]
        public void WithGroupStateRejectsAGroupOfAnotherDocument()
        {
            var first = Create(Dictionary(("OFF", Array(off))));
            var second = Create(Dictionary(("OFF", Array(off))));

            Assert.Throws<ArgumentException>(() => first.WithGroupState(second.Groups[0], true));
        }

        [Fact]
        public void OrderGivesGroupsTheirSublayersAndLabelledCollections()
        {
            var child = AddGroup(3, "Child");
            var labelled = AddGroup(4, "Labelled");
            var order = Array(on, Array(child), Array(new StringToken("Label"), labelled), off);
            var state = Create(Dictionary(("Order", order)), on, off, child, labelled);

            Assert.Equal(3, state.Order.Count);

            Assert.Equal("On", state.Order[0].Group!.Name);
            Assert.Equal("Child", Assert.Single(state.Order[0].Children).Group!.Name);

            Assert.Null(state.Order[1].Group);
            Assert.Equal("Label", state.Order[1].Label);
            Assert.Equal("Labelled", Assert.Single(state.Order[1].Children).Group!.Name);

            Assert.Equal("Off", state.Order[2].Group!.Name);
            Assert.Empty(state.Order[2].Children);
        }

        [Fact]
        public void GroupsMissingFromOrderAreNotPresented()
        {
            var state = Create(Dictionary(("Order", Array(off))));

            Assert.Equal("Off", Assert.Single(state.Order).Group!.Name);
        }

        [Fact]
        public void AbsentOrEmptyOrderFallsBackToAFlatListOfAllGroups()
        {
            var absent = Create(Dictionary(("OFF", Array(off))));
            var empty = Create(Dictionary(("Order", Array())));

            foreach (var state in new[] { absent, empty })
            {
                Assert.Equal(new[] { "On", "Off" }, state.Order.Select(n => n.Group!.Name));
                Assert.All(state.Order, n => Assert.Empty(n.Children));
            }
        }

        [Fact]
        public void DeeplyNestedOrderIsCutOffRatherThanOverflowing()
        {
            IToken nested = Array(off);
            for (int i = 0; i < 1000; i++)
            {
                nested = Array(nested);
            }

            var state = Create(Dictionary(("Order", Array(on, nested))));

            Assert.Equal("On", state.Order[0].Group!.Name);
        }

        private OptionalContentState Create(DictionaryToken defaultConfiguration, params IToken[] ocgs)
        {
            if (ocgs.Length == 0)
            {
                ocgs = [on, off];
            }

            var catalog = Dictionary(
                ("Type", NameToken.Catalog),
                ("OCProperties", Dictionary(("OCGs", new ArrayToken(ocgs)), ("D", defaultConfiguration))));

            var state = OptionalContentState.Create(catalog, scanner);
            Assert.NotNull(state);
            return state;
        }

        private IndirectReferenceToken AddGroup(long number, string name, IToken? intent = null)
        {
            var entries = new List<(string, IToken)>
            {
                ("Type", NameToken.Ocg),
                ("Name", new StringToken(name))
            };

            if (intent is not null)
            {
                entries.Add(("Intent", intent));
            }

            var reference = new IndirectReference(number, 0);
            scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference, Dictionary(entries.ToArray()));
            return new IndirectReferenceToken(reference);
        }

        private DictionaryToken Resolve(IndirectReferenceToken reference)
        {
            return (DictionaryToken)scanner.Get(reference.Data).Data;
        }

        private DictionaryToken Copy(IndirectReferenceToken reference)
        {
            var data = new Dictionary<NameToken, IToken>();
            foreach (var pair in Resolve(reference).Data)
            {
                data[NameToken.Create(pair.Key)] = pair.Value;
            }

            return new DictionaryToken(data);
        }

        private static DictionaryToken Membership(params (string Key, IToken Value)[] entries)
        {
            var all = new List<(string, IToken)> { ("Type", NameToken.Ocmd) };
            all.AddRange(entries);
            return Dictionary(all.ToArray());
        }

        private static ArrayToken Array(params IToken[] items) => new ArrayToken(items);

        private static DictionaryToken Dictionary(params (string Key, IToken Value)[] entries)
        {
            var data = new Dictionary<NameToken, IToken>();
            foreach (var (key, value) in entries)
            {
                data[NameToken.Create(key)] = value;
            }

            return new DictionaryToken(data);
        }
    }
}