namespace UglyToad.PdfPig.Tests.Graphics
{
    using System;
    using System.Collections.Generic;
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.Graphics;
    using PdfPig.Tokens;
    using PdfPig.Tests.Tokens;
    using Xunit;

    public class OptionalContentConditionTests
    {
        private readonly TestPdfTokenScanner scanner = new TestPdfTokenScanner();

        [Fact]
        public void OutsideAnyLayerTheConditionIsAlways()
        {
            var (tracker, _, _, state) = TwoGroups();

            Assert.Same(OptionalContentCondition.Always, tracker.Current);
            Assert.True(tracker.Current.IsAlways);
            Assert.True(tracker.Current.IsVisible(state));
        }

        [Fact]
        public void NestedLayersCombineAsAConjunction()
        {
            var (tracker, on, off, state) = TwoGroups();

            tracker.Begin(NameToken.Oc, on);
            var outer = tracker.Current;
            tracker.Begin(NameToken.Oc, off);
            var inner = tracker.Current;

            Assert.True(outer.IsVisible(state));
            Assert.False(inner.IsVisible(state));

            var offTurnedOn = state.WithGroupState(state.Groups[1], true);
            Assert.True(inner.IsVisible(offTurnedOn));

            tracker.End();
            Assert.Same(outer, tracker.Current);
            tracker.End();
            Assert.Same(OptionalContentCondition.Always, tracker.Current);
        }

        [Fact]
        public void TheSameChainIsInterned()
        {
            var (tracker, on, _, _) = TwoGroups();

            tracker.Begin(NameToken.Oc, on);
            var first = tracker.Current;
            tracker.End();
            tracker.Begin(NameToken.Oc, on);

            Assert.Same(first, tracker.Current);
        }

        [Fact]
        public void NonOcTagsDoNotChangeTheCondition()
        {
            var (tracker, on, _, _) = TwoGroups();

            tracker.Begin(NameToken.Create("Span"), on);

            Assert.Same(OptionalContentCondition.Always, tracker.Current);
        }

        [Fact]
        public void EnterUsesTheDictionarysOcEntry()
        {
            var (tracker, _, off, state) = TwoGroups();
            var image = new DictionaryToken(new Dictionary<NameToken, IToken> { { NameToken.Oc, off } });

            Assert.True(tracker.Enter(image));
            Assert.False(tracker.Current.IsVisible(state));
            tracker.Exit();
            Assert.Same(OptionalContentCondition.Always, tracker.Current);

            var plain = new DictionaryToken(new Dictionary<NameToken, IToken>());
            Assert.False(tracker.Enter(plain));
        }

        [Fact]
        public void IsVisibleRejectsAStateOfAnotherDocument()
        {
            var (tracker, on, _, _) = TwoGroups();
            var (_, _, _, otherState) = TwoGroups();

            tracker.Begin(NameToken.Oc, on);

            Assert.Throws<ArgumentException>(() => tracker.Current.IsVisible(otherState));
        }

        [Fact]
        public void WithoutDocumentOptionalContentEverythingIsAlways()
        {
            var tracker = new MarkedContentTracker(null, null, false, scanner);

            tracker.Begin(NameToken.Oc, Group("Any"));

            Assert.Same(OptionalContentCondition.Always, tracker.Current);
        }

        [Fact]
        public void VisibilityStateDrivesIsHidden()
        {
            var (_, on, off, state) = TwoGroups();
            var tracker = new MarkedContentTracker(state, state, false, scanner);

            tracker.Begin(NameToken.Oc, on);
            Assert.False(tracker.IsHidden);
            tracker.Begin(NameToken.Oc, off);
            Assert.True(tracker.IsHidden);
            tracker.End();
            Assert.False(tracker.IsHidden);

            var notSkipping = new MarkedContentTracker(state, null, false, scanner);
            notSkipping.Begin(NameToken.Oc, off);
            Assert.False(notSkipping.IsHidden);
            Assert.False(notSkipping.Current.IsVisible(state));
        }

        private (MarkedContentTracker Tracker, DictionaryToken On, DictionaryToken Off, OptionalContentState State) TwoGroups()
        {
            var on = Group("On");
            var off = Group("Off");
            var onRef = new IndirectReference(1, 0);
            var offRef = new IndirectReference(2, 0);
            scanner.Objects[onRef] = new ObjectToken(XrefLocation.File(0), onRef, on);
            scanner.Objects[offRef] = new ObjectToken(XrefLocation.File(0), offRef, off);
            var ocgs = new ArrayToken([new IndirectReferenceToken(onRef), new IndirectReferenceToken(offRef)]);
            var offList = new ArrayToken([new IndirectReferenceToken(offRef)]);

            var catalog = Dictionary(
                (NameToken.Type, NameToken.Catalog),
                (NameToken.Ocproperties, Dictionary((NameToken.Ocgs, ocgs), (NameToken.D, Dictionary((NameToken.Off, offList))))));

            var state = OptionalContentState.Create(catalog, scanner);
            Assert.NotNull(state);
            return (new MarkedContentTracker(state, null, false, scanner), on, off, state);
        }

        private static DictionaryToken Group(string name)
            => Dictionary((NameToken.Type, NameToken.Ocg), (NameToken.Name, new StringToken(name)));

        private static DictionaryToken Dictionary(params (NameToken Key, IToken Value)[] entries)
        {
            var data = new Dictionary<NameToken, IToken>();
            foreach (var (key, value) in entries)
            {
                data[key] = value;
            }

            return new DictionaryToken(data);
        }
    }
}
