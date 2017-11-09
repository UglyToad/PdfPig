namespace UglyToad.Pdf.Text.Operators
{
    using System.Collections.Generic;

    public class BaseTextComponentApproach : ITextComponentApproach
    {
        private readonly byte[] bytes;
        private readonly TextObjectComponentType textObjectComponentType;
        private readonly IReadOnlyList<TextObjectComponentType> operandTypes;

        public BaseTextComponentApproach(byte[] bytes, TextObjectComponentType textObjectComponentType,
            IReadOnlyList<TextObjectComponentType> operandTypes)
        {
            this.bytes = bytes;
            this.textObjectComponentType = textObjectComponentType;
            this.operandTypes = operandTypes;
        }

        public bool CanRead(byte b, int offset)
        {
            if (offset >= bytes.Length)
            {
                return false;
            }

            return bytes[offset] == b;
        }

        public ITextObjectComponent Read(IReadOnlyList<byte> readBytes, IEnumerable<byte> furtherBytes, out int offset)
        {
            bool hasOpenedEnumerator = false;
            offset = bytes.Length;
            using (var enumerator = furtherBytes.GetEnumerator())
            {
                for (var i = 0; i < bytes.Length; i++)
                {
                    if (i < readBytes.Count)
                    {
                        if (readBytes[i] != bytes[i])
                        {
                            return null;
                        }

                        // Look beyond the end
                        if (i == bytes.Length - 1)
                        {
                            if (!hasOpenedEnumerator && enumerator.MoveNext() && !IsEmpty(enumerator.Current))
                            {
                                return null;
                            }
                        }
                    }
                    else
                    {
                        hasOpenedEnumerator = true;

                        if (!enumerator.MoveNext())
                        {
                            return null;
                        }

                        var curr = enumerator.Current;

                        if (curr != bytes[i])
                        {
                            return null;
                        }

                        if (i == bytes.Length - 1 && enumerator.MoveNext() && !IsEmpty(enumerator.Current))
                        {
                            return null;
                        }
                    }
                }
            }

            return new Operator(textObjectComponentType, operandTypes);
        }

        public static bool IsEmpty(byte b)
        {
            return b == ' ' || b == '\r' || b == '\n' || b == 0;
        }
    }
}