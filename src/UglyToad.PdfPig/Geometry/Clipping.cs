namespace UglyToad.PdfPig.Geometry
{
    using UglyToad.PdfPig.Graphics;

    /// <summary>
    /// 
    /// </summary>
    internal static class Clipping
    {
        const double factor = 10_000.0;

        /// <summary>
        /// DOES NOTHING
        /// </summary>
        /// <param name="clipping"></param>
        /// <param name="subject"></param>
        /// <returns></returns>
        public static PdfPath Clip(this PdfPath clipping, PdfPath subject)
        {
            return subject;
        }
    }
}
