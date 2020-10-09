namespace UglyToad.PdfPig.Content
{
    using System;
    using Core;

    /// <summary>
    /// The boundary of the physical medium to display or print on.
    /// </summary>
    /// <remarks>
    /// See table 3.27 from the PDF specification version 1.7.
    /// </remarks>
    public class MediaBox
    {
        ///<summary>
        /// User space units per inch.
        /// </summary>
        private const double PointsPerInch = 72;

        /// <summary>
        /// User space units per millimeter.
        /// </summary>
        private const double PointsPerMm = 1 / (10 * 2.54) * PointsPerInch;

        /// <summary>
        /// A <see cref="MediaBox"/> the size of U.S. Letter, 8.5" x 11" Paper.
        /// </summary>
        public static readonly MediaBox Letter = new MediaBox(new PdfRectangle(0, 0, 8.5 * PointsPerInch, 11 * PointsPerInch));

        /// <summary>
        /// A <see cref="MediaBox"/> the size of U.S. Legal, 8.5" x 14" Paper.
        /// </summary>
        public static readonly MediaBox Legal = new MediaBox(new PdfRectangle(0, 0, 8.5 * PointsPerInch, 14 * PointsPerInch));

        /// <summary>
        /// A <see cref="MediaBox"/> the size of A0 Paper.
        /// </summary>
        public static readonly MediaBox A0 = new MediaBox(new PdfRectangle(0, 0, 841 * PointsPerMm, 1189 * PointsPerMm));

        /// <summary>
        /// A <see cref="MediaBox"/> the size of A1 Paper.
        /// </summary>
        public static readonly MediaBox A1 = new MediaBox(new PdfRectangle(0, 0, 594 * PointsPerMm, 841 * PointsPerMm));

        /// <summary>
        /// A <see cref="MediaBox"/> the size of A2 Paper.
        /// </summary>
        public static readonly MediaBox A2 = new MediaBox(new PdfRectangle(0, 0, 420 * PointsPerMm, 594 * PointsPerMm));

        /// <summary>
        /// A <see cref="MediaBox"/> the size of A3 Paper.
        /// </summary>
        public static readonly MediaBox A3 = new MediaBox(new PdfRectangle(0, 0, 297 * PointsPerMm, 420 * PointsPerMm));

        /// <summary>
        /// A <see cref="MediaBox"/> the size of A4 Paper.
        /// </summary>
        public static readonly MediaBox A4 = new MediaBox(new PdfRectangle(0, 0, 210 * PointsPerMm, 297 * PointsPerMm));

        /// <summary>
        /// A <see cref="MediaBox"/> the size of A5 Paper.
        /// </summary>
        public static readonly MediaBox A5 = new MediaBox(new PdfRectangle(0, 0, 148 * PointsPerMm, 210 * PointsPerMm));

        /// <summary>
        /// A <see cref="MediaBox"/> the size of A6 Paper.
        /// </summary>
        public static readonly MediaBox A6 = new MediaBox(new PdfRectangle(0, 0, 105 * PointsPerMm, 148 * PointsPerMm));

        /// <summary>
        /// Bounds
        /// </summary>
        public PdfRectangle Bounds { get; }

        /// <summary>
        /// MediaBox
        /// </summary>
        /// <param name="bounds"></param>
        internal MediaBox(PdfRectangle? bounds)
        {
            Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
        }
    }
}
