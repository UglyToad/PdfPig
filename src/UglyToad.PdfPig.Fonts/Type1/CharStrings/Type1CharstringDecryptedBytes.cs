namespace UglyToad.PdfPig.Fonts.Type1.CharStrings
{
    using System;
    using System.Globalization;

    internal sealed class Type1CharstringDecryptedBytes
    {
        private readonly byte[] _bytes;

        public ReadOnlySpan<byte> Bytes => _bytes;

        public int Index { get; }

        public string Name { get; }

        public SourceType Source { get; }

        public Type1CharstringDecryptedBytes(byte[] bytes, int index)
        {
            _bytes = bytes;
            Index = index;
            Name = GlyphList.NotDefined;
            Source = SourceType.Subroutine;
        }

        public Type1CharstringDecryptedBytes(string name, byte[] bytes, int index)
        {
            _bytes = bytes;
            Index = index;
            Name = name ?? index.ToString(CultureInfo.InvariantCulture);
            Source = SourceType.Charstring;
        }

        public enum SourceType
        {
            Subroutine,
            Charstring
        }

        public override string ToString()
        {
            return $"{Name} {Source} {Index} {Bytes.Length} bytes";
        }
    }
}
