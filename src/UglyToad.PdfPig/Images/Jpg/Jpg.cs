namespace UglyToad.PdfPig.Images.Jpg
{
    using System.Collections.Generic;
    using System.Data.SqlTypes;
    using System.IO;
    using System.Runtime.InteropServices.ComTypes;
    using UglyToad.PdfPig.Tokens;
    using Parser = UglyToad.PdfPig.Images.Jpg.Helpers.Parser;


    /// <summary>
    /// Jpg
    /// </summary>
    public class Jpg
    {
#if DEBUG
        /// <summary>
        /// isOnDebug
        /// </summary>
        public static bool isOnDebug = true;        // additional debug logging
        /// <summary>
        /// HasRestart
        /// </summary>
        public static bool hasRestart = false;
        /// <summary>
        /// hasJpgEndOfStreamWithoutEndOfImageMarker
        /// </summary>
        public static bool hasJpgEndOfStreamWithoutEndOfImageMarker = false;
         
        /// <summary>
        /// hasAdobeAppSegment
        /// </summary>
        public static bool hasAdobeAppSegment = false;

        /// <summary>
        /// AdobeAppSegmentTransformCode
        /// </summary>
        public static int AdobeAppSegmentTransformCode = -1; // Default none


        /// <summary>
        /// precision - 8 bit or 16 bit Jpg
        /// </summary>
        public static int precision = -1;
#endif
        /// <summary>
        /// Width
        /// </summary>
        public int Width { get; private set; }
        /// <summary>
        /// Height
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// Number Of Components
        /// </summary>
        public int NumberOfComponents { get; private set; }
         
        /// <summary>
        /// Data
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Comments
        /// </summary>
        public List<string> Comments { get; private set; }

        internal Jpg(int width, int height, int numberOfComponents, byte[] data , List<string>comments) {
            this.Width = width;
            this.Height = height;
            this.Data = data;
            this.NumberOfComponents = numberOfComponents;
            this.Comments = comments;
        }

        /// <summary>
        /// Parse
        /// </summary>
        /// <param name="ab">DCT encoded bytes to be decoded</param>
        /// /// <param name="dictionary">Pdf Dictionary of stream to decode</param>
        /// <returns></returns>
        public static Jpg Parse(byte[] ab, DictionaryToken dictionary)
        {
            return Parser.Parse(ab, dictionary);
        }
    }
}

