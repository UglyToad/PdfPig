namespace UglyToad.PdfPig.Graphics
{
    using PdfPig.Core;
    using Tokens;
    using UglyToad.PdfPig.Graphics.Core;

    /// <summary>
    /// The current graphics state context when running a PDF content stream.
    /// </summary>
    public interface IOperationContext
    {
        /// <summary>
        /// The current position.
        /// </summary>
        PdfPoint CurrentPosition { get; set; }

        /// <summary>
        /// The matrices for the current text state.
        /// </summary>
        TextMatrices TextMatrices { get; }

        /// <summary>
        /// The number of graphics states on the stack.
        /// </summary>
        int StackSize { get; }

        /// <summary>
        /// Gets the current graphic state.
        /// </summary>
        CurrentGraphicsState GetCurrentState();

        /// <summary>
        /// Sets the current graphics state to the state from the top of the stack.
        /// </summary>
        void PopState();

        /// <summary>
        /// Saves a copy of the current graphics state on the stack.
        /// </summary>
        void PushState();

        /// <summary>
        /// Shows the text represented by the provided bytes using the current graphics state.
        /// </summary>
        /// <param name="bytes">The bytes of the text.</param>
        void ShowText(IInputBytes bytes);

        /// <summary>
        /// Interprets the tokens to draw text at positions.
        /// </summary>
        /// <param name="tokens">The tokens to show.</param>
        void ShowPositionedText(IReadOnlyList<IToken> tokens);

        /// <summary>
        /// Retrieves the named XObject and applies it to the current state.
        /// </summary>
        /// <param name="xObjectName">The name of the XObject.</param>
        void ApplyXObject(NameToken xObjectName);

        /// <summary>
        /// Start a new sub-path.
        /// </summary>
        void BeginSubpath();

        /// <summary>
        /// Close the current subpath.
        /// </summary>
        PdfPoint? CloseSubpath();

        /// <summary>
        /// Stroke the current path.
        /// </summary>
        /// <param name="close">Whether to also close the path.</param>
        void StrokePath(bool close);

        /// <summary>
        /// Fill the current path.
        /// </summary>
        /// <param name="fillingRule">The filling rule to use.</param>
        /// <param name="close">Whether to also close the path.</param>
        void FillPath(FillingRule fillingRule, bool close);

        /// <summary>
        /// Fill and stroke the current path.
        /// </summary>
        /// <param name="fillingRule">The filling rule to use.</param>
        /// <param name="close">Whether to also close the path.</param>
        void FillStrokePath(FillingRule fillingRule, bool close);

        /// <summary>
        /// Add a move command to the path.
        /// <para>Should implement matrix transformations.</para>
        /// </summary>
        void MoveTo(double x, double y);

        /// <summary>
        /// Add a bezier curve to the current subpath.
        /// <para>Should implement matrix transformations.</para>
        /// </summary>
        void BezierCurveTo(double x1, double y1, double x2, double y2, double x3, double y3);

        /// <summary>
        /// Add a bezier curve to the current subpath.
        /// <para>Should implement matrix transformations.</para>
        /// </summary>
        void BezierCurveTo(double x2, double y2, double x3, double y3);

        /// <summary>
        /// Add a line command to the subpath.
        /// <para>Should implement matrix transformations.</para>
        /// </summary>
        void LineTo(double x, double y);

        /// <summary>
        /// Add a rectangle following the pdf specification (m, l, l, l, c) path. A new subpath will be created.
        /// <para>Should implement matrix transformations.</para>
        /// </summary>
        void Rectangle(double x, double y, double width, double height);

        /// <summary>
        /// End the path object without filling or stroking it. This operator shall be a path-painting no-op,
        /// used primarily for the side effect of changing the current clipping path (see 8.5.4, "Clipping Path Operators").
        /// </summary>
        void EndPath();

        /// <summary>
        /// Close the current path.
        /// </summary>
        void ClosePath();

        /// <summary>
        /// Indicate that a marked content region is started.
        /// </summary>
        void BeginMarkedContent(NameToken name, NameToken? propertyDictionaryName, DictionaryToken? properties);

        /// <summary>
        /// Indicates that the current marked content region is complete.
        /// </summary>
        void EndMarkedContent();

        /// <summary>
        /// Update the graphics state to apply the state from the named ExtGState dictionary.
        /// </summary>
        /// <param name="stateName">The name of the state to apply.</param>
        void SetNamedGraphicsState(NameToken stateName);

        /// <summary>
        /// Indicate that an inline image is being defined.
        /// </summary>
        void BeginInlineImage();

        /// <summary>
        /// Define the properties of the inline image currently being drawn.
        /// </summary>
        void SetInlineImageProperties(IReadOnlyDictionary<NameToken, IToken> properties);

        /// <summary>
        /// Indicates that the current inline image is complete.
        /// </summary>
        void EndInlineImage(ReadOnlyMemory<byte> bytes);

        /// <summary>
        /// Modify the clipping rule of the current path.
        /// </summary>
        void ModifyClippingIntersect(FillingRule clippingRule);

        /// <summary>
        /// Set the flatness tolerance in the graphics state.
        /// Flatness is a number in the range 0 to 100; a value of 0 specifies the output device’s default flatness tolerance.
        /// </summary>
        /// <param name="tolerance"></param>
        void SetFlatnessTolerance(double tolerance);

        /// <summary>
        /// Set the line cap style in the graphics state.
        /// </summary>
        void SetLineCap(LineCapStyle cap);

        /// <summary>
        /// Set the line dash pattern in the graphics state.
        /// </summary>
        void SetLineDashPattern(LineDashPattern pattern);

        /// <summary>
        /// Set the line join style in the graphics state.
        /// </summary>
        void SetLineJoin(LineJoinStyle join);

        /// <summary>
        /// Set the line width in the graphics state.
        /// </summary>
        void SetLineWidth(double width);

        /// <summary>
        /// Set the miter limit in the graphics state.
        /// </summary>
        void SetMiterLimit(double limit);

        /// <summary>
        /// Move to the start of the next line.
        /// </summary>
        /// <remarks>
        /// This performs this operation: 0 -Tl Td
        /// The offset is negative leading text (Tl) value, this is incorrect in the specification.
        /// </remarks>
        void MoveToNextLineWithOffset();

        /// <summary>
        /// Set the font and the font size.
        /// Font is the name of a font resource in the Font subdictionary of the current resource dictionary.
        /// Size is a number representing a scale factor.
        /// </summary>
        void SetFontAndSize(NameToken font, double size);

        /// <summary>
        /// Set the horizontal scaling.
        /// </summary>
        /// <param name="scale"></param>
        void SetHorizontalScaling(double scale);

        /// <summary>
        /// Set the text leading.
        /// </summary>
        void SetTextLeading(double leading);

        /// <summary>
        /// Set the text rendering mode.
        /// </summary>
        void SetTextRenderingMode(TextRenderingMode mode);

        /// <summary>
        /// Set text rise.
        /// </summary>
        void SetTextRise(double rise);

        /// <summary>
        /// Sets the word spacing.
        /// </summary>
        void SetWordSpacing(double spacing);

        /// <summary>
        /// Modify the current transformation matrix by concatenating the specified matrix.
        /// </summary>
        void ModifyCurrentTransformationMatrix(double[] value);

        /// <summary>
        /// Set the character spacing to a number expressed in unscaled text space units.
        /// Initial value: 0.
        /// </summary>
        void SetCharacterSpacing(double spacing);

        /// <summary>
        /// Paint the shape and colour shading described by a shading dictionary, subject to the current clipping path. The current colour in the graphics state is neither used nor altered. The effect is different from that of painting a path using a shading pattern as the current colour.
        /// </summary>
        /// <param name="shading">The name of a shading dictionary resource in the Shading subdictionary of the current resource dictionary.</param>
        void PaintShading(NameToken shading);
    }
}
