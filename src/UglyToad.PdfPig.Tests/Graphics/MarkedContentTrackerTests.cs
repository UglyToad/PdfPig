namespace UglyToad.PdfPig.Tests.Graphics
{
    using System.Collections.Generic;
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.Graphics;
    using PdfPig.Tokens;
    using PdfPig.Tests.Tokens;
    using Xunit;

    /// <summary>
    /// Covers <see cref="MarkedContentTracker"/>: marked-content nesting across content streams (14.6),
    /// hidden optional content (8.11.3) and replacement text (14.9.4).
    /// </summary>
    public class MarkedContentTrackerTests
    {
        private static readonly NameToken Span = NameToken.Create("Span");

        private readonly TestPdfTokenScanner scanner = new TestPdfTokenScanner();

        [Fact]
        public void UnbalancedEndIsIgnored()
        {
            var tracker = new MarkedContentTracker(null, null, false, scanner);

            Assert.False(tracker.CanEnd);
            Assert.False(tracker.End());

            tracker.Begin(Span, null);
            Assert.True(tracker.End());
            Assert.False(tracker.End());
        }

        [Fact]
        public void StreamCannotEndSequencesOpenedByEnclosingStream()
        {
            var tracker = new MarkedContentTracker(null, null, false, scanner);

            tracker.Begin(Span, null);
            int enclosingFloor = tracker.EnterStream();

            // The form's own sequence can be ended, the page's one cannot.
            tracker.Begin(Span, null);
            Assert.True(tracker.End());
            Assert.False(tracker.End());

            tracker.ExitStream(enclosingFloor);
            Assert.True(tracker.End());
        }

        [Fact]
        public void HiddenUntilTheHidingSequenceEnds()
        {
            var (tracker, off) = WithHiddenGroup();

            tracker.Begin(Span, null);
            Assert.False(tracker.IsHidden);

            tracker.Begin(NameToken.Oc, off);
            Assert.True(tracker.IsHidden);

            // Nested sequences, visible or not, keep the content hidden.
            tracker.Begin(Span, null);
            tracker.Begin(NameToken.Oc, null);
            tracker.End();
            tracker.End();
            Assert.True(tracker.IsHidden);

            tracker.End();
            Assert.False(tracker.IsHidden);
        }

        [Fact]
        public void OnlyTheOcTagHidesContent()
        {
            var (tracker, off) = WithHiddenGroup();

            tracker.Begin(Span, off);
            Assert.False(tracker.IsHidden);
        }

        [Fact]
        public void HiddenGroupIsIgnoredWhenNotSkipping()
        {
            var tracker = new MarkedContentTracker(null, null, false, scanner);

            tracker.Begin(NameToken.Oc, Group("Off"));
            Assert.False(tracker.IsHidden);
        }

        [Fact]
        public void ActualTextGoesToTheFirstGlyphOnly()
        {
            var tracker = new MarkedContentTracker(null, null, true, scanner);

            Assert.Equal("a", tracker.ApplyActualText("a"));

            tracker.Begin(Span, ActualText("fi\u00adne"));

            // Soft hyphens are stripped; the rest of the sequence extracts as nothing.
            Assert.Equal("fine", tracker.ApplyActualText("f"));
            Assert.Equal(string.Empty, tracker.ApplyActualText("i"));

            tracker.End();
            Assert.Equal("b", tracker.ApplyActualText("b"));
        }

        [Fact]
        public void NestedActualTextDoesNotReplaceTheOuterOne()
        {
            var tracker = new MarkedContentTracker(null, null, true, scanner);

            tracker.Begin(Span, ActualText("outer"));
            tracker.Begin(Span, ActualText("inner"));
            Assert.Equal("outer", tracker.ApplyActualText("x"));

            // Ending the inner sequence keeps the outer one in effect.
            tracker.End();
            Assert.Equal(string.Empty, tracker.ApplyActualText("y"));

            tracker.End();
            Assert.Equal("z", tracker.ApplyActualText("z"));
        }

        [Fact]
        public void ActualTextIsIgnoredWhenNotUsed()
        {
            var tracker = new MarkedContentTracker(null, null, false, scanner);

            tracker.Begin(Span, ActualText("replacement"));
            Assert.Equal("a", tracker.ApplyActualText("a"));
        }

        private (MarkedContentTracker Tracker, DictionaryToken Off) WithHiddenGroup()
        {
            var off = Group("Off");
            var reference = new IndirectReference(1, 0);
            scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference, off);
            var ocgs = new ArrayToken([new IndirectReferenceToken(reference)]);

            var catalog = Dictionary(
                (NameToken.Type, NameToken.Catalog),
                (NameToken.Ocproperties, Dictionary((NameToken.Ocgs, ocgs), (NameToken.D, Dictionary((NameToken.Off, ocgs))))));

            var state = OptionalContentState.Create(catalog, scanner);
            Assert.NotNull(state);
            return (new MarkedContentTracker(state, state, false, scanner), off);
        }

        private static DictionaryToken Group(string name)
        {
            return Dictionary((NameToken.Type, NameToken.Ocg), (NameToken.Name, new StringToken(name)));
        }

        private static DictionaryToken ActualText(string text)
        {
            return Dictionary((NameToken.ActualText, new StringToken(text)));
        }

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