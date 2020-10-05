using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Graphics;
using UglyToad.PdfPig.Graphics.Colors;
using UglyToad.PdfPig.PdfFonts;

namespace UglyToad.PdfPig
{
    /// <summary>
    /// IDrawingSystem
    /// </summary>
    public interface IDrawingProcessor
    {
        /// <summary>
        /// Init
        /// </summary>
        /// <param name="page"></param>
        void Init(Page page);

        /// <summary>
        /// DrawPage
        /// </summary>
        /// <param name="page"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        MemoryStream DrawPage(Page page, double scale);

        /// <summary>
        /// DrawLetter
        /// </summary>
        /// <param name="value"></param>
        /// <param name="glyphRectangle"></param>
        /// <param name="startBaseLine"></param>
        /// <param name="endBaseLine"></param>
        /// <param name="width"></param>
        /// <param name="fontSize"></param>
        /// <param name="font"></param>
        /// <param name="color"></param>
        /// <param name="pointSize"></param>
        void DrawLetter(string value, PdfRectangle glyphRectangle, PdfPoint startBaseLine, PdfPoint endBaseLine, double width, double fontSize, FontDetails font, IColor color, double pointSize);

        /// <summary>
        /// DrawLetter
        /// </summary>
        /// <param name="pdfSubpaths"></param>
        /// <param name="color"></param>
        /// <param name="renderingMatrix"></param>
        /// <param name="textMatrix"></param>
        /// <param name="transformationMatrix"></param>
        void DrawLetter(List<PdfSubpath> pdfSubpaths, IColor color, TransformationMatrix renderingMatrix, TransformationMatrix textMatrix, TransformationMatrix transformationMatrix);

        /// <summary>
        /// DrawImage
        /// </summary>
        /// <param name="image"></param>
        void DrawImage(IPdfImage image);

        /// <summary>
        /// Draw path.
        /// </summary>
        /// <param name="path"></param>
        void DrawPath(PdfPath path);

        /// <summary>
        /// Update clipping.
        /// </summary>
        void UpdateClipPath();
    }
}
