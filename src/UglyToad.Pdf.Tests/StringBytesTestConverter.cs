namespace UglyToad.Pdf.Tests
{
    using System.Linq;
    using IO;

    public static class StringBytesTestConverter
    {
        public static Result Convert(string s)
        {
            var input = new ByteArrayInputBytes(s.Select(x => (byte)x).ToArray());

            input.MoveNext();
            var initialByte = input.CurrentByte;

            return new Result
            {
                First = initialByte,
                Bytes = input
            };
        }

        public class Result
        {
            public byte First { get; set; }

            public IInputBytes Bytes { get; set; }
        }
    }
}
