namespace UglyToad.PdfPig.AcroForms.Fields
{
    using System;
    using System.Collections.Generic;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// A field in an interactive <see cref="AcroForm"/>.
    /// </summary>
    internal abstract class AcroFieldBase
    {
        /// <summary>
        /// The raw PDF dictionary for this field.
        /// </summary>
        public DictionaryToken Dictionary { get; }

        /// <summary>
        /// The <see cref="string"/> representing the type of this field.
        /// </summary>
        public string FieldType { get; }

        /// <summary>
        /// Specifies various characteristics of the field.
        /// </summary>
        public uint FieldFlags { get; }

        public AcroFieldCommonInformation Information { get; }

        protected AcroFieldBase(DictionaryToken dictionary, string fieldType, uint fieldFlags, AcroFieldCommonInformation information)
        {
            Dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            FieldType = fieldType ?? throw new ArgumentNullException(nameof(fieldType));
            FieldFlags = fieldFlags;
            Information = information;
        }
    }

    /// <summary>
    /// A non-leaf field in the form's structure.
    /// </summary>
    internal class NonTerminalAcroField : AcroFieldBase
    {
        public IReadOnlyList<AcroFieldBase> Children { get; }

        public NonTerminalAcroField(DictionaryToken dictionary, string fieldType, uint fieldFlags, AcroFieldCommonInformation information,
            IReadOnlyList<AcroFieldBase> children) : 
            base(dictionary, fieldType, fieldFlags, information)
        {
            Children = children ?? throw new ArgumentNullException(nameof(children));
        }
    }

    internal class AcroRadioButtonsField : AcroFieldBase
    {
        public AcroButtonFieldFlags Flags { get; }

        public AcroRadioButtonsField(DictionaryToken dictionary, string fieldType, AcroButtonFieldFlags fieldFlags, 
            AcroFieldCommonInformation information) : 
            base(dictionary, fieldType, (uint)fieldFlags, information)
        {
            Flags = fieldFlags;
        }
    }

    internal class AcroPushButtonField : AcroFieldBase
    {
        public AcroButtonFieldFlags Flags { get; }

        public AcroPushButtonField(DictionaryToken dictionary, string fieldType, AcroButtonFieldFlags fieldFlags, 
            AcroFieldCommonInformation information) : 
            base(dictionary, fieldType, (uint)fieldFlags, information)
        {
            Flags = fieldFlags;
        }
    }

    internal class AcroCheckboxField : AcroFieldBase
    {
        public AcroButtonFieldFlags Flags { get; }

        public NameToken CurrentValue { get; }

        public AcroCheckboxField(DictionaryToken dictionary, string fieldType, AcroButtonFieldFlags fieldFlags,
            AcroFieldCommonInformation information, NameToken currentValue) :
            base(dictionary, fieldType, (uint)fieldFlags, information)
        {
            Flags = fieldFlags;
            CurrentValue = currentValue;
        }
    }

    internal class AcroTextField : AcroFieldBase
    {
        public AcroTextFieldFlags Flags { get; }

        public AcroTextField(DictionaryToken dictionary, string fieldType, AcroTextFieldFlags fieldFlags,
            AcroFieldCommonInformation information, string textValue) : 
            base(dictionary, fieldType, (uint)fieldFlags, information)
        {
            Flags = fieldFlags;
        }
    }

    internal class AcroSignatureField : AcroFieldBase
    {
        public AcroSignatureField(DictionaryToken dictionary, string fieldType, uint fieldFlags, AcroFieldCommonInformation information) : 
            base(dictionary, fieldType, fieldFlags, information)
        {
        }
    }

    internal class AcroListBoxField : AcroFieldBase
    {
        public AcroChoiceFieldFlags Flags { get; }

        public IReadOnlyList<AcroChoiceOption> Options { get; }

        public AcroListBoxField(DictionaryToken dictionary, string fieldType, AcroChoiceFieldFlags fieldFlags,
            AcroFieldCommonInformation information, IReadOnlyList<AcroChoiceOption> options) : 
            base(dictionary, fieldType, (uint)fieldFlags, information)
        {
            Flags = fieldFlags;
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }
    }

    internal class AcroComboBoxField : AcroFieldBase
    {
        public AcroChoiceFieldFlags Flags { get; }

        public AcroComboBoxField(DictionaryToken dictionary, string fieldType, AcroChoiceFieldFlags fieldFlags, 
            AcroFieldCommonInformation information) :
            base(dictionary, fieldType, (uint)fieldFlags, information)
        {
            Flags = fieldFlags;
        }
    }

    /// <summary>
    /// An option in a choice field, either <see cref="AcroComboBoxField"/> or <see cref="AcroListBoxField"/>.
    /// </summary>
    internal class AcroChoiceOption
    {
        /// <summary>
        /// The index of this option in the array.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The text of the option.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The value of the option when the form is exported.
        /// </summary>
        public string ExportValue { get; }

        /// <summary>
        /// Create a new <see cref="AcroChoiceOption"/>.
        /// </summary>
        public AcroChoiceOption(int index, string name, string exportValue = null)
        {
            Index = index;
            Name = name;
            ExportValue = exportValue;
        }
    }

    /// <summary>
    /// Information from the field dictionary which is common across all field types.
    /// </summary>
    internal class AcroFieldCommonInformation
    {
        /// <summary>
        /// The reference to the field which is the parent of this one, if applicable.
        /// </summary>
        public IndirectReference? Parent { get; set; }

        /// <summary>
        /// The partial field name for this field. The fully qualified field name is the
        /// period '.' joined name of all parents' partial names and this field's partial name.
        /// </summary>
        [CanBeNull]
        public string PartialName { get; }

        /// <summary>
        /// The alternate field name to be used instead of the fully qualified field name where
        /// the field is being identified on the user interface or by screen readers.
        /// </summary>
        [CanBeNull]
        public string AlternateName { get; }

        /// <summary>
        /// The mapping name used when exporting form field data from the document.
        /// </summary>
        [CanBeNull]
        public string MappingName { get; }

        /// <summary>
        /// Create a new <see cref="AcroFieldCommonInformation"/>.
        /// </summary>
        public AcroFieldCommonInformation(IndirectReference? parent, string partialName, string alternateName, string mappingName)
        {
            Parent = parent;
            PartialName = partialName;
            AlternateName = alternateName;
            MappingName = mappingName;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Parent: {Parent}. Partial: {PartialName}. Alternate: {AlternateName}. Mapping: {MappingName}.";
        }
    }

    /// <summary>
    /// Flags specifying various characteristics of a button type field in an <see cref="AcroFieldBase"/>.
    /// </summary>
    [Flags]
    internal enum AcroButtonFieldFlags : uint
    {
        /// <summary>
        /// The user may not change the value of the field.
        /// </summary>
        ReadOnly = 1 << 0,
        /// <summary>
        /// The field must have a value before the form can be submitted.
        /// </summary>
        Required = 1 << 1,
        /// <summary>
        /// Must not be exported by the submit form action.
        /// </summary>
        NoExport = 1 << 2,
        /// <summary>
        /// For radio buttons, one radio button must be set at all times.
        /// </summary>
        NoToggleToOff = 1 << 14,
        /// <summary>
        /// The field is a set of radio buttons.
        /// </summary>
        Radio = 1 << 15,
        /// <summary>
        /// The field is a push button.
        /// </summary>
        PushButton = 1 << 16,
        /// <summary>
        /// For radio buttons a group of radio buttons will toggle on/off at the same time based on their initial value.
        /// </summary>
        RadiosInUnison = 1 << 25
    }

    /// <summary>
    /// Flags specifying various characteristics of a text type field in an <see cref="AcroFieldBase"/>.
    /// </summary>
    [Flags]
    internal enum AcroTextFieldFlags : uint
    {
        /// <summary>
        /// The user may not change the value of the field.
        /// </summary>
        ReadOnly = 1 << 0,
        /// <summary>
        /// The field must have a value before the form can be submitted.
        /// </summary>
        Required = 1 << 1,
        /// <summary>
        /// Must not be exported by the submit form action.
        /// </summary>
        NoExport = 1 << 2,
        /// <summary>
        /// The field can contain multiple lines of text.
        /// </summary>
        Multiline = 1 << 12,
        /// <summary>
        /// The field is for a password and should not be displayed as text and should not be stored to file.
        /// </summary>
        Password = 1 << 13,
        /// <summary>
        /// The field represents a file path selection.
        /// </summary>
        FileSelect = 1 << 20,
        /// <summary>
        /// The text entered is not spell checked.
        /// </summary>
        DoNotSpellCheck = 1 << 22,
        /// <summary>
        /// The field does not scroll if the text exceeds the bounds of the field.
        /// </summary>
        DoNotScroll = 1 << 23,
        /// <summary>
        /// For a text field which is not a <see cref="Password"/>, <see cref="Multiline"/> or <see cref="FileSelect"/>
        /// the field text is evenly spaced by splitting into 'combs' based on the MaxLen entry in the field dictionary.
        /// </summary>
        Comb = 1 << 24,
        /// <summary>
        /// The value of the field is a rich text string.
        /// </summary>
        RichText = 1 << 25
    }

    /// <summary>
    /// Flags specifying various characteristics of a choice type field in an <see cref="AcroFieldBase"/>.
    /// </summary>
    [Flags]
    internal enum AcroChoiceFieldFlags : uint
    {
        /// <summary>
        /// The user may not change the value of the field.
        /// </summary>
        ReadOnly = 1 << 0,
        /// <summary>
        /// The field must have a value before the form can be submitted.
        /// </summary>
        Required = 1 << 1,
        /// <summary>
        /// Must not be exported by the submit form action.
        /// </summary>
        NoExport = 1 << 2,
        /// <summary>
        /// The field is a combo box.
        /// </summary>
        Combo = 1 << 17,
        /// <summary>
        /// The combo box includes an editable text box. <see cref="Combo"/> must be set.
        /// </summary>
        Edit = 1 << 18,
        /// <summary>
        /// The options should be sorted alphabetically, this should be ignored by viewer applications.
        /// </summary>
        Sort = 1 << 19,
        /// <summary>
        /// The field allows multiple options to be selected.
        /// </summary>
        MultiSelect = 1 << 21,
        /// <summary>
        /// The text entered in the field is not spell checked. <see cref="Combo"/> and <see cref="Edit"/> must be set.
        /// </summary>
        DoNotSpellCheck = 1 << 22,
        /// <summary>
        /// Any associated field action is fired when the selection is changed rather than on losing focus.
        /// </summary>
        CommitOnSelectionChange = 1 << 26
    }
}
