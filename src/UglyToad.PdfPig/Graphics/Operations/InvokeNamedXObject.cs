namespace UglyToad.PdfPig.Graphics.Operations
{
    using System;
    using System.IO;
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// Paint the specified XObject. 
    /// The operand name must appear as a key in the XObject subdictionary of the current resource dictionary. 
    /// The associated value must be a stream whose Type entry, if present, is XObject.
    /// The effect of <see cref="T:UglyToad.PdfPig.Graphics.Operations.InvokeNamedXObject" /> depends on the value of the XObject's Subtype entry, which may be Image, Form or PS. 
    /// </summary>
    public class InvokeNamedXObject : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "Do";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The name of the XObject which appears in the XObject subdictionary of the current resource dictionary.
        /// </summary>
        public NameToken Name { get; }

        /// <summary>
        /// Create a new <see cref="InvokeNamedXObject"/>.
        /// </summary>
        /// <param name="name">The name of the XObject.</param>
        public InvokeNamedXObject(NameToken name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.ApplyXObject(Name);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteText($"/{Name}");
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Name} {Symbol}";
        }
    }
}