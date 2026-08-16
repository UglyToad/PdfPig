namespace UglyToad.PdfPig.Tests.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using PdfPig.Graphics.Colors.Icc;
    using PdfPig.Logging;
    using PdfPig.Util;

    /// <summary>
    /// Handing profile bytes to a third-party <see cref="IIccProfileService"/> is the one step every ICC
    /// route shares - a profile stream the document points at, and the profile embedded in a JPEG 2000
    /// codestream. The two used to carry their own copy of it and had already drifted apart on what a
    /// service that <i>declines</i> should produce, so the step lives in one place.
    /// </summary>
    public class IccProfileParserTests
    {
        private sealed class RecordingLog : ILog
        {
            public List<string> Warnings { get; } = [];

            public List<string> Errors { get; } = [];

            public void Debug(string message) { }

            public void Debug(string message, Exception ex) { }

            public void Warn(string message) => Warnings.Add(message);

            public void Error(string message) => Errors.Add(message);

            public void Error(string message, Exception ex) => Errors.Add(message);
        }

        /// <summary>
        /// Answers <paramref name="profile"/>, or throws when <paramref name="throws"/>.
        /// </summary>
        private sealed class StubService(IIccProfile? profile, bool throws = false) : IIccProfileService
        {
            public bool TryGetProfile(ReadOnlyMemory<byte> profileBytes, [NotNullWhen(true)] out IIccProfile? parsed)
            {
                if (throws)
                {
                    throw new InvalidOperationException("Simulated profile parser failure.");
                }

                parsed = profile;
                return parsed is not null;
            }

            public bool UseOutputIntent => false;

            public string? PreferredOutputIntentSubtype => null;
        }

        private static readonly ReadOnlyMemory<byte> ProfileBytes = new byte[] { 1, 2, 3, 4 };

        [Fact]
        public void AParsedProfileIsReturnedAndNothingIsLogged()
        {
            var log = new RecordingLog();
            var expected = new TestIccProfile();

            var profile = IccProfileParser.Parse(ProfileBytes, new StubService(expected), log);

            Assert.Same(expected, profile);
            Assert.Empty(log.Warnings);
            Assert.Empty(log.Errors);
        }

        [Fact]
        public void AServiceThatDeclinesIsLoggedAsAWarning()
        {
            // The divergence this method exists to remove: the profile-stream route warned here while the
            // JPEG 2000 route dropped the profile silently, so the same unreadable profile was diagnosable
            // in one place and invisible in the other.
            var log = new RecordingLog();

            var profile = IccProfileParser.Parse(ProfileBytes, new StubService(null), log);

            Assert.Null(profile);
            Assert.Single(log.Warnings);
            Assert.Empty(log.Errors);
        }

        [Fact]
        public void AServiceThatThrowsIsLoggedAsAnErrorRatherThanPropagating()
        {
            // TryGetProfile is third-party code. A profile that makes it throw is no worse than one it
            // declines: it costs the colour space its profile and nothing more.
            var log = new RecordingLog();

            var profile = IccProfileParser.Parse(ProfileBytes, new StubService(null, throws: true), log);

            Assert.Null(profile);
            Assert.Single(log.Errors);
        }

        [Fact]
        public void ANullLogIsTolerated()
        {
            // ILog is optional throughout the ICC paths.
            Assert.Null(IccProfileParser.Parse(ProfileBytes, new StubService(null), null));
        }
    }
}
