using System;

namespace UglyToad.Pdf.Logging
{
    public interface ILog
    {
        void Debug(string message);
        void Debug(string message, Exception ex);
        void Warn(string message);
        void Error(string message);
        void Error(string message, Exception ex);
    }

    internal class NoOpLog : ILog
    {
        public void Debug(string message)
        {
        }

        public void Debug(string message, Exception ex)
        {
        }

        public void Warn(string message)
        {
        }

        public void Error(string message)
        {
        }

        public void Error(string message, Exception ex)
        {
        }
    }
}
