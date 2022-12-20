namespace UglyToad.PdfPig.Tests
{
    using System;

    public static class TestEnvironment
    {
        public static bool IsSingleByteNewLine(string s) => s.IndexOf('\r') < 0;
            
    }
}
