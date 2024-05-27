namespace UglyToad.PdfPig.Tests.Dla
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.DocumentLayoutAnalysis;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
    using UglyToad.PdfPig.Core;

    public class UnsupervisedReadingOrderTests
    {
        [Fact]
        public void ReadingOrderOrdersItemsOnTheSameRowContents()
        {
            TextBlock leftTextBlock = CreateFakeTextBlock(new PdfRectangle(new PdfPoint(0, 0), new PdfPoint(10, 10)));
            TextBlock rightTextBlock = CreateFakeTextBlock(new PdfRectangle(new PdfPoint(100, 0), new PdfPoint(110, 10)));

            // We deliberately submit in the wrong order
            var textBlocks = new List<TextBlock>() { rightTextBlock, leftTextBlock };

            var unsupervisedReadingOrderDetector = new UnsupervisedReadingOrderDetector(5, UnsupervisedReadingOrderDetector.SpatialReasoningRules.RowWise);
            var orderedBlocks = unsupervisedReadingOrderDetector.Get(textBlocks);

            var ordered = orderedBlocks.OrderBy(x => x.ReadingOrder).ToList();
            Assert.Equal(0, ordered[0].BoundingBox.Left);
            Assert.Equal(100, ordered[1].BoundingBox.Left);
        }


        [Fact]
        public void DocumentTest()
        {
            var title = CreateFakeTextBlock(new PdfRectangle(new PdfPoint(x: 42.6, y: 709.06), new PdfPoint(x: 42.6, y: 709.06)));
            var line1_Left = CreateFakeTextBlock(new PdfRectangle(new PdfPoint(x: 42.6, y: 668.86), new PdfPoint(x: 42.6, y: 668.86)));
            var line1_Right = CreateFakeTextBlock(new PdfRectangle(new PdfPoint(x: 302.21, y: 668.86), new PdfPoint(x: 302.21, y: 668.86)));
            var line2_Left = CreateFakeTextBlock(new PdfRectangle(new PdfPoint(x: 42.6, y: 608.26), new PdfPoint(x: 42.6, y: 608.26)));
            var line2_Taller_Right = CreateFakeTextBlock(new PdfRectangle(new PdfPoint(x: 302.21, y: 581.35), new PdfPoint(x: 302.21, y: 581.35)));
            var line3 = CreateFakeTextBlock(new PdfRectangle(new PdfPoint(x: 42.6, y: 515.83), new PdfPoint(x: 42.6, y: 515.83)));
            var line4_left = CreateFakeTextBlock(new PdfRectangle(new PdfPoint(x: 42.6, y: 490.27), new PdfPoint(x: 42.6, y: 490.27)));
            var line4_right = CreateFakeTextBlock(new PdfRectangle(new PdfPoint(x: 302.21, y: 491.59), new PdfPoint(x: 302.21, y: 491.59)));

            // We deliberately submit in the wrong order
            var textBlocks = new List<TextBlock>() { title, line4_left, line2_Taller_Right, line4_right, line1_Right, line1_Left, line3, line2_Left };

            var unsupervisedReadingOrderDetector = new UnsupervisedReadingOrderDetector(5, UnsupervisedReadingOrderDetector.SpatialReasoningRules.RowWise);
            var orderedBlocks = unsupervisedReadingOrderDetector.Get(textBlocks);

            var ordered = orderedBlocks.OrderBy(x => x.ReadingOrder).ToList();
            Assert.Equal(title.BoundingBox, ordered[0].BoundingBox);
            Assert.Equal(line1_Left.BoundingBox, ordered[1].BoundingBox);
            Assert.Equal(line1_Right.BoundingBox, ordered[2].BoundingBox);
            Assert.Equal(line2_Left.BoundingBox, ordered[3].BoundingBox);
            Assert.Equal(line2_Taller_Right.BoundingBox, ordered[4].BoundingBox);
            Assert.Equal(line3.BoundingBox, ordered[5].BoundingBox);
            Assert.Equal(line4_left.BoundingBox, ordered[6].BoundingBox);
            Assert.Equal(line4_right.BoundingBox, ordered[7].BoundingBox);
        }

        private static TextBlock CreateFakeTextBlock(PdfRectangle boundingBox)
        {
            var letter = new Letter("a",
                boundingBox,
                boundingBox.BottomLeft,
                boundingBox.BottomRight,
                10, 1, null, TextRenderingMode.NeitherClip, null, null, 0, 0);// These don't matter
            var leftTextBlock = new TextBlock(new[] { new TextLine(new[] { new Word(new[] { letter }) }) });
            return leftTextBlock;
        }
    }
}
