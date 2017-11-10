namespace UglyToad.Pdf.Tests
{
    using System.Text;
    using IO;

    public static class StringBytesTestConverter
    {
        public static Result Convert(string s)
        {
            var input = new ByteArrayInputBytes(Encoding.UTF8.GetBytes(s));

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
