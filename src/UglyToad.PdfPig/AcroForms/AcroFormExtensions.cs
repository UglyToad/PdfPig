namespace UglyToad.PdfPig.AcroForms
{
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.AcroForms.Fields;

    /// <summary>
    /// Extensions for AcroForm.
    /// </summary>
    public static class AcroFormExtensions
    {
        /// <summary>
        /// Get fields containing data in form.
        /// </summary>
        public static IEnumerable<AcroFieldBase> GetFields(this AcroForm form)
        {
            return form.Fields.SelectMany(f => f.GetFields());
        }

        /// <summary>
        /// Get fields containing data which are children of field.
        /// </summary>
        public static IEnumerable<AcroFieldBase> GetFields(this AcroFieldBase fieldBase)
        {
            if (fieldBase.FieldType != AcroFieldType.Unknown)
            {
                yield return fieldBase;
            }

            if (fieldBase is AcroNonTerminalField nonTerminalField)
            {
                foreach (var child in nonTerminalField.Children)
                {
                    foreach (var item in child.GetFields())
                    {
                        yield return item;
                    }
                }
            }
        }

        /// <summary>
        /// Get string values of field.
        /// </summary>
        public static KeyValuePair<string, string> GetFieldValue(this AcroFieldBase fieldBase)
        {
            if (fieldBase is AcroTextField textField)
            {
                return new KeyValuePair<string, string>(textField.Information.PartialName, textField.Value);
            }
            else if (fieldBase is AcroCheckboxField checkboxField)
            {
                return new KeyValuePair<string, string>(checkboxField.Information.PartialName, checkboxField.IsChecked.ToString());
            }
            return new KeyValuePair<string, string>(fieldBase.Information.PartialName, "");
        }
    }
}
