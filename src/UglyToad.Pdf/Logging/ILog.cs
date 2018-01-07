using System;

namespace UglyToad.Pdf.Logging
{
    /// <summary>
    /// Logs internal messages from the PDF parsing process. Consumers can provide their own implementation
    /// in the <see cref="ParsingOptions"/> to intercept log messages.
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// Record an informational debug message.
        /// </summary>
        void Debug(string message);
        /// <summary>
        /// Record an informational debug message with exception.
        /// </summary>
        void Debug(string message, Exception ex);
        /// <summary>
        /// Record an warning message due to a non-error issue encountered in parsing.
        /// </summary>
        void Warn(string message);
        /// <summary>
        /// Record an error message due to an issue encountered in parsing.
        /// </summary>
        void Error(string message);
        /// <summary>
        /// Record an error message due to an issue encountered in parsing with exception.
        /// </summary>
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
