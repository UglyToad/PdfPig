namespace UglyToad.Pdf.Text.Operators
{
    using System.Collections.Generic;

    public class FontTextComponentApproach : ITextComponentApproach
    {
        public bool CanRead(byte b, int offset)
        {
            if (offset == 0 && b == '/')
            {
                return true;
            }

            if (offset == 0)
            {
                return false;
            }

            return !BaseTextComponentApproach.IsEmpty(b);
        }

        public ITextObjectComponent Read(IReadOnlyList<byte> readBytes, IEnumerable<byte> furtherBytes, out int offset)
        {
            offset = readBytes.Count;

            using (var reader = furtherBytes.GetEnumerator())
            {
                var values = new List<byte>(readBytes);

                while (reader.MoveNext() && !BaseTextComponentApproach.IsEmpty(reader.Current))
                {
                    values.Add(reader.Current);
                    offset++;
                }

                return new OperandComponent(new FontOperand(values), TextObjectComponentType.Font);
            }
        }
    }

    public class OperandComponent : ITextObjectComponent
    {
        public bool IsOperator { get; } = false;

        public IReadOnlyList<TextObjectComponentType> OperandTypes { get; } = new TextObjectComponentType[0];

        public TextObjectComponentType Type { get; }

        public IOperand AsOperand { get; }

        public OperandComponent(IOperand operand, TextObjectComponentType type)
        {
            Type = type;
            AsOperand = operand;
        }
    }

    public class FontOperand : IOperand
    {
        public IReadOnlyList<byte> RawBytes { get; }

        public FontOperand(IReadOnlyList<byte> bytes)
        {
            RawBytes = bytes;
        }
    }

    public class StringOperand : IOperand
    {
        public IReadOnlyList<byte> RawBytes { get; }

        public StringOperand(IReadOnlyList<byte> bytes)
        {
            RawBytes = bytes;
        }
    }
}