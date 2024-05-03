namespace UglyToad.PdfPig.AcroForms.Fields
{
    using Core;
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// A scrollable list box field.
    /// </summary>
    public class AcroListBoxField : AcroFieldBase
    {
        /// <summary>
        /// The flags specifying the behaviour of this field.
        /// </summary>
        public AcroChoiceFieldFlags Flags { get; }

        /// <summary>
        /// The options to be presented to the user.
        /// </summary>
        public IReadOnlyList<AcroChoiceOption> Options { get; }

        /// <summary>
        /// The names of any currently selected options.
        /// </summary>
        public IReadOnlyList<string> SelectedOptions { get; }

        /// <summary>
        /// For multiple select lists with duplicate names gives the indices of the selected options.
        /// </summary>
        public IReadOnlyList<int>? SelectedOptionIndices { get; }

        /// <summary>
        /// For scrollable list boxes gives the index of the first visible option.
        /// </summary>
        public int TopIndex { get; }

        /// <summary>
        /// Whether the field allows multiple selections.
        /// </summary>
        public bool SupportsMultiSelect => Flags.Equals(AcroChoiceFieldFlags.MultiSelect);

        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="AcroListBoxField"/>.
        /// </summary>
        /// <param name="dictionary">The dictionary for this field.</param>
        /// <param name="fieldType">The type of this field, must be <see cref="NameToken.Ch"/>.</param>
        /// <param name="fieldFlags">The flags specifying behaviour for this field.</param>
        /// <param name="information">Additional information for this field.</param>
        /// <param name="options">The options in this field.</param>
        /// <param name="selectedOptionIndices">The indices of the selected options where there are multiple with the same name.</param>
        /// <param name="topIndex">The first visible option index.</param>
        /// <param name="selectedOptions">The names of the selected options.</param>
        /// <param name="pageNumber">The number of the page this field appears on.</param>
        /// <param name="bounds">The location of this field on the page.</param>
        public AcroListBoxField(DictionaryToken dictionary, string fieldType, AcroChoiceFieldFlags fieldFlags,
            AcroFieldCommonInformation information, IReadOnlyList<AcroChoiceOption> options,
            IReadOnlyList<string> selectedOptions, 
            IReadOnlyList<int>? selectedOptionIndices,
            int? topIndex,
            int? pageNumber,
            PdfRectangle? bounds) : 
            base(dictionary, fieldType, (uint)fieldFlags, AcroFieldType.ListBox, information, pageNumber, bounds)
        {
            Flags = fieldFlags;
            Options = options ?? throw new ArgumentNullException(nameof(options));
            SelectedOptions = selectedOptions ?? throw new ArgumentNullException(nameof(selectedOptions));
            SelectedOptionIndices = selectedOptionIndices;
            TopIndex = topIndex ?? 0;
        }
    }
}