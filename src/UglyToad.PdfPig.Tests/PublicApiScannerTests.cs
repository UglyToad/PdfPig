﻿namespace UglyToad.PdfPig.Tests
{
    using System.Reflection;
    using PdfPig.Graphics.Operations;

    public class PublicApiScannerTests
    {
        [Fact]
        public void OnlyExposedApiIsPublic()
        {
            var assembly = typeof(PdfDocument).Assembly;

            var types = assembly.GetTypes();

            var publicTypeNames = new List<string>();

            foreach (var type in types)
            {
                if (type.FullName == null)
                {
                    continue;
                }

                // Skip coverage measuring instrumentation classes.
                if (type.FullName.StartsWith("Coverlet", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (type.IsPublic)
                {
                    publicTypeNames.Add(type.FullName);
                }
            }

            var expected = new List<string>
            {
                "UglyToad.PdfPig.AcroForms.AcroForm",
                "UglyToad.PdfPig.AcroForms.AcroFormExtensions",
                "UglyToad.PdfPig.AcroForms.SignatureFlags",
                "UglyToad.PdfPig.AcroForms.Fields.AcroButtonFieldFlags",
                "UglyToad.PdfPig.AcroForms.Fields.AcroCheckboxField",
                "UglyToad.PdfPig.AcroForms.Fields.AcroCheckboxesField",
                "UglyToad.PdfPig.AcroForms.Fields.AcroChoiceFieldFlags",
                "UglyToad.PdfPig.AcroForms.Fields.AcroChoiceOption",
                "UglyToad.PdfPig.AcroForms.Fields.AcroComboBoxField",
                "UglyToad.PdfPig.AcroForms.Fields.AcroFieldBase",
                "UglyToad.PdfPig.AcroForms.Fields.AcroFieldCommonInformation",
                "UglyToad.PdfPig.AcroForms.Fields.AcroFieldType",
                "UglyToad.PdfPig.AcroForms.Fields.AcroListBoxField",
                "UglyToad.PdfPig.AcroForms.Fields.AcroNonTerminalField",
                "UglyToad.PdfPig.AcroForms.Fields.AcroPushButtonField",
                "UglyToad.PdfPig.AcroForms.Fields.AcroRadioButtonField",
                "UglyToad.PdfPig.AcroForms.Fields.AcroRadioButtonsField",
                "UglyToad.PdfPig.AcroForms.Fields.AcroSignatureField",
                "UglyToad.PdfPig.AcroForms.Fields.AcroTextField",
                "UglyToad.PdfPig.AcroForms.Fields.AcroTextFieldFlags",
                "UglyToad.PdfPig.Actions.AbstractGoToAction",
                "UglyToad.PdfPig.Actions.PdfAction",
                "UglyToad.PdfPig.Actions.ActionType",
                "UglyToad.PdfPig.Actions.GoToAction",
                "UglyToad.PdfPig.Actions.GoToEAction",
                "UglyToad.PdfPig.Actions.GoToRAction",
                "UglyToad.PdfPig.Actions.UriAction",
                "UglyToad.PdfPig.AdvancedPdfDocumentAccess",
                "UglyToad.PdfPig.Annotations.Annotation",
                "UglyToad.PdfPig.Annotations.AnnotationBorder",
                "UglyToad.PdfPig.Annotations.AnnotationFlags",
                "UglyToad.PdfPig.Annotations.AnnotationType",
                "UglyToad.PdfPig.Annotations.AppearanceStream",
                "UglyToad.PdfPig.Annotations.AnnotationProvider",
                "UglyToad.PdfPig.Annotations.QuadPointsQuadrilateral",
                "UglyToad.PdfPig.Content.ArtifactMarkedContentElement",
                "UglyToad.PdfPig.Content.BasePageFactory`1",
                "UglyToad.PdfPig.Content.Catalog",
                "UglyToad.PdfPig.Content.CropBox",
                "UglyToad.PdfPig.Content.DocumentInformation",
                "UglyToad.PdfPig.Content.EmbeddedFile",
                "UglyToad.PdfPig.Content.Hyperlink",
                "UglyToad.PdfPig.Content.InlineImage",
                "UglyToad.PdfPig.Content.IPageFactory`1",
                "UglyToad.PdfPig.Content.IPdfImage",
                "UglyToad.PdfPig.Content.IResourceStore",
                "UglyToad.PdfPig.Content.Letter",
                "UglyToad.PdfPig.Content.MarkedContentElement",
                "UglyToad.PdfPig.Content.MediaBox",
                "UglyToad.PdfPig.Content.OptionalContentGroupElement",
                "UglyToad.PdfPig.Content.Page",
                "UglyToad.PdfPig.Content.PageRotationDegrees",
                "UglyToad.PdfPig.Content.PageSize",
                "UglyToad.PdfPig.Content.PageTreeMembers",
                "UglyToad.PdfPig.Content.PageTreeNode",
                "UglyToad.PdfPig.Content.Word",
                "UglyToad.PdfPig.Content.TextOrientation",
                "UglyToad.PdfPig.Content.XmpMetadata",
                "UglyToad.PdfPig.CrossReference.CrossReferenceTable",
                "UglyToad.PdfPig.CrossReference.CrossReferenceType",
                "UglyToad.PdfPig.CrossReference.TrailerDictionary",
                "UglyToad.PdfPig.Exceptions.PdfDocumentEncryptedException",
                "UglyToad.PdfPig.Filters.DefaultFilterProvider",
                "UglyToad.PdfPig.Filters.IFilter",
                "UglyToad.PdfPig.Filters.IFilterProvider",
                "UglyToad.PdfPig.Filters.ILookupFilterProvider",
                "UglyToad.PdfPig.Functions.FunctionTypes",
                "UglyToad.PdfPig.Functions.PdfFunction",
                "UglyToad.PdfPig.PdfFonts.CharacterBoundingBox",
                "UglyToad.PdfPig.PdfFonts.DescriptorFontFile",
                "UglyToad.PdfPig.PdfFonts.FontDescriptor",
                "UglyToad.PdfPig.PdfFonts.FontDescriptorFlags",
                "UglyToad.PdfPig.PdfFonts.FontDetails",
                "UglyToad.PdfPig.PdfFonts.FontStretch",
                "UglyToad.PdfPig.PdfFonts.IFont",
                "UglyToad.PdfPig.Geometry.GeometryExtensions",
                "UglyToad.PdfPig.Geometry.UserSpaceUnit",
                "UglyToad.PdfPig.Graphics.BaseStreamProcessor`1",
                "UglyToad.PdfPig.Graphics.Colors.CMYKColor",
                "UglyToad.PdfPig.Graphics.Colors.ColorSpace",
                "UglyToad.PdfPig.Graphics.PdfPath",
                "UglyToad.PdfPig.Graphics.Colors.ResourceColorSpace",
                "UglyToad.PdfPig.Graphics.Colors.ColorSpaceExtensions",
                "UglyToad.PdfPig.Graphics.Colors.ColorSpaceFamily",
                "UglyToad.PdfPig.Graphics.Colors.GrayColor",
                "UglyToad.PdfPig.Graphics.Colors.IColor",
                "UglyToad.PdfPig.Graphics.Colors.RGBColor",
                "UglyToad.PdfPig.Graphics.Colors.PatternColor",
                "UglyToad.PdfPig.Graphics.Colors.TilingPatternColor",
                "UglyToad.PdfPig.Graphics.Colors.ShadingPatternColor",
                "UglyToad.PdfPig.Graphics.Colors.PatternType",
                "UglyToad.PdfPig.Graphics.Colors.PatternPaintType",
                "UglyToad.PdfPig.Graphics.Colors.PatternTilingType",
                "UglyToad.PdfPig.Graphics.Colors.ColorSpaceDetails",
                "UglyToad.PdfPig.Graphics.Colors.CalGrayColorSpaceDetails",
                "UglyToad.PdfPig.Graphics.Colors.CalRGBColorSpaceDetails",
                "UglyToad.PdfPig.Graphics.Colors.DeviceGrayColorSpaceDetails",
                "UglyToad.PdfPig.Graphics.Colors.DeviceRgbColorSpaceDetails",
                "UglyToad.PdfPig.Graphics.Colors.DeviceCmykColorSpaceDetails",
                "UglyToad.PdfPig.Graphics.Colors.DeviceNColorSpaceDetails",
                "UglyToad.PdfPig.Graphics.Colors.ICCBasedColorSpaceDetails",
                "UglyToad.PdfPig.Graphics.Colors.IndexedColorSpaceDetails",
                "UglyToad.PdfPig.Graphics.Colors.LabColorSpaceDetails",
                "UglyToad.PdfPig.Graphics.Colors.PatternColorSpaceDetails",
                "UglyToad.PdfPig.Graphics.Colors.SeparationColorSpaceDetails",
                "UglyToad.PdfPig.Graphics.Colors.UnsupportedColorSpaceDetails",
                "UglyToad.PdfPig.Graphics.Colors.Shading",
                "UglyToad.PdfPig.Graphics.Colors.ShadingType",
                "UglyToad.PdfPig.Graphics.Colors.FunctionBasedShading",
                "UglyToad.PdfPig.Graphics.Colors.AxialShading",
                "UglyToad.PdfPig.Graphics.Colors.RadialShading",
                "UglyToad.PdfPig.Graphics.Colors.FreeFormGouraudShading",
                "UglyToad.PdfPig.Graphics.Colors.LatticeFormGouraudShading",
                "UglyToad.PdfPig.Graphics.Colors.CoonsPatchMeshesShading",
                "UglyToad.PdfPig.Graphics.Colors.TensorProductPatchMeshesShading",
                "UglyToad.PdfPig.Graphics.Core.LineCapStyle",
                "UglyToad.PdfPig.Graphics.Core.LineDashPattern",
                "UglyToad.PdfPig.Graphics.Core.LineJoinStyle",
                "UglyToad.PdfPig.Graphics.Core.RenderingIntent",
                "UglyToad.PdfPig.Graphics.CurrentFontState",
                "UglyToad.PdfPig.Graphics.CurrentGraphicsState",
                "UglyToad.PdfPig.Graphics.IColorSpaceContext",
                "UglyToad.PdfPig.Graphics.InlineImageBuilder",
                "UglyToad.PdfPig.Graphics.IOperationContext",
                "UglyToad.PdfPig.Graphics.Operations.ClippingPaths.ModifyClippingByEvenOddIntersect",
                "UglyToad.PdfPig.Graphics.Operations.ClippingPaths.ModifyClippingByNonZeroWindingIntersect",
                "UglyToad.PdfPig.Graphics.Operations.Compatibility.BeginCompatibilitySection",
                "UglyToad.PdfPig.Graphics.Operations.Compatibility.EndCompatibilitySection",
                "UglyToad.PdfPig.Graphics.Operations.PathPainting.CloseAndStrokePath",
                "UglyToad.PdfPig.Graphics.Operations.PathPainting.CloseFillPathEvenOddRuleAndStroke",
                "UglyToad.PdfPig.Graphics.Operations.PathPainting.CloseFillPathNonZeroWindingAndStroke",
                "UglyToad.PdfPig.Graphics.Operations.PathPainting.EndPath",
                "UglyToad.PdfPig.Graphics.Operations.PathPainting.FillPathEvenOddRule",
                "UglyToad.PdfPig.Graphics.Operations.PathPainting.FillPathEvenOddRuleAndStroke",
                "UglyToad.PdfPig.Graphics.Operations.PathPainting.FillPathNonZeroWinding",
                "UglyToad.PdfPig.Graphics.Operations.PathPainting.FillPathNonZeroWindingAndStroke",
                "UglyToad.PdfPig.Graphics.Operations.PathPainting.FillPathNonZeroWindingCompatibility",
                "UglyToad.PdfPig.Graphics.Operations.PathPainting.StrokePath",
                "UglyToad.PdfPig.Graphics.Operations.General.SetColorRenderingIntent",
                "UglyToad.PdfPig.Graphics.Operations.General.SetFlatnessTolerance",
                "UglyToad.PdfPig.Graphics.Operations.General.SetLineCap",
                "UglyToad.PdfPig.Graphics.Operations.General.SetLineDashPattern",
                "UglyToad.PdfPig.Graphics.Operations.General.SetLineJoin",
                "UglyToad.PdfPig.Graphics.Operations.General.SetLineWidth",
                "UglyToad.PdfPig.Graphics.Operations.General.SetMiterLimit",
                "UglyToad.PdfPig.Graphics.Operations.IGraphicsStateOperation",
                "UglyToad.PdfPig.Graphics.Operations.InlineImages.BeginInlineImage",
                "UglyToad.PdfPig.Graphics.Operations.InlineImages.BeginInlineImageData",
                "UglyToad.PdfPig.Graphics.Operations.InlineImages.EndInlineImage",
                "UglyToad.PdfPig.Graphics.Operations.InvokeNamedXObject",
                "UglyToad.PdfPig.Graphics.Operations.MarkedContent.BeginMarkedContent",
                "UglyToad.PdfPig.Graphics.Operations.MarkedContent.BeginMarkedContentWithProperties",
                "UglyToad.PdfPig.Graphics.Operations.MarkedContent.DesignateMarkedContentPoint",
                "UglyToad.PdfPig.Graphics.Operations.MarkedContent.DesignateMarkedContentPointWithProperties",
                "UglyToad.PdfPig.Graphics.Operations.MarkedContent.EndMarkedContent",
                "UglyToad.PdfPig.Graphics.Operations.PaintShading",
                "UglyToad.PdfPig.Graphics.Operations.PathConstruction.AppendDualControlPointBezierCurve",
                "UglyToad.PdfPig.Graphics.Operations.PathConstruction.AppendEndControlPointBezierCurve",
                "UglyToad.PdfPig.Graphics.Operations.PathConstruction.AppendRectangle",
                "UglyToad.PdfPig.Graphics.Operations.PathConstruction.AppendStartControlPointBezierCurve",
                "UglyToad.PdfPig.Graphics.Operations.PathConstruction.AppendStraightLineSegment",
                "UglyToad.PdfPig.Graphics.Operations.PathConstruction.BeginNewSubpath",
                "UglyToad.PdfPig.Graphics.Operations.PathConstruction.CloseSubpath",
                "UglyToad.PdfPig.Graphics.Operations.SetNonStrokeColor",
                "UglyToad.PdfPig.Graphics.Operations.SetNonStrokeColorAdvanced",
                "UglyToad.PdfPig.Graphics.Operations.SetNonStrokeColorDeviceCmyk",
                "UglyToad.PdfPig.Graphics.Operations.SetNonStrokeColorDeviceGray",
                "UglyToad.PdfPig.Graphics.Operations.SetNonStrokeColorDeviceRgb",
                "UglyToad.PdfPig.Graphics.Operations.SetNonStrokeColorSpace",
                "UglyToad.PdfPig.Graphics.Operations.SetStrokeColorDeviceCmyk",
                "UglyToad.PdfPig.Graphics.Operations.SetStrokeColor",
                "UglyToad.PdfPig.Graphics.Operations.SetStrokeColorAdvanced",
                "UglyToad.PdfPig.Graphics.Operations.SetStrokeColorDeviceGray",
                "UglyToad.PdfPig.Graphics.Operations.SetStrokeColorDeviceRgb",
                "UglyToad.PdfPig.Graphics.Operations.SetStrokeColorSpace",
                "UglyToad.PdfPig.Graphics.Operations.SpecialGraphicsState.ModifyCurrentTransformationMatrix",
                "UglyToad.PdfPig.Graphics.Operations.SpecialGraphicsState.Pop",
                "UglyToad.PdfPig.Graphics.Operations.SpecialGraphicsState.Push",
                "UglyToad.PdfPig.Graphics.Operations.SpecialGraphicsState.SetGraphicsStateParametersFromDictionary",
                "UglyToad.PdfPig.Graphics.Operations.TextObjects.BeginText",
                "UglyToad.PdfPig.Graphics.Operations.TextObjects.EndText",
                "UglyToad.PdfPig.Graphics.Operations.TextPositioning.MoveToNextLine",
                "UglyToad.PdfPig.Graphics.Operations.TextPositioning.MoveToNextLineWithOffset",
                "UglyToad.PdfPig.Graphics.Operations.TextPositioning.MoveToNextLineWithOffsetSetLeading",
                "UglyToad.PdfPig.Graphics.Operations.TextPositioning.SetTextMatrix",
                "UglyToad.PdfPig.Graphics.Operations.TextShowing.MoveToNextLineShowText",
                "UglyToad.PdfPig.Graphics.Operations.TextShowing.MoveToNextLineShowTextWithSpacing",
                "UglyToad.PdfPig.Graphics.Operations.TextShowing.ShowText",
                "UglyToad.PdfPig.Graphics.Operations.TextShowing.ShowTextsWithPositioning",
                "UglyToad.PdfPig.Graphics.Operations.TextState.SetCharacterSpacing",
                "UglyToad.PdfPig.Graphics.Operations.TextState.SetFontAndSize",
                "UglyToad.PdfPig.Graphics.Operations.TextState.SetHorizontalScaling",
                "UglyToad.PdfPig.Graphics.Operations.TextState.SetTextLeading",
                "UglyToad.PdfPig.Graphics.Operations.TextState.SetTextRenderingMode",
                "UglyToad.PdfPig.Graphics.Operations.TextState.SetTextRise",
                "UglyToad.PdfPig.Graphics.Operations.TextState.SetWordSpacing",
                "UglyToad.PdfPig.Graphics.Operations.TextState.Type3SetGlyphWidth",
                "UglyToad.PdfPig.Graphics.Operations.TextState.Type3SetGlyphWidthAndBoundingBox",
                "UglyToad.PdfPig.Graphics.PerformantRectangleTransformer",
                "UglyToad.PdfPig.Graphics.TextMatrices",
                "UglyToad.PdfPig.Graphics.XObjectContentRecord",
                "UglyToad.PdfPig.Images.ColorSpaceDetailsByteConverter",
                "UglyToad.PdfPig.Logging.ILog",
                "UglyToad.PdfPig.Outline.Bookmarks",
                "UglyToad.PdfPig.Outline.BookmarkNode",
                "UglyToad.PdfPig.Outline.DocumentBookmarkNode",
                "UglyToad.PdfPig.Outline.EmbeddedBookmarkNode",
                "UglyToad.PdfPig.Outline.ExternalBookmarkNode",
                "UglyToad.PdfPig.Outline.UriBookmarkNode",
                "UglyToad.PdfPig.Outline.Destinations.ExplicitDestination",
                "UglyToad.PdfPig.Outline.Destinations.ExplicitDestinationCoordinates",
                "UglyToad.PdfPig.Outline.Destinations.ExplicitDestinationType",
                "UglyToad.PdfPig.Outline.Destinations.NamedDestinations",
                "UglyToad.PdfPig.ParsingOptions",
                "UglyToad.PdfPig.Parser.IPageContentParser",
                "UglyToad.PdfPig.Parser.Parts.DirectObjectFinder",
                "UglyToad.PdfPig.PdfDocument",
                "UglyToad.PdfPig.PdfExtensions",
                "UglyToad.PdfPig.Rendering.IPageImageRenderer",
                "UglyToad.PdfPig.Rendering.PdfRendererImageFormat",
                "UglyToad.PdfPig.Structure",
                "UglyToad.PdfPig.Tokenization.Scanner.IPdfTokenScanner",
                "UglyToad.PdfPig.Util.Adler32Checksum",
                "UglyToad.PdfPig.Util.ArrayTokenExtensions",
                "UglyToad.PdfPig.Util.IWordExtractor",
                "UglyToad.PdfPig.Util.DefaultWordExtractor",
                "UglyToad.PdfPig.Util.DateFormatHelper",
                "UglyToad.PdfPig.Util.DictionaryTokenExtensions",
                "UglyToad.PdfPig.Util.WhitespaceSizeStatistics",
                "UglyToad.PdfPig.Writer.ITokenWriter",
                "UglyToad.PdfPig.Writer.PdfAStandard",
                "UglyToad.PdfPig.Writer.PdfDocumentBuilder",
                "UglyToad.PdfPig.Writer.PdfMerger",
                "UglyToad.PdfPig.Writer.PdfTextRemover",
                "UglyToad.PdfPig.Writer.PdfWriterType",
                "UglyToad.PdfPig.Writer.PdfPageBuilder",
                "UglyToad.PdfPig.Writer.TokenWriter",
                "UglyToad.PdfPig.XObjects.XObjectFactory",
                "UglyToad.PdfPig.XObjects.XObjectImage",
                "UglyToad.PdfPig.XObjects.XObjectType"
            };

            foreach (var publicTypeName in publicTypeNames)
            {
                Assert.True(expected.Contains(publicTypeName), $"Type should not be public: {publicTypeName}.");
            }

            foreach (var expectedPublicType in expected)
            {
                Assert.True(publicTypeNames.Contains(expectedPublicType), $"Type should be public: {expectedPublicType}.");
            }
        }

        [Fact]
        public void AllSpecificationOperatorsArePresent()
        {
            var assembly = typeof(PdfDocument).Assembly;

            var types = assembly.GetTypes().Where(x => typeof(IGraphicsStateOperation).IsAssignableFrom(x) && !x.IsInterface);

            var expectedSymbols = new[]
            {
                "b",
                "B",
                "b*",
                "B*",
                "BDC",
                "BI",
                "BMC",
                "BT",
                "BX",
                "c",
                "cm",
                "CS",
                "cs",
                "d",
                "d0",
                "d1",
                "Do",
                "DP",
                "EI",
                "EMC",
                "ET",
                "EX",
                "f",
                "F",
                "f*",
                "G",
                "g",
                "gs",
                "h",
                "i",
                "ID",
                "j",
                "J",
                "K",
                "k",
                "l",
                "m",
                "M",
                "MP",
                "n",
                "q",
                "Q",
                "re",
                "RG",
                "rg",
                "ri",
                "s",
                "S",
                "SC",
                "sc",
                "SCN",
                "scn",
                "sh",
                "T*",
                "Tc",
                "Td",
                "TD",
                "Tf",
                "Tj",
                "TJ",
                "TL",
                "Tm",
                "Tr",
                "Ts",
                "Tw",
                "Tz",
                "v",
                "w",
                "W",
                "W*",
                "y",
                "'",
                "\""
            };

            var symbols = new List<string>();

            foreach (var type in types)
            {
                var symbol = type.GetField("Symbol", BindingFlags.Public | BindingFlags.Static);
                if (symbol == null)
                {
                    continue;
                }

                symbols.Add(symbol.GetValue(null).ToString());
            }

            foreach (var expectedSymbol in expectedSymbols)
            {
                Assert.True(symbols.Contains(expectedSymbol), $"There is no operation defined with the symbol: {expectedSymbol}.");
            }
        }
    }
}
