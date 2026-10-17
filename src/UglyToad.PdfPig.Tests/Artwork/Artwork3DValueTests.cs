namespace UglyToad.PdfPig.Tests.Artwork
{
    using UglyToad.PdfPig.Annotations;
    using UglyToad.PdfPig.Artwork;
    using UglyToad.PdfPig.Tokens;

    public class Artwork3DValueTests
    {
        [Fact]
        public void RichMediaSubtypeMapsToRichMedia()
        {
            Assert.Equal(AnnotationType.RichMedia, NameToken.Create("RichMedia").ToAnnotationType());
        }

        [Fact]
        public void ExistingAnnotationTypeValuesAreUnchanged()
        {
            Assert.Equal(24, (int)AnnotationType.Artwork3D);
            Assert.Equal(25, (int)AnnotationType.Other);
            Assert.Equal(26, (int)AnnotationType.RichMedia);
        }

        [Fact]
        public void MatrixKeepsFileOrder()
        {
            var m = new Artwork3DMatrix(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);

            Assert.Equal(1, m.A);
            Assert.Equal(9, m.I);
            Assert.Equal(10, m.Tx);
            Assert.Equal(12, m.Tz);

            for (var i = 0; i < 12; i++)
            {
                Assert.Equal(i + 1, m[i]);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => m[12]);
        }

        [Fact]
        public void IdentityMatrixLeavesAxesAlone()
        {
            var m = Artwork3DMatrix.Identity;

            Assert.Equal(1, m.A);
            Assert.Equal(1, m.E);
            Assert.Equal(1, m.I);
            Assert.Equal(0, m.B + m.C + m.D + m.F + m.G + m.H + m.Tx + m.Ty + m.Tz);
        }
    }
}
