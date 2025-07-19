namespace UglyToad.PdfPig.Writer;

using Core;
using Graphics.Operations;
using Graphics.Operations.SpecialGraphicsState;
using System;
using System.Collections.Generic;

internal static class PdfContentTransformationReader
{
    public static TransformationMatrix? GetGlobalTransform(IEnumerable<IGraphicsStateOperation> operations)
    {
        TransformationMatrix? activeMatrix = null;
        var stackDepth = 0;
        foreach (var operation in operations)
        {
            if (operation is ModifyCurrentTransformationMatrix cm)
            {
                if (stackDepth == 0 && cm.Value.Length == 6)
                {
                    activeMatrix = TransformationMatrix.FromArray(cm.Value);
                }
            }
            else if (operation is Push push)
            {
                stackDepth++;
            }
            else if (operation is Pop pop)
            {
                stackDepth--;
            }
        }

        return activeMatrix;
    }
}
