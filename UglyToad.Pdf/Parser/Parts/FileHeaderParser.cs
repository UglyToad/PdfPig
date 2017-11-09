namespace UglyToad.Pdf.Parser.Parts
{
    using System;
    using System.Text.RegularExpressions;
    using IO;
    using Logging;
    using Util;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Used to retrieve the version header from the PDF file.
    /// </summary>
    /// <remarks>
    /// The first line of a PDF file should be a header consisting of the 5 characters %PDF– followed by a version number of the form 1.N, where N is a digit between 0 and 7.
    /// A conforming reader should accept files with any of the following headers:
    /// %PDF–1.0
    /// %PDF–1.1
    /// %PDF–1.2
    /// %PDF–1.3
    /// %PDF–1.4
    /// %PDF–1.5
    /// %PDF–1.6
    /// %PDF–1.7
    /// For versions equal or greater to PDF 1.4, the optional Version entry in the document’s catalog dictionary should be used instead of the header version.
    /// </remarks>
    public class FileHeaderParser
    {
        private const string PdfHeader = "%PDF-";
        private const string FdfHeader = "%FDF-";
        private const string PdfDefaultVersion = "1.4";
        private const string FdfDefaultVersion = "1.0";

        private readonly ILog log;
        
        public FileHeaderParser(ILog log)
        {
            this.log = log;
        }

        [NotNull]
        public HeaderVersion ReadHeader([NotNull]IRandomAccessRead reader, bool isLenientParsing)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (TryFindHeader(PdfHeader, PdfDefaultVersion, reader, isLenientParsing, out var version))
            {
                return version;
            }

            if (TryFindHeader(FdfHeader, FdfDefaultVersion, reader, isLenientParsing, out version))
            {
                return version;
            }
            
            throw new FormatException("The pdf or fdf document did not seem to contain a version header.");
        }

        private bool TryFindHeader(string marker, string defaultVersion, IRandomAccessRead reader, bool isLenientParsing, out HeaderVersion version)
        {
            version = null;

            // Read the first line
            var currentLine = ReadHelper.ReadLine(reader);

            if (!currentLine.Contains(marker))
            {
                // Move to the next line
                currentLine = ReadHelper.ReadLine(reader);

                while (!currentLine.Contains(marker))
                {
                    var startsWithDigit = currentLine.Length > 0 && char.IsDigit(currentLine[0]);
                    // if a line starts with a digit, it has to be the first one with data in it
                    if (startsWithDigit)
                    {
                        break;
                    }

                    currentLine = ReadHelper.ReadLine(reader);
                }
            }

            if (!currentLine.Contains(marker))
            {
                reader.ReturnToBeginning();
                return false;
            }

            var headerStartIndex = currentLine.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

            if (headerStartIndex > 0)
            {
                //trim off any leading characters
                currentLine = currentLine.Substring(headerStartIndex);
            }

            var regex = new Regex($"{marker}\\d.\\d");
            if (currentLine.StartsWith(marker) && !regex.IsMatch(currentLine))
            {
                if (currentLine.Length < marker.Length + 3)
                {
                    // No version number at all, set to 1.4 as default
                    currentLine = marker + defaultVersion;
                    //LOG.debug("No version found, set to " + defaultVersion + " as default.");
                }
                else
                {
                    var headerGarbage = currentLine.Substring(marker.Length + 3) + "\n";

                    currentLine = currentLine.Substring(0, marker.Length + 3);

                    reader.Rewind(OtherEncodings.StringAsLatin1Bytes(headerGarbage).Length);
                }
            }

            decimal headerVersion = -1;
            try
            {
                var headerParts = currentLine.Split('-');

                if (headerParts.Length == 2)
                {
                    headerVersion = decimal.Parse(headerParts[1]);
                }
            }
            catch (FormatException ex)
            {
                log?.Debug("Can't parse the header version: " + currentLine, ex);
            }

            if (headerVersion < 0)
            {
                if (isLenientParsing)
                {
                    headerVersion = 1.7m;
                }
                else
                {
                    throw new InvalidOperationException("Error getting header version: " + currentLine);
                }
            }

            reader.ReturnToBeginning();
            version = new HeaderVersion(headerVersion, currentLine);

            return true;
        }
    }
}
