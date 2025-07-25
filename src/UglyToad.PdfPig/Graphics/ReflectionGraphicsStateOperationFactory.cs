namespace UglyToad.PdfPig.Graphics
{
    using Operations;
    using Operations.ClippingPaths;
    using Operations.Compatibility;
    using Operations.General;
    using Operations.InlineImages;
    using Operations.MarkedContent;
    using Operations.PathConstruction;
    using Operations.PathPainting;
    using Operations.SpecialGraphicsState;
    using Operations.TextObjects;
    using Operations.TextPositioning;
    using Operations.TextShowing;
    using Operations.TextState;
    using PdfPig.Core;
    using System;
#if NET8_0_OR_GREATER
    using System.Collections.Frozen;
#endif
    using System.Collections.Generic;
    using System.Linq;
    using Tokens;

    /// <summary>
    /// Reflection based graphics state operation factory.
    /// </summary>
    public sealed class ReflectionGraphicsStateOperationFactory : IGraphicsStateOperationFactory
    {
        /// <summary>
        /// The single instance of the <see cref="ReflectionGraphicsStateOperationFactory"/>.
        /// </summary>
        public static readonly ReflectionGraphicsStateOperationFactory Instance = new ReflectionGraphicsStateOperationFactory();

        private ReflectionGraphicsStateOperationFactory()
        {
            // private
        }
        
        private static readonly IReadOnlyDictionary<string, Type> Operations =
            new Dictionary<string, Type>
            {
                { SetStrokeColorAdvanced.Symbol, typeof(SetStrokeColorAdvanced) },
                { SetStrokeColorSpace.Symbol, typeof(SetStrokeColorSpace) },
                { SetCharacterSpacing.Symbol, typeof(SetCharacterSpacing) },
                { ModifyCurrentTransformationMatrix.Symbol, typeof(ModifyCurrentTransformationMatrix) },
                { SetStrokeColorDeviceCmyk.Symbol, typeof(SetStrokeColorDeviceCmyk) },
                { EndCompatibilitySection.Symbol, typeof(EndCompatibilitySection) },
                { CloseFillPathEvenOddRuleAndStroke.Symbol, typeof(CloseFillPathEvenOddRuleAndStroke) },
                { SetStrokeColor.Symbol, typeof(SetStrokeColor) },
                { SetLineDashPattern.Symbol, typeof(SetLineDashPattern) },
                { DesignateMarkedContentPointWithProperties.Symbol, typeof(DesignateMarkedContentPointWithProperties) },
                { SetStrokeColorDeviceRgb.Symbol, typeof(SetStrokeColorDeviceRgb) },
                { BeginMarkedContent.Symbol, typeof(BeginMarkedContent) },
                { BeginNewSubpath.Symbol, typeof(BeginNewSubpath) },
                { EndMarkedContent.Symbol, typeof(EndMarkedContent) },
                { SetNonStrokeColorDeviceCmyk.Symbol, typeof(SetNonStrokeColorDeviceCmyk) },
                { InvokeNamedXObject.Symbol, typeof(InvokeNamedXObject) },
                { EndPath.Symbol, typeof(EndPath) },
                { SetGraphicsStateParametersFromDictionary.Symbol, typeof(SetGraphicsStateParametersFromDictionary) },
                { FillPathEvenOddRule.Symbol, typeof(FillPathEvenOddRule) },
                { Type3SetGlyphWidth.Symbol, typeof(Type3SetGlyphWidth) },
                { Push.Symbol, typeof(Push) },
                { Pop.Symbol, typeof(Pop) },
                { DesignateMarkedContentPoint.Symbol, typeof(DesignateMarkedContentPoint) },
                { SetNonStrokeColorAdvanced.Symbol, typeof(SetNonStrokeColorAdvanced) },
                { MoveToNextLineShowTextWithSpacing.Symbol, typeof(MoveToNextLineShowTextWithSpacing) },
                { SetHorizontalScaling.Symbol, typeof(SetHorizontalScaling) },
                { BeginCompatibilitySection.Symbol, typeof(BeginCompatibilitySection) },
                { SetFlatnessTolerance.Symbol, typeof(SetFlatnessTolerance) },
                { EndInlineImage.Symbol, typeof(EndInlineImage) },
                { MoveToNextLineWithOffset.Symbol, typeof(MoveToNextLineWithOffset) },
                { SetTextLeading.Symbol, typeof(SetTextLeading) },
                { BeginText.Symbol, typeof(BeginText) },
                { BeginMarkedContentWithProperties.Symbol, typeof(BeginMarkedContentWithProperties) },
                { AppendDualControlPointBezierCurve.Symbol, typeof(AppendDualControlPointBezierCurve) },
                { CloseFillPathNonZeroWindingAndStroke.Symbol, typeof(CloseFillPathNonZeroWindingAndStroke) },
                { StrokePath.Symbol, typeof(StrokePath) },
                { MoveToNextLine.Symbol, typeof(MoveToNextLine) },
                { ShowText.Symbol, typeof(ShowText) },
                { FillPathNonZeroWindingAndStroke.Symbol, typeof(FillPathNonZeroWindingAndStroke) },
                { AppendEndControlPointBezierCurve.Symbol, typeof(AppendEndControlPointBezierCurve) },
                { AppendStartControlPointBezierCurve.Symbol, typeof(AppendStartControlPointBezierCurve) },
                { SetNonStrokeColor.Symbol, typeof(SetNonStrokeColor) },
                { CloseAndStrokePath.Symbol, typeof(CloseAndStrokePath) },
                { BeginInlineImageData.Symbol, typeof(BeginInlineImageData) },
                { ModifyClippingByNonZeroWindingIntersect.Symbol, typeof(ModifyClippingByNonZeroWindingIntersect) },
                { MoveToNextLineShowText.Symbol, typeof(MoveToNextLineShowText) },
                { SetLineCap.Symbol, typeof(SetLineCap) },
                { FillPathNonZeroWinding.Symbol, typeof(FillPathNonZeroWinding) },
                { FillPathEvenOddRuleAndStroke.Symbol, typeof(FillPathEvenOddRuleAndStroke) },
                { SetFontAndSize.Symbol, typeof(SetFontAndSize) },
                { SetColorRenderingIntent.Symbol, typeof(SetColorRenderingIntent) },
                { PaintShading.Symbol, typeof(PaintShading) },
                { SetMiterLimit.Symbol, typeof(SetMiterLimit) },
                { AppendRectangle.Symbol, typeof(AppendRectangle) },
                { SetNonStrokeColorSpace.Symbol, typeof(SetNonStrokeColorSpace) },
                { MoveToNextLineWithOffsetSetLeading.Symbol, typeof(MoveToNextLineWithOffsetSetLeading) },
                { CloseSubpath.Symbol, typeof(CloseSubpath) },
                { SetStrokeColorDeviceGray.Symbol, typeof(SetStrokeColorDeviceGray) },
                { SetWordSpacing.Symbol, typeof(SetWordSpacing) },
                { BeginInlineImage.Symbol, typeof(BeginInlineImage) },
                { SetNonStrokeColorDeviceRgb.Symbol, typeof(SetNonStrokeColorDeviceRgb) },
                { SetTextMatrix.Symbol, typeof(SetTextMatrix) },
                { SetTextRise.Symbol, typeof(SetTextRise) },
                { Type3SetGlyphWidthAndBoundingBox.Symbol, typeof(Type3SetGlyphWidthAndBoundingBox) },
                { ModifyClippingByEvenOddIntersect.Symbol, typeof(ModifyClippingByEvenOddIntersect) },
                { AppendStraightLineSegment.Symbol, typeof(AppendStraightLineSegment) },
                { EndText.Symbol, typeof(EndText) },
                { FillPathNonZeroWindingCompatibility.Symbol, typeof(FillPathNonZeroWindingCompatibility) },
                { ShowTextsWithPositioning.Symbol, typeof(ShowTextsWithPositioning) },
                { SetLineJoin.Symbol, typeof(SetLineJoin) },
                { SetLineWidth.Symbol, typeof(SetLineWidth) },
                { SetNonStrokeColorDeviceGray.Symbol, typeof(SetNonStrokeColorDeviceGray) },
                { SetTextRenderingMode.Symbol, typeof(SetTextRenderingMode) },
            }
#if NET8_0_OR_GREATER
            .ToFrozenDictionary()
#endif
            ;

        private static double[] TokensToDoubleArray(IReadOnlyList<IToken> tokens, bool exceptLast = false)
        {
            using var result = new ArrayPoolBufferWriter<double>(16);

            for (var i = 0; i < tokens.Count - (exceptLast ? 1 : 0); i++)
            {
                var operand = tokens[i];

                if (operand is ArrayToken arr)
                {
                    for (var j = 0; j < arr.Length; j++)
                    {
                        var innerOperand = arr[j];

                        if (!(innerOperand is NumericToken innerNumeric))
                        {
                            return result.WrittenSpan.ToArray();
                        }

                        result.Write(innerNumeric.Data);
                    }
                }

                if (!(operand is NumericToken numeric))
                {
                    return result.WrittenSpan.ToArray();
                }

                result.Write(numeric.Data);
            }

            return result.WrittenSpan.ToArray();
        }

        private static int OperandToInt(IToken token)
        {
            if (!(token is NumericToken numeric))
            {
                throw new InvalidOperationException($"Invalid operand token encountered when expecting numeric: {token}.");
            }

            return numeric.Int;
        }

        private static double OperandToDouble(IToken token)
        {
            if (!(token is NumericToken numeric))
            {
                throw new InvalidOperationException($"Invalid operand token encountered when expecting numeric: {token}.");
            }

            return numeric.Data;
        }

        /// <inheritdoc/>
        public IGraphicsStateOperation? Create(OperatorToken op, IReadOnlyList<IToken> operands)
        {
            switch (op.Data)
            {
                case ModifyClippingByEvenOddIntersect.Symbol:
                    return ModifyClippingByEvenOddIntersect.Value;
                case ModifyClippingByNonZeroWindingIntersect.Symbol:
                    return ModifyClippingByNonZeroWindingIntersect.Value;
                case BeginCompatibilitySection.Symbol:
                    return BeginCompatibilitySection.Value;
                case EndCompatibilitySection.Symbol:
                    return EndCompatibilitySection.Value;
                case SetColorRenderingIntent.Symbol:
                    return new SetColorRenderingIntent((NameToken)operands[0]);
                case SetFlatnessTolerance.Symbol:
                    if (operands.Count == 0)
                    {
                        return null; // Should not happen by definition
                    }
                    return new SetFlatnessTolerance(OperandToDouble(operands[0]));
                case SetLineCap.Symbol:
                    return new SetLineCap(OperandToInt(operands[0]));
                case SetLineDashPattern.Symbol:
                    return new SetLineDashPattern(TokensToDoubleArray(operands, true), OperandToInt(operands[operands.Count - 1]));
                case SetLineJoin.Symbol:
                    return new SetLineJoin(OperandToInt(operands[0]));
                case SetLineWidth.Symbol:
                    return new SetLineWidth(OperandToDouble(operands[0]));
                case SetMiterLimit.Symbol:
                    return new SetMiterLimit(OperandToDouble(operands[0]));
                case AppendDualControlPointBezierCurve.Symbol:
                    if (operands.Count == 0)
                    {
                        return null;
                    }
                    return new AppendDualControlPointBezierCurve(OperandToDouble(operands[0]),
                        OperandToDouble(operands[1]),
                        OperandToDouble(operands[2]),
                        OperandToDouble(operands[3]),
                        OperandToDouble(operands[4]),
                        OperandToDouble(operands[5]));
                case AppendEndControlPointBezierCurve.Symbol:
                    if (operands.Count == 0)
                    {
                        return null;
                    }
                    return new AppendEndControlPointBezierCurve(OperandToDouble(operands[0]),
                        OperandToDouble(operands[1]),
                        OperandToDouble(operands[2]),
                        OperandToDouble(operands[3]));
                case AppendRectangle.Symbol:
                    if (operands.Count == 0)
                    {
                        return null;
                    }
                    return new AppendRectangle(OperandToDouble(operands[0]),
                        OperandToDouble(operands[1]),
                        OperandToDouble(operands[2]),
                        OperandToDouble(operands[3]));
                case AppendStartControlPointBezierCurve.Symbol:
                    if (operands.Count == 0)
                    {
                        return null;
                    }
                    return new AppendStartControlPointBezierCurve(OperandToDouble(operands[0]),
                        OperandToDouble(operands[1]),
                        OperandToDouble(operands[2]),
                        OperandToDouble(operands[3]));
                case AppendStraightLineSegment.Symbol:
                    return new AppendStraightLineSegment(OperandToDouble(operands[0]),
                        OperandToDouble(operands[1]));
                case BeginNewSubpath.Symbol:
                    return new BeginNewSubpath(OperandToDouble(operands[0]),
                        OperandToDouble(operands[1]));
                case CloseSubpath.Symbol:
                    return CloseSubpath.Value;
                case ModifyCurrentTransformationMatrix.Symbol:
                    return new ModifyCurrentTransformationMatrix(TokensToDoubleArray(operands));
                case Pop.Symbol:
                    return Pop.Value;
                case Push.Symbol:
                    return Push.Value;
                case SetGraphicsStateParametersFromDictionary.Symbol:
                    return new SetGraphicsStateParametersFromDictionary((NameToken)operands[0]);
                case BeginText.Symbol:
                    return BeginText.Value;
                case EndText.Symbol:
                    return EndText.Value;
                case SetCharacterSpacing.Symbol:
                    return new SetCharacterSpacing(OperandToDouble(operands[0]));
                case SetFontAndSize.Symbol:
                    return new SetFontAndSize((NameToken)operands[0], OperandToDouble(operands[1]));
                case SetHorizontalScaling.Symbol:
                    return new SetHorizontalScaling(OperandToDouble(operands[0]));
                case SetTextLeading.Symbol:
                    return new SetTextLeading(OperandToDouble(operands[0]));
                case SetTextRenderingMode.Symbol:
                    return new SetTextRenderingMode(OperandToInt(operands[0]));
                case SetTextRise.Symbol:
                    return new SetTextRise(OperandToDouble(operands[0]));
                case SetWordSpacing.Symbol:
                    return new SetWordSpacing(OperandToDouble(operands[0]));
                case CloseAndStrokePath.Symbol:
                    return CloseAndStrokePath.Value;
                case CloseFillPathEvenOddRuleAndStroke.Symbol:
                    return CloseFillPathEvenOddRuleAndStroke.Value;
                case CloseFillPathNonZeroWindingAndStroke.Symbol:
                    return CloseFillPathNonZeroWindingAndStroke.Value;
                case BeginInlineImage.Symbol:
                    return BeginInlineImage.Value;
                case BeginMarkedContent.Symbol:
                    return new BeginMarkedContent((NameToken)operands[0]);
                case BeginMarkedContentWithProperties.Symbol:
                    var bdcName = (NameToken)operands[0];
                    if (operands[1] is DictionaryToken contentSequenceDictionary)
                    {
                        return new BeginMarkedContentWithProperties(bdcName, contentSequenceDictionary);
                    }
                    else if (operands[1] is NameToken contentSequenceName)
                    {
                        return new BeginMarkedContentWithProperties(bdcName, contentSequenceName);
                    }

                    var errorMessageBdc = string.Join(", ", operands.Select(x => x.ToString()));
                    throw new PdfDocumentFormatException($"Attempted to set a marked-content sequence with invalid parameters: [{errorMessageBdc}]");
                case DesignateMarkedContentPoint.Symbol:
                    return new DesignateMarkedContentPoint((NameToken)operands[0]);
                case DesignateMarkedContentPointWithProperties.Symbol:
                    var dpName = (NameToken)operands[0];
                    if (operands[1] is DictionaryToken contentPointDictionary)
                    {
                        return new DesignateMarkedContentPointWithProperties(dpName, contentPointDictionary);
                    }
                    else if (operands[1] is NameToken contentPointName)
                    {
                        return new DesignateMarkedContentPointWithProperties(dpName, contentPointName);
                    }

                    var errorMessageDp = string.Join(", ", operands.Select(x => x.ToString()));
                    throw new PdfDocumentFormatException($"Attempted to set a marked-content point with invalid parameters: [{errorMessageDp}]");
                case EndMarkedContent.Symbol:
                    return EndMarkedContent.Value;
                case EndPath.Symbol:
                    return EndPath.Value;
                case FillPathEvenOddRule.Symbol:
                    return FillPathEvenOddRule.Value;
                case FillPathEvenOddRuleAndStroke.Symbol:
                    return FillPathEvenOddRuleAndStroke.Value;
                case FillPathNonZeroWinding.Symbol:
                    return FillPathNonZeroWinding.Value;
                case FillPathNonZeroWindingAndStroke.Symbol:
                    return FillPathNonZeroWindingAndStroke.Value;
                case FillPathNonZeroWindingCompatibility.Symbol:
                    return FillPathNonZeroWindingCompatibility.Value;
                case InvokeNamedXObject.Symbol:
                    return new InvokeNamedXObject((NameToken)operands[0]);
                case MoveToNextLine.Symbol:
                    return MoveToNextLine.Value;
                case MoveToNextLineShowText.Symbol:
                    if (operands.Count != 1)
                    {
                        throw new InvalidOperationException($"Attempted to create a move to next line and show text operation with {operands.Count} operands.");
                    }

                    if (operands[0] is StringToken snl)
                    {
                        return new MoveToNextLineShowText(snl.Data);
                    }

                    if (operands[0] is HexToken hnl)
                    {
                        return new MoveToNextLineShowText(hnl.Memory);
                    }

                    throw new InvalidOperationException($"Tried to create a move to next line and show text operation with operand type: {operands[0]?.GetType().Name ?? "null"}");
                case MoveToNextLineShowTextWithSpacing.Symbol:
                    var wordSpacing = (NumericToken)operands[0];
                    var charSpacing = (NumericToken)operands[1];
                    var text = operands[2];

                    if (text is StringToken stringToken)
                    {
                        return new MoveToNextLineShowTextWithSpacing(wordSpacing.Double, charSpacing.Double, stringToken.Data);
                    }

                    if (text is HexToken hexToken)
                    {
                        return new MoveToNextLineShowTextWithSpacing(wordSpacing.Double, charSpacing.Double, hexToken.Memory);
                    }

                    throw new InvalidOperationException($"Tried to create a MoveToNextLineShowTextWithSpacing operation with operand type: {operands[2]?.GetType().Name ?? "null"}");
                case MoveToNextLineWithOffset.Symbol:
                    return new MoveToNextLineWithOffset(OperandToDouble(operands[0]), OperandToDouble(operands[1]));
                case MoveToNextLineWithOffsetSetLeading.Symbol:
                    return new MoveToNextLineWithOffsetSetLeading(OperandToDouble(operands[0]), OperandToDouble(operands[1]));
                case PaintShading.Symbol:
                    return new PaintShading((NameToken)operands[0]);
                case SetNonStrokeColor.Symbol:
                    return new SetNonStrokeColor(TokensToDoubleArray(operands));
                case SetNonStrokeColorAdvanced.Symbol:
                    if (operands[operands.Count - 1] is NameToken scnLowerPatternName)
                    {
                        return new SetNonStrokeColorAdvanced(operands.Take(operands.Count - 1).Select(x => ((NumericToken)x).Data).ToArray(), scnLowerPatternName);
                    }
                    
                    if (operands.All(x => x is NumericToken))
                    {
                        return new SetNonStrokeColorAdvanced(operands.Select(x => ((NumericToken)x).Data).ToArray());
                    }

                    var errorMessageScnLower = string.Join(", ", operands.Select(x => x.ToString()));
                    throw new PdfDocumentFormatException($"Attempted to set a non-stroke color space (scn) with invalid arguments: [{errorMessageScnLower}]");
                case SetNonStrokeColorDeviceCmyk.Symbol:
                    return new SetNonStrokeColorDeviceCmyk(OperandToDouble(operands[0]),
                        OperandToDouble(operands[1]),
                        OperandToDouble(operands[2]),
                        OperandToDouble(operands[3]));
                case SetNonStrokeColorDeviceGray.Symbol:
                    return new SetNonStrokeColorDeviceGray(OperandToDouble(operands[0]));
                case SetNonStrokeColorDeviceRgb.Symbol:
                    return new SetNonStrokeColorDeviceRgb(OperandToDouble(operands[0]),
                        OperandToDouble(operands[1]),
                        OperandToDouble(operands[2]));
                case SetNonStrokeColorSpace.Symbol:
                    return new SetNonStrokeColorSpace((NameToken)operands[0]);
                case SetStrokeColor.Symbol:
                    return new SetStrokeColor(TokensToDoubleArray(operands));
                case SetStrokeColorAdvanced.Symbol:
                    if (operands[operands.Count - 1] is NameToken scnPatternName)
                    {
                        return new SetStrokeColorAdvanced(operands.Take(operands.Count - 1).Select(x => ((NumericToken)x).Data).ToList(), scnPatternName);
                    }
                    else if (operands.All(x => x is NumericToken))
                    {
                        return new SetStrokeColorAdvanced(operands.Select(x => ((NumericToken)x).Data).ToList());
                    }

                    var errorMessageScn = string.Join(", ", operands.Select(x => x.ToString()));
                    throw new PdfDocumentFormatException($"Attempted to set a stroke color space (SCN) with invalid arguments: [{errorMessageScn}]");
                case SetStrokeColorDeviceCmyk.Symbol:
                    var setStrokeColorCmykArgs = GetExpectedDoubles(SetNonStrokeColorDeviceCmyk.Symbol,
                        operands,
                        4);
                    return new SetStrokeColorDeviceCmyk(
                        setStrokeColorCmykArgs[0],
                        setStrokeColorCmykArgs[1],
                        setStrokeColorCmykArgs[2],
                        setStrokeColorCmykArgs[3]);
                case SetStrokeColorDeviceGray.Symbol:
                    return new SetStrokeColorDeviceGray(OperandToDouble(operands[0]));
                case SetStrokeColorDeviceRgb.Symbol:
                    return new SetStrokeColorDeviceRgb(OperandToDouble(operands[0]),
                        OperandToDouble(operands[1]),
                        OperandToDouble(operands[2]));
                case SetStrokeColorSpace.Symbol:
                    return new SetStrokeColorSpace((NameToken)operands[0]);
                case SetTextMatrix.Symbol:
                    return new SetTextMatrix(TokensToDoubleArray(operands));
                case StrokePath.Symbol:
                    return StrokePath.Value;
                case ShowText.Symbol:
                    if (operands.Count != 1)
                    {
                        throw new InvalidOperationException($"Attempted to create a show text operation with {operands.Count} operands.");
                    }

                    if (operands[0] is StringToken s)
                    {
                        return new ShowText(s.Data);
                    }
                    
                    if (operands[0] is HexToken h)
                    {
                        return new ShowText(h.Memory);
                    }
                    
                    throw new InvalidOperationException($"Tried to create a show text operation with operand type: {operands[0]?.GetType().Name ?? "null"}");
                case ShowTextsWithPositioning.Symbol:
                    if (operands.Count == 0)
                    {
                        throw new InvalidOperationException("Cannot have 0 parameters for a TJ operator.");
                    }

                    if (operands.Count == 1 && operands[0] is ArrayToken arrayToken)
                    {
                        return new ShowTextsWithPositioning(arrayToken.Data);
                    }

                    var array = operands.ToArray();

                    return new ShowTextsWithPositioning(array);
                case BeginInlineImageData.Symbol:
                    // Should never be encountered because it is handled by the page content parser.
                    return null;
                case EndInlineImage.Symbol:
                    // Should never be encountered because it is handled by the page content parser.
                    return null;
                case Type3SetGlyphWidth.Symbol:
                    var t3SetWidthArgs = GetExpectedDoubles(Type3SetGlyphWidth.Symbol, operands, 2);
                    return new Type3SetGlyphWidth(t3SetWidthArgs[0], t3SetWidthArgs[1]);
                case Type3SetGlyphWidthAndBoundingBox.Symbol:
                    var t3SetWidthAndBbArgs = GetExpectedDoubles(Type3SetGlyphWidthAndBoundingBox.Symbol, operands, 6);
                    return new Type3SetGlyphWidthAndBoundingBox(
                        t3SetWidthAndBbArgs[0],
                        t3SetWidthAndBbArgs[1],
                        t3SetWidthAndBbArgs[2],
                        t3SetWidthAndBbArgs[3],
                        t3SetWidthAndBbArgs[4],
                        t3SetWidthAndBbArgs[5]);
            }

            if (!Operations.TryGetValue(op.Data, out _))
            {
                return null;
            }

            throw new NotImplementedException(
                $"No support implemented for content operator {op.Data}");
        }

        private static double[] GetExpectedDoubles(string operatorSymbol, IReadOnlyList<IToken> operands, int resultCount)
        {
            var results = new double[resultCount];

            if (operands.Count < resultCount)
            {
                throw new InvalidOperationException(
                    $"Invalid operands for {operatorSymbol}, needed {resultCount} numbers, got: {PrintOperands(operands)}");
            }

            for (var i = 0; i < resultCount; i++)
            {
                var op = operands[i];

                if (op is not NumericToken nt)
                {
                    throw new InvalidOperationException(
                        $"Invalid operands for {operatorSymbol}, needed {resultCount} numbers, got: {PrintOperands(operands)}");
                }

                results[i] = nt.Data;
            }

            return results;
        }

        private static string PrintOperands(IEnumerable<IToken> operands)
        {
            return "[" + string.Join(", ", operands.Select(x => x.ToString())) + "]";
        }
    }
}