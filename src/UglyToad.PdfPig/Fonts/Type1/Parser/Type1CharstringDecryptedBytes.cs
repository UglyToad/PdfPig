namespace UglyToad.PdfPig.Fonts.Type1.Parser
{
    using System;
    using System.Collections.Generic;

    internal class Type1CharstringDecryptedBytes
    {
        public IReadOnlyList<byte> Bytes { get; }

        public int Index { get; }

        public string Name { get; }

        public SourceType Source { get; }

        public Type1CharstringDecryptedBytes(IReadOnlyList<byte> bytes, int index)
        {
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
            Index = index;
            Name = ".notdef";
            Source = SourceType.Subroutine;
        }

        public Type1CharstringDecryptedBytes(string name, IReadOnlyList<byte> bytes, int index)
        {
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
            Index = index;
            Name = name;
            Source = SourceType.Charstring;
        }

        public enum SourceType
        {
            Subroutine,
            Charstring
        }

        public override string ToString()
        {
            return $"{Name} {Source} {Index} {Bytes.Count} bytes";
        }
    }
}
