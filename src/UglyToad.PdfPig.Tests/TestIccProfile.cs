namespace UglyToad.PdfPig.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using PdfPig.Graphics.Colors.Icc;
    using PdfPig.Graphics.Core;

    /// <summary>
    /// A profile that parses but yields no transform, with unit ranges for each of its
    /// <paramref name="numberOfComponents"/> components.
    /// <para>
    /// This is the profile for a test that only needs one to <i>exist</i> - that a colour space adopted it,
    /// that an output intent carries one, that a cache handed back the same instance. A test about what a
    /// profile converts to wants its own double instead, with a transform that answers something
    /// recognisable.
    /// </para>
    /// </summary>
    internal sealed class TestIccProfile(int numberOfComponents = 3) : IIccProfile
    {
        public int NumberOfComponents { get; } = numberOfComponents;

        public IReadOnlyList<double> ComponentRanges { get; } =
            Enumerable.Repeat(new[] { 0.0, 1.0 }, numberOfComponents).SelectMany(x => x).ToArray();

        public bool TryGetTransform(RenderingIntent intent, [NotNullWhen(true)] out IIccTransform? transform)
        {
            transform = null;
            return false;
        }
    }

    /// <summary>
    /// A service that parses whatever it is handed, into a fresh <see cref="TestIccProfile"/>. For tests
    /// where the profile only has to be resolvable; the services that decline, throw or count are specific
    /// to the tests that need them and live there.
    /// </summary>
    internal sealed class TestIccProfileService(int numberOfComponents = 3) : IIccProfileService
    {
        public bool TryGetProfile(ReadOnlyMemory<byte> profileBytes, [NotNullWhen(true)] out IIccProfile? profile)
        {
            profile = new TestIccProfile(numberOfComponents);
            return true;
        }

        /// <summary>
        /// Output-intent colour management is a rendering concern that PdfPig's own tests never exercise.
        /// </summary>
        public bool UseOutputIntent => false;

        public string? PreferredOutputIntentSubtype => null;
    }
}
