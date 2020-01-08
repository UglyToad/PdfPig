namespace UglyToad.PdfPig.Graphics
{
    using PdfPig.Core;
    using System.Collections.Generic;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// The current graphics state context when running a PDF content stream.
    /// </summary>
    public interface IOperationContext
    {
        /// <summary>
        /// The current path being drawn if applicable.
        /// </summary>
        [CanBeNull]
        PdfPath CurrentPath { get; }

        /// <summary>
        /// The active colorspaces for this content stream.
        /// </summary>
        IColorSpaceContext ColorSpaceContext { get; }

        /// <summary>
        /// The current position.
        /// </summary>
        PdfPoint CurrentPosition { get; set; }

        /// <summary>
        /// Get the currently active <see cref="CurrentGraphicsState"/>. States are stored on a stack structure.
        /// </summary>
        /// <returns>The currently active graphics state.</returns>
        CurrentGraphicsState GetCurrentState();

        /// <summary>
        /// The matrices for the current text state.
        /// </summary>
        TextMatrices TextMatrices { get; }

        /// <summary>
        /// The current transformation matrix
        /// </summary>
        TransformationMatrix CurrentTransformationMatrix { get; }

        /// <summary>
        /// The number of graphics states on the stack.
        /// </summary>
        int StackSize { get; }

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
        /// Stroke the current path.
        /// </summary>
        /// <param name="close">Whether to also close the path.</param>
        void StrokePath(bool close);

        /// <summary>
        /// Fill the current path.
        /// </summary>
        /// <param name="close">Whether to also close the path.</param>
        void FillPath(bool close);

        /// <summary>
        /// Close the current path.
        /// </summary>
        void ClosePath();

        /// <summary>
        /// 
        /// </summary>
        void BeginMarkedContent(NameToken name, NameToken propertyDictionaryName, DictionaryToken Properties);

        /// <summary>
        /// 
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
        void EndInlineImage(IReadOnlyList<byte> bytes);
    }
}