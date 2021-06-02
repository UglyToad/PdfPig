namespace UglyToad.PdfPig.Tests
{
    using System;

    public static class TestEnvironment
    {
        public static readonly bool IsUnixPlatform = Environment.NewLine.Length == 1;
    }
}
