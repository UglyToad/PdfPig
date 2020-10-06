using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig.Logging;

namespace UglyToad.PdfPig.SystemDrawing
{
    public class SystemDrawingLogger : ILog
    {
        public void Debug(string message)
        {
            System.Diagnostics.Debug.Print("Debug: " + message);
        }

        public void Debug(string message, Exception ex)
        {
            System.Diagnostics.Debug.Print("Debug: " + message);
        }

        public void Error(string message)
        {
            System.Diagnostics.Debug.Print("Error: " + message);
        }

        public void Error(string message, Exception ex)
        {
            System.Diagnostics.Debug.Print("Error: " + message);
        }

        public void Warn(string message)
        {
            System.Diagnostics.Debug.Print("Warn: " + message);
        }
    }
}
