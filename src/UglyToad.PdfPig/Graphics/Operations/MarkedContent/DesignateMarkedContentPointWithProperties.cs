namespace UglyToad.PdfPig.Graphics.Operations.MarkedContent
{
    using System.IO;
    using Tokens;
    using Writer;

    /// <inheritdoc />
    /// <summary>
    /// Designate a single marked-content point in the content stream with an associated property list.
    /// </summary>
    public class DesignateMarkedContentPointWithProperties : IGraphicsStateOperation
    {
        private static readonly TokenWriter TokenWriter = new TokenWriter();

        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "DP";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// A name indicating the role or significance of the point.
        /// </summary>
        public NameToken Name { get; }

        /// <summary>
        /// The name of the property dictionary in the Properties subdictionary of the current resource dictionary.
        /// Can be <see langword="null"/> if the property dictionary is provided inline.
        /// </summary>
        public NameToken? PropertyDictionaryName { get; }

        /// <summary>
        /// The marked-content point properties.
        /// Can be <see langword="null"/> if a name of the property dictionary is provided instead.
        /// </summary>
        public DictionaryToken? Properties { get; }

        /// <summary>
        /// Create a new <see cref="DesignateMarkedContentPointWithProperties"/>.
        /// </summary>
        /// <param name="name">The name of the marked-content point.</param>
        /// <param name="propertyDictionaryName">The name of the property dictionary.</param>
        public DesignateMarkedContentPointWithProperties(NameToken name, NameToken propertyDictionaryName)
        {
            Name = name;
            PropertyDictionaryName = propertyDictionaryName;
        }

        /// <summary>
        /// Create a new <see cref="DesignateMarkedContentPointWithProperties"/>.
        /// </summary>
        /// <param name="name">The name of the marked-content point.</param>
        /// <param name="properties">The properties of the marked-content point.</param>
        public DesignateMarkedContentPointWithProperties(NameToken name, DictionaryToken properties)
        {
            Name = name;
            Properties = properties;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            // need to implement marked content here
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            TokenWriter.WriteToken(Name, stream);
            stream.WriteWhiteSpace();

            if (PropertyDictionaryName != null)
            {
                TokenWriter.WriteToken(PropertyDictionaryName, stream);
            }
            else
            {
                TokenWriter.WriteToken(Properties!, stream);
            }

            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }
    }
}