namespace UglyToad.PdfPig.Tests.Fonts.TrueType
{
    using System;
    using System.IO;

    internal static class TrueTypeTestHelper
    {
        public static byte[] GetFileBytes(string name)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "TrueType");

            name = name.EndsWith(".ttf") || name.EndsWith(".txt") ? name : name + ".ttf";

            var file = Path.Combine(path, name);

            return File.ReadAllBytes(file);
        }
    }
}
