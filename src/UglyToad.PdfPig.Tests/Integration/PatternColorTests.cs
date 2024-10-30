namespace UglyToad.PdfPig.Tests.Integration
{
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Graphics.Colors;

    public class PatternColorTests
    {
        [Fact]
        public void ShadingPattern1()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("cat-genetics_bobld.pdf")))
            {
                var page = document.GetPage(1);

                var annotationStamp = page.GetAnnotations().ElementAt(14);
                Assert.Equal(Annotations.AnnotationType.Stamp, annotationStamp.Type);
                Assert.True(annotationStamp.HasNormalAppearance);

                var appearance = annotationStamp.normalAppearanceStream;
                // TODO - load color space in annotation appearance
                // TODO - contains function with indirect reference
            }
        }

        [Fact]
        public void ShadingPattern2()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("output_w3c_csswg_drafts_issues2023.pdf")))
            {
                var page = document.GetPage(1);
                var path = page.Paths.Single();
                var color = path.FillColor;
                Assert.Equal(ColorSpace.Pattern, color.ColorSpace);

                var patternColor = color as PatternColor;
                Assert.Equal(PatternType.Shading, patternColor.PatternType);
                Assert.NotNull(patternColor.PatternDictionary);

                var shadingColor = patternColor as ShadingPatternColor;
                Assert.NotNull(shadingColor.Shading);

                Assert.Equal(ColorSpace.DeviceN, shadingColor.Shading.ColorSpace.Type);

                var deviceNCs = shadingColor.Shading.ColorSpace as DeviceNColorSpaceDetails;
                Assert.NotNull(deviceNCs);
                Assert.Equal(2, deviceNCs.Names.Count);
                Assert.Contains("PANTONE Reflex Blue C", deviceNCs.Names.Select(n => n.Data));
                Assert.Contains("PANTONE Warm Red C", deviceNCs.Names.Select(n => n.Data));
            }
        }

        [Fact]
        public void TillingPattern1()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("22060_A1_01_Plans-1.pdf")))
            {
                var page = document.GetPage(1);
                var filledPath = page.Paths.Where(p => p.IsFilled).ToArray();
                var pattern = filledPath[0].FillColor;
                Assert.Equal(ColorSpace.Pattern, pattern.ColorSpace);

                var patternColor = pattern as PatternColor;
                Assert.Equal(PatternType.Tiling, patternColor.PatternType);
                Assert.Equal(0.213333, patternColor.Matrix[0, 0]);
                Assert.Equal(0.0, patternColor.Matrix[0, 1]);
                Assert.Equal(0.0, patternColor.Matrix[0, 2]);

                Assert.Equal(0.0, patternColor.Matrix[1, 0]);
                Assert.Equal(0.213333, patternColor.Matrix[1, 1]);
                Assert.Equal(0.0, patternColor.Matrix[1, 2]);

                Assert.Equal(-0.231058, patternColor.Matrix[2, 0]);
                Assert.Equal(1190.67, patternColor.Matrix[2, 1]);
                Assert.Equal(1.0, patternColor.Matrix[2, 2]);

                Assert.Null(patternColor.ExtGState);
                Assert.NotNull(patternColor.PatternDictionary);

                var tillingColor = patternColor as TilingPatternColor;
                Assert.NotNull(tillingColor.PatternStream);
                Assert.Equal(1897.47, tillingColor.XStep);
                Assert.Equal(2012.23, tillingColor.YStep);
                Assert.Equal(142, tillingColor.Data.Length);

                Assert.Equal(new PdfPoint(-18.6026, -1992.51), tillingColor.BBox.BottomLeft);
                Assert.Equal(new PdfPoint(1878.86, 19.7278), tillingColor.BBox.TopRight);
                Assert.Equal(PatternPaintType.Coloured, tillingColor.PaintType);
                Assert.Equal(PatternTilingType.ConstantSpacing, tillingColor.TilingType);
                Assert.NotNull(tillingColor.Resources);
                Assert.Equal(4, tillingColor.Resources.Data.Count);
            }
        }

        [Fact]
        public void TillingPattern2()
        {
            // 53
            // 307
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("SPARC - v9 Architecture Manual.pdf")))
            {
                // page 53
                var page = document.GetPage(53);
                var strokedPath = page.Paths.Where(p => p.StrokeColor?.ColorSpace == ColorSpace.Pattern).ToArray();
                Assert.Equal(5, strokedPath.Length);
                foreach (var p in strokedPath)
                {
                    Assert.Equal(ColorSpace.Pattern, p.StrokeColor.ColorSpace);
                    var patternColor = p.StrokeColor as PatternColor;
                    Assert.Equal(PatternType.Tiling, patternColor.PatternType);
                    var tillingColor = patternColor as TilingPatternColor;
                    Assert.Equal(PatternPaintType.Uncoloured, tillingColor.PaintType);
                    Assert.Equal(PatternTilingType.ConstantSpacingFasterTiling, tillingColor.TilingType);
                }

                // page 307
                page = document.GetPage(307);
                strokedPath = page.Paths.Where(p => p.StrokeColor?.ColorSpace == ColorSpace.Pattern).ToArray();
                Assert.Equal(2, strokedPath.Length);
                foreach (var p in strokedPath)
                {
                    Assert.Equal(ColorSpace.Pattern, p.StrokeColor.ColorSpace);
                    var patternColor = p.StrokeColor as PatternColor;
                    Assert.Equal(PatternType.Tiling, patternColor.PatternType);
                    var tillingColor = patternColor as TilingPatternColor;
                    Assert.Equal(PatternPaintType.Uncoloured, tillingColor.PaintType);
                    Assert.Equal(PatternTilingType.ConstantSpacingFasterTiling, tillingColor.TilingType);
                }
            }
        }
    }
}
