namespace UglyToad.Pdf.Text.Operators
{
    using System;
    using System.Collections.Generic;

    public class NumericTextComponentApproach : ITextComponentApproach
    {
        private static readonly HashSet<byte> SupportedCharacterSet = new HashSet<byte>
        {
            (byte)'0',
            (byte)'1',
            (byte)'2',
            (byte)'3',
            (byte)'4',
            (byte)'5',
            (byte)'6',
            (byte)'7',
            (byte)'8',
            (byte)'9',
            (byte)'+',
            (byte)'-',
            (byte)'.'
        };

        public bool CanRead(byte b, int offset)
        {
            return SupportedCharacterSet.Contains(b);
        }

        public ITextObjectComponent Read(IReadOnlyList<byte> readBytes, IEnumerable<byte> furtherBytes, out int offset)
        {
            offset = readBytes.Count;
            var bytes = new List<byte>(readBytes);

            using (var reader = furtherBytes.GetEnumerator())
            {
                while (reader.MoveNext() && !BaseTextComponentApproach.IsEmpty(reader.Current))
                {
                    if (!SupportedCharacterSet.Contains(reader.Current))
                    {
                        throw new InvalidOperationException("Unsupported byte in numeric operator: " + (char)reader.Current);
                    }

                    bytes.Add(reader.Current);
                    offset++;
                }
            }

            return new OperandComponent(new NumericOperand(bytes), TextObjectComponentType.Numeric);
        }
    }

    public class NumericOperand : IOperand
    {
        public NumericOperand(IReadOnlyList<byte> bytes)
        {
            
        }

        public IReadOnlyList<byte> RawBytes { get; set; }
    }
}