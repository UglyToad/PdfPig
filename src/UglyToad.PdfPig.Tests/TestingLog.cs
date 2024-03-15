namespace UglyToad.PdfPig.Tests
{
    using Logging;

    public class TestingLog : ILog
    {
        public void Debug(string message){}

        public void Debug(string message, Exception ex){}

        public void Warn(string message){}

        public void Error(string message)
        {
            
        }

        public void Error(string message, Exception ex)
        {
        }
    }
}