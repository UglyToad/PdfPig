namespace UglyToad.PdfPig.Tests.ContentTests
{
    using System.Collections.Generic;
    using PdfPig.Content;
    using PdfPig.Core;
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