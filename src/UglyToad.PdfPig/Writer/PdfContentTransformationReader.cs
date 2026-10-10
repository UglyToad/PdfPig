namespace UglyToad.PdfPig.Writer;

using Core;
using Graphics.Operations;
using Graphics.Operations.SpecialGraphicsState;
using System.Collections.Generic;

internal static class PdfContentTransformationReader
{
    public static TransformationMatrix? GetGlobalTransform(IEnumerable<IGraphicsStateOperation> operations)
    {
        var stackDepth = 0;
        return GetGlobalTransform(operations, ref stackDepth);
    }

    /// <summary>
    /// Get the global transform of one of the content streams of a page. A page's content streams are a single
    /// stream once concatenated, so a 'q' can be closed by a 'Q' in a later stream: <paramref name="stackDepth"/>
    /// carries the save/restore depth from one stream of the page to the next.
    /// </summary>
    public static TransformationMatrix? GetGlobalTransform(IEnumerable<IGraphicsStateOperation> operations, ref int stackDepth)
    {
        TransformationMatrix? activeMatrix = null;
        foreach (var operation in operations)
        {
            if (operation is ModifyCurrentTransformationMatrix cm)
            {
                if (stackDepth == 0 && cm.Value.Length == 6)
                {
                    var matrix = TransformationMatrix.FromArray(cm.Value);
                    activeMatrix = activeMatrix.HasValue ? matrix.Multiply(activeMatrix.Value) : matrix;
                }
            }
            else if (operation is Push)
            {
                stackDepth++;
            }
            else if (operation is Pop)
            {
                stackDepth--;
            }
        }

        return activeMatrix;
    }

    /// <summary>
    /// Get the 'cm' operation undoing the global transform, or <see langword="null"/> if the transform
    /// is not invertible (e.g. '0 0 0 0 0 0 cm') in which case no operation is able to undo it.
    /// </summary>
    public static ModifyCurrentTransformationMatrix? GetInverseOperation(TransformationMatrix globalTransform)
    {
        var inverse = globalTransform.Inverse();
        double[] values = [inverse.A, inverse.B, inverse.C, inverse.D, inverse.E, inverse.F];

        foreach (var value in values)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return null;
            }
        }

        return new ModifyCurrentTransformationMatrix(values);
    }
}
