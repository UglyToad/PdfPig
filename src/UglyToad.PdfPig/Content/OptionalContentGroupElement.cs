using System;
using System.Collections.Generic;
using UglyToad.PdfPig.Tokenization.Scanner;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Content
{
    /// <summary>
    /// An optional content group is a dictionary representing a collection of graphics
    /// that can be made visible or invisible dynamically by users of viewers applications.
    /// </summary>
    public class OptionalContentGroupElement
    {
        /// <summary>
        /// The type of PDF object that this dictionary describes.
        /// <para>Must be OCG for an optional content group dictionary.</para>
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// The name of the optional content group, suitable for presentation in a viewer application's user interface.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A single name or an array containing any combination of names.
        /// <para>Default value is 'View'.</para>
        /// </summary>
        public IReadOnlyList<string> Intent { get; }

        /// <summary>
        /// A usage dictionary describing the nature of the content controlled by the group.
        /// </summary>
        public IReadOnlyDictionary<string, IToken> Usage { get; }

        /// <summary>
        /// Underlying <see cref="MarkedContentElement"/>.
        /// </summary>
        public MarkedContentElement MarkedContent { get; }

        internal OptionalContentGroupElement(MarkedContentElement markedContentElement, IPdfTokenScanner  pdfTokenScanner)
        {
            MarkedContent = markedContentElement;

            // Type - Required
            if (markedContentElement.Properties.TryGet(NameToken.Type, pdfTokenScanner, out NameToken type))
            {
                Type = type.Data;
            }
            else if (markedContentElement.Properties.TryGet(NameToken.Type, pdfTokenScanner, out StringToken typeStr))
            {
                Type = typeStr.Data;
            }
            else
            {
                throw new ArgumentException($"Cannot parse optional content's {nameof(Type)} from {nameof(markedContentElement.Properties)}. This is a required field.", nameof(markedContentElement.Properties));
            }

            switch (Type)
            {
                case "OCG": // Optional content group dictionary
                    // Name - Required
                    if (markedContentElement.Properties.TryGet(NameToken.Name, pdfTokenScanner, out NameToken name))
                    {
                        Name = name.Data;
                    }
                    else if (markedContentElement.Properties.TryGet(NameToken.Name, pdfTokenScanner, out StringToken nameStr))
                    {
                        Name = nameStr.Data;
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot parse optional content's {nameof(Name)} from {nameof(markedContentElement.Properties)}. This is a required field.", nameof(markedContentElement.Properties));
                    }

                    // Intent - Optional
                    if (markedContentElement.Properties.TryGet(NameToken.Intent, pdfTokenScanner, out NameToken intentName))
                    {
                        Intent = new string[] { intentName.Data };
                    }
                    else if (markedContentElement.Properties.TryGet(NameToken.Intent, pdfTokenScanner, out StringToken intentStr))
                    {
                        Intent = new string[] { intentStr.Data };
                    }
                    else if (markedContentElement.Properties.TryGet(NameToken.Intent, pdfTokenScanner, out ArrayToken intentArray))
                    {
                        List<string> intentList = new List<string>();
                        foreach (var token in intentArray.Data)
                        {
                            if (token is NameToken nameA)
                            {
                                intentList.Add(nameA.Data);
                            }
                            else if (token is StringToken strA)
                            {
                                intentList.Add(strA.Data);
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                        }
                        Intent = intentList;
                    }
                    else
                    {
                        // Default value is 'View'.
                        Intent = new string[] { "View" };
                    }

                    // Usage - Optional
                    if (markedContentElement.Properties.TryGet(NameToken.Usage, pdfTokenScanner, out DictionaryToken usage))
                    {
                        this.Usage = usage.Data;
                    }
                    break;

                case "OCMD":
                    // OCGs - Optional
                    if (markedContentElement.Properties.TryGet(NameToken.Ocgs, pdfTokenScanner, out DictionaryToken ocgsD))
                    {
                        // dictionary or array
                        throw new NotImplementedException($"{NameToken.Ocgs}");
                    }
                    else if (markedContentElement.Properties.TryGet(NameToken.Ocgs, pdfTokenScanner, out ArrayToken ocgsA))
                    {
                        // dictionary or array
                        throw new NotImplementedException($"{NameToken.Ocgs}");
                    }

                    // P - Optional
                    if (markedContentElement.Properties.TryGet(NameToken.P, pdfTokenScanner, out NameToken p))
                    {
                        throw new NotImplementedException($"{NameToken.P}");
                    }

                    // VE - Optional
                    if (markedContentElement.Properties.TryGet(NameToken.VE, pdfTokenScanner, out ArrayToken ve))
                    {
                        throw new NotImplementedException($"{NameToken.VE}");
                    }
                    break;

                default:
                    throw new ArgumentException($"Unknown Optional Content of type '{Type}' not known.", nameof(Type));
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override string ToString()
        {
            return $"{Type} - {Name} [{string.Join(",", Intent)}]: {MarkedContent?.ToString()}";
        }
    }
}
