using UglyToad.PdfPig.Tokenization;

namespace UglyToad.PdfPig.Graphics
{
    using Operations;
    using Operations.ClippingPaths;
    using Operations.Compatibility;
    using Operations.General;
    using Operations.InlineImages;
    using Operations.MarkedContent;
    using Operations.PathConstruction;
    using Operations.SpecialGraphicsState;
    using Operations.TextObjects;
    using Operations.TextPositioning;
    using Operations.TextShowing;
    using Operations.TextState;
    using PdfPig.Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Tokens;
    using Operations.PathPainting;

    internal class ReflectionGraphicsStateOperationFactory : IGraphicsStateOperationFactory
    {
        private static readonly ListPool<decimal> DecimalListPool = new ListPool<decimal>(10);

        private readonly IReadOnlyDictionary<string, Type> operations;

        public ReflectionGraphicsStateOperationFactory()
        {
            var assemblyTypes = Assembly.GetAssembly(typeof(ReflectionGraphicsStateOperationFactory)).GetTypes();

            var result = new Dictionary<string, Type>();

            foreach (var assemblyType in assemblyTypes)
            {
                if (!assemblyType.IsInterface && typeof(IGraphicsStateOperation).IsAssignableFrom(assemblyType))
                {
                    var symbol = assemblyType.GetField("Symbol");

                    if (symbol == null)
                    {
                        throw new InvalidOperationException("An operation type was defined without the public const Symbol being declared. Type was: " + assemblyType.FullName);
                    }

                    var value = symbol.GetValue(null).ToString();

                    result[value] = assemblyType;
                }
            }

            operations = result;
        }

        private static decimal[] TokensToDecimalArray(IReadOnlyList<IToken> tokens, bool exceptLast = false)
        {
            var result = DecimalListPool.Borrow();

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
                            var val = result.ToArray();
                            DecimalListPool.Return(result);
                            return val.ToArray();
                        }

                        result.Add(innerNumeric.Data);
                    }
                }

                if (!(operand is NumericToken numeric))
                {
                    var val = result.ToArray();
                    DecimalListPool.Return(result);
                    return val.ToArray();
                }

                result.Add(numeric.Data);
            }

            var returnValue = result.ToArray();
            DecimalListPool.Return(result);
            return returnValue;
        }

        private static int OperandToInt(IToken token)
        {
            if (!(token is NumericToken numeric))
            {
                throw new InvalidOperationException($"Invalid operand token encountered when expecting numeric: {token}.");
            }

            return numeric.Int;
        }

        private static decimal OperandToDecimal(IToken token)
        {
            if (!(token is NumericToken numeric))
            {
                throw new InvalidOperationException($"Invalid operand token encountered when expecting numeric: {token}.");
            }

            return numeric.Data;
        }

        public IGraphicsStateOperation Create(OperatorToken op, IReadOnlyList<IToken> operands)
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
                    return new SetFlatnessTolerance(OperandToDecimal(operands[0]));
                case SetLineCap.Symbol:
                    return new SetLineCap(OperandToInt(operands[0]));
                case SetLineDashPattern.Symbol:
                    return new SetLineDashPattern(TokensToDecimalArray(operands, true), OperandToInt(operands[operands.Count - 1]));
                case SetLineJoin.Symbol:
                    return new SetLineJoin(OperandToInt(operands[0]));
                case SetLineWidth.Symbol:
                    return new SetLineWidth(OperandToDecimal(operands[0]));
                case SetMiterLimit.Symbol:
                    return new SetMiterLimit(OperandToDecimal(operands[0]));
                case AppendDualControlPointBezierCurve.Symbol:
                    return new AppendDualControlPointBezierCurve(OperandToDecimal(operands[0]),
                        OperandToDecimal(operands[1]),
                        OperandToDecimal(operands[2]),
                        OperandToDecimal(operands[3]),
                        OperandToDecimal(operands[4]),
                        OperandToDecimal(operands[5]));
                case AppendEndControlPointBezierCurve.Symbol:
                    return new AppendEndControlPointBezierCurve(OperandToDecimal(operands[0]),
                        OperandToDecimal(operands[1]),
                        OperandToDecimal(operands[2]),
                        OperandToDecimal(operands[3]));
                case AppendRectangle.Symbol:
                    return new AppendRectangle(OperandToDecimal(operands[0]),
                        OperandToDecimal(operands[1]),
                        OperandToDecimal(operands[2]),
                        OperandToDecimal(operands[3]));
                case AppendStartControlPointBezierCurve.Symbol:
                    return new AppendStartControlPointBezierCurve(OperandToDecimal(operands[0]),
                        OperandToDecimal(operands[1]),
                        OperandToDecimal(operands[2]),
                        OperandToDecimal(operands[3]));
                case AppendStraightLineSegment.Symbol:
                    return new AppendStraightLineSegment(OperandToDecimal(operands[0]),
                        OperandToDecimal(operands[1]));
                case BeginNewSubpath.Symbol:
                    return new BeginNewSubpath(OperandToDecimal(operands[0]),
                        OperandToDecimal(operands[1]));
                case CloseSubpath.Symbol:
                    return CloseSubpath.Value;
                case ModifyCurrentTransformationMatrix.Symbol:
                    return new ModifyCurrentTransformationMatrix(TokensToDecimalArray(operands));
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
                    return new SetCharacterSpacing(OperandToDecimal(operands[0]));
                case SetFontAndSize.Symbol:
                    return new SetFontAndSize((NameToken)operands[0], OperandToDecimal(operands[1]));
                case SetHorizontalScaling.Symbol:
                    return new SetHorizontalScaling(OperandToDecimal(operands[0]));
                case SetTextLeading.Symbol:
                    return new SetTextLeading(OperandToDecimal(operands[0]));
                case SetTextRenderingMode.Symbol:
                    return new SetTextRenderingMode(OperandToInt(operands[0]));
                case SetTextRise.Symbol:
                    return new SetTextRise(OperandToDecimal(operands[0]));
                case SetWordSpacing.Symbol:
                    return new SetWordSpacing(OperandToDecimal(operands[0]));
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
                    else if (operands[0] is HexToken hnl)
                    {
                        return new MoveToNextLineShowText(hnl.Bytes.ToArray());
                    }
                    else
                    {
                        throw new InvalidOperationException($"Tried to create a move to next line and show text operation with operand type: {operands[0]?.GetType().Name ?? "null"}");
                    }
                case MoveToNextLineWithOffset.Symbol:
                    if (operands.Count < 2)
                    {
                        return null;
                    }

                    return new MoveToNextLineWithOffset(OperandToDecimal(operands[0]), OperandToDecimal(operands[1]));
                case MoveToNextLineWithOffsetSetLeading.Symbol:
                    if (operands.Count < 2)
                    {
                        return null;
                    }

                    return new MoveToNextLineWithOffsetSetLeading(OperandToDecimal(operands[0]), OperandToDecimal(operands[1]));
                case PaintShading.Symbol:
                    return new PaintShading((NameToken)operands[0]);
                case SetNonStrokeColor.Symbol:
                    return new SetNonStrokeColor(TokensToDecimalArray(operands));
                case SetNonStrokeColorAdvanced.Symbol:
                    if (operands[operands.Count - 1] is NameToken scnLowerPatternName)
                    {
                        return new SetNonStrokeColorAdvanced(operands.Take(operands.Count - 1).Select(x => ((NumericToken)x).Data).ToList(), scnLowerPatternName);
                    }
                    else if (operands.All(x => x is NumericToken))
                    {
                        return new SetNonStrokeColorAdvanced(operands.Select(x => ((NumericToken)x).Data).ToList());
                    }

                    var errorMessageScnLower = string.Join(", ", operands.Select(x => x.ToString()));
                    throw new PdfDocumentFormatException($"Attempted to set a non-stroke color space (scn) with invalid arguments: [{errorMessageScnLower}]");
                case SetNonStrokeColorDeviceCmyk.Symbol:
                    return new SetNonStrokeColorDeviceCmyk(OperandToDecimal(operands[0]),
                        OperandToDecimal(operands[1]),
                        OperandToDecimal(operands[2]),
                        OperandToDecimal(operands[3]));
                case SetNonStrokeColorDeviceGray.Symbol:
                    return new SetNonStrokeColorDeviceGray(OperandToDecimal(operands[0]));
                case SetNonStrokeColorDeviceRgb.Symbol:
                    return new SetNonStrokeColorDeviceRgb(OperandToDecimal(operands[0]),
                        OperandToDecimal(operands[1]),
                        OperandToDecimal(operands[2]));
                case SetNonStrokeColorSpace.Symbol:
                    return new SetNonStrokeColorSpace((NameToken)operands[0]);
                case SetStrokeColor.Symbol:
                    return new SetStrokeColor(TokensToDecimalArray(operands));
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
                    return new SetStrokeColorDeviceCmyk(OperandToDecimal(operands[0]),
                        OperandToDecimal(operands[1]),
                        OperandToDecimal(operands[2]),
                        OperandToDecimal(operands[3]));
                case SetStrokeColorDeviceGray.Symbol:
                    return new SetStrokeColorDeviceGray(OperandToDecimal(operands[0]));
                case SetStrokeColorDeviceRgb.Symbol:
                    return new SetStrokeColorDeviceRgb(OperandToDecimal(operands[0]),
                        OperandToDecimal(operands[1]),
                        OperandToDecimal(operands[2]));
                case SetStrokeColorSpace.Symbol:
                    return new SetStrokeColorSpace((NameToken)operands[0]);
                case SetTextMatrix.Symbol:
                    return new SetTextMatrix(TokensToDecimalArray(operands));
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
                    else if (operands[0] is HexToken h)
                    {
                        return new ShowText(h.Bytes.ToArray());
                    }
                    else
                    {
                        throw new InvalidOperationException($"Tried to create a show text operation with operand type: {operands[0]?.GetType().Name ?? "null"}");
                    }
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
            }

            if (!operations.TryGetValue(op.Data, out Type operationType))
            {
                return null;
            }

            var constructors = operationType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (constructors.Length == 0)
            {
                throw new InvalidOperationException("No constructors to invoke were found for operation type: " + operationType.FullName);
            }

            // This only works by luck...
            var constructor = constructors[0];

            if (constructor.IsPrivate)
            {
                return (IGraphicsStateOperation)operationType.GetField("Value").GetValue(null);
            }

            var parameters = constructor.GetParameters();

            var offset = 0;

            var arguments = new List<object>();

            foreach (var parameter in parameters)
            {
                if (offset >= operands.Count)
                {
                    throw new InvalidOperationException($"Fewer operands {operands.Count} found than required ({offset + 1}) for operator: {op.Data}.");
                }

                if (parameter.ParameterType == typeof(decimal))
                {
                    if (operands[offset] is NumericToken numeric)
                    {
                        arguments.Add(numeric.Data);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Expected a decimal parameter for operation type {operationType.FullName}. Instead got: {operands[offset]}");
                    }

                    offset++;
                }
                else if (parameter.ParameterType == typeof(int))
                {
                    if (operands[offset] is NumericToken numeric)
                    {
                        arguments.Add(numeric.Int);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Expected an integer parameter for operation type {operationType.FullName}. Instead got: {operands[offset]}");
                    }

                    offset++;
                }
                else if (parameter.ParameterType == typeof(decimal[]))
                {
                    if (operands[offset] is ArrayToken arr)
                    {
                        arguments.Add(arr.Data.OfType<NumericToken>().Select(x => x.Data).ToArray());
                        offset++;
                        continue;
                    }

                    var array = new List<decimal>();
                    while (offset < operands.Count && operands[offset] is NumericToken numeric)
                    {
                        array.Add(numeric.Data);
                        offset++;
                    }

                    arguments.Add(array.ToArray());
                }
                else if (parameter.ParameterType == typeof(NameToken))
                {
                    if (operands[offset] is NameToken name)
                    {
                        arguments.Add(name);
                    }
                    else if (operands[offset] is StringToken s)
                    {
                        arguments.Add(NameToken.Create(s.Data));
                    }
                    else
                    {
                        throw new InvalidOperationException($"Expected a decimal array parameter for operation type {operationType.FullName}. Instead got: {operands[offset]}");
                    }

                    offset++;
                }
                else if (parameter.ParameterType == typeof(string))
                {
                    if (operands[offset] is StringToken stringToken)
                    {
                        arguments.Add(stringToken.Data);
                    }
                    else if (operands[offset] is HexToken hexToken)
                    {
                        arguments.Add(hexToken.Data);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Expected a string parameter for operation type {operationType.FullName}. Instead got: {operands[offset]}");
                    }

                    offset++;
                }
                else
                {
                    throw new NotImplementedException($"Unsupported parameter type {parameter.ParameterType.FullName} for operation type {operationType.FullName}.");
                }
            }

            var result = constructor.Invoke(arguments.ToArray());

            return (IGraphicsStateOperation)result;
        }
    }
}