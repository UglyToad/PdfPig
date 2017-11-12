namespace UglyToad.Pdf.Tokenization.Tokens
{
    public class ObjectDelimiterToken : IDataToken<string>
    {
        public static ObjectDelimiterToken StartObject = new ObjectDelimiterToken("obj");
        public static ObjectDelimiterToken EndObject = new ObjectDelimiterToken("endobj");
        public static ObjectDelimiterToken StartStream = new ObjectDelimiterToken("stream");
        public static ObjectDelimiterToken EndStream = new ObjectDelimiterToken("endstream");

        public string Data { get; }

        private ObjectDelimiterToken(string data)
        {
            Data = data;
        }
    }
}
