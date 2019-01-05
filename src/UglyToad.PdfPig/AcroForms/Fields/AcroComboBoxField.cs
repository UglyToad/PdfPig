namespace UglyToad.PdfPig.AcroForms.Fields
{
    using System;
    using System.Collections.Generic;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <inheritdoc />
    /// <summary>
    /// A combo box consisting of a drop-down list optionally accompanied by an editable text box in which the
    /// user can type a value other than the predefined choices.
    /// </summary>
    public class AcroComboBoxField : AcroFieldBase
    {
        /// <summary>
        /// The flags specifying the behaviour of this field.
        /// </summary>
        public AcroChoiceFieldFlags Flags { get; }

        /// <summary>
        /// The options to be presented to the user.
        /// </summary>
        [NotNull]
        public IReadOnlyList<AcroChoiceOption> Options { get; }

        /// <summary>
        /// The names of any currently selected options.
        /// </summary>
        [NotNull]
        public IReadOnlyList<string> SelectedOptions { get; }

        /// <summary>
        /// For multiple select lists with duplicate names gives the indices of the selected options.
        /// </summary>
        [CanBeNull]
        public IReadOnlyList<int> SelectedOptionIndices { get; }

        /// <summary>
        /// Create a new <see cref="AcroComboBoxField"/>.
        /// </summary>
        /// <param name="dictionary">The dictionary for this field.</param>
        /// <param name="fieldType">The type of this field, must be <see cref="NameToken.Ch"/>.</param>
        /// <param name="fieldFlags">The flags specifying behaviour for this field.</param>
        /// <param name="information">Additional information for this field.</param>
        /// <param name="options">The options in this field.</param>
        /// <param name="selectedOptionIndices">The indices of the selected options where there are multiple with the same name.</param>
        /// <param name="selectedOptions">The names of the selected options.</param>
        public AcroComboBoxField(DictionaryToken dictionary, string fieldType, AcroChoiceFieldFlags fieldFlags,
            AcroFieldCommonInformation information, IReadOnlyList<AcroChoiceOption> options, 
            IReadOnlyList<string> selectedOptions, 
            IReadOnlyList<int> selectedOptionIndices) :
            base(dictionary, fieldType, (uint)fieldFlags, information)
        {
            Flags = fieldFlags;
            Options = options ?? throw new ArgumentNullException(nameof(options));
            SelectedOptions = selectedOptions ?? throw new ArgumentNullException(nameof(selectedOptions));
            SelectedOptionIndices = selectedOptionIndices;
        }
    }
}