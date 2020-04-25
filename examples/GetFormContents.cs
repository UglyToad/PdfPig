namespace UglyToad.Examples
{
    using System;
    using System.Linq;
    using PdfPig;
    using PdfPig.AcroForms.Fields;

    internal static class GetFormContents
    {
        public static void Run(string filePath)
        {
            using (var document = PdfDocument.Open(filePath))
            {
                if (!document.TryGetForm(out var form))
                {
                    Console.WriteLine($"No form found in file: {filePath}.");
                    return;
                }

                var page1Fields = form.GetFieldsForPage(1);

                foreach (var field in page1Fields)
                {
                    switch (field)
                    {
                        case AcroTextField text:
                            Console.WriteLine($"Found text field on page 1 with text: {text.Value}.");
                            break;
                        case AcroCheckboxesField cboxes:
                            Console.WriteLine($"Found checkboxes field on page 1 with {cboxes.Children.Count} checkboxes.");
                            break;
                        case AcroListBoxField listbox:
                            var opts = string.Join(", ", listbox.Options.Select(x => x.Name));
                            Console.WriteLine($"Found listbox field on page 1 with options: {opts}.");
                            break;
                    }
                }
            }
        }
    }
}