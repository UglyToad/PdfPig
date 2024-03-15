namespace UglyToad.PdfPig.Tests
{
    public static class TestEnvironment
    {
        public static bool IsSingleByteNewLine(string s) => s.IndexOf('\r') < 0;           
    }
}
