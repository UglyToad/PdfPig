namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Tokens;

    /// <summary>
    /// A marked content element can be used to provide application specific data in the
    /// page's content stream. Interpretation of the marked content is outside of the PDF specification.
    /// </summary>
    public class MarkedContentElement
    {
        /// <summary>
        /// Marked-content identifier.
        /// </summary>
        public int MarkedContentIdentifier { get; }

        /// <summary>
        /// The index of this marked content element in the set of marked content in the page.
        /// <see cref="Children"/> marked content elements will have the same index as the parent.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// A name indicating the role or significance of the point.
        /// </summary>
        public string Tag { get; }

        /// <summary>
        /// The properties for this element.
        /// </summary>
        public DictionaryToken Properties { get; }

        /// <summary>
        /// Is the marked content an artifact, see <see cref="ArtifactMarkedContentElement"/>.
        /// </summary>
        public bool IsArtifact { get; }

        /// <summary>
        /// Child contents.
        /// </summary>
        public IReadOnlyList<MarkedContentElement> Children { get; }

        /// <summary>
        /// Letters contained in this marked content.
        /// </summary>
        public IReadOnlyList<Letter> Letters { get; }

        /// <summary>
        /// Paths contained in this marked content.
        /// </summary>
        public IReadOnlyList<PdfPath> Paths { get; }

        /// <summary>
        /// Images contained in this marked content.
        /// </summary>
        public IReadOnlyList<IPdfImage> Images { get; }

        /// <summary>
        /// The natural language specification.
        /// </summary>
        public string Language { get; }

        /// <summary>
        /// The replacement text.
        /// </summary>
        public string ActualText { get; }

        /// <summary>
        /// The alternate description.
        /// </summary>
        public string AlternateDescription { get; }

        /// <summary>
        /// The abbreviation expansion text.
        /// </summary>
        public string ExpandedForm { get; }
        
        public MarkedContentElement(int markedContentIdentifier, NameToken tag, DictionaryToken properties, 
            string language,
            string actualText,
            string alternateDescription,
            string expandedForm,
            bool isArtifact, 
            IReadOnlyList<MarkedContentElement> children,
            IReadOnlyList<Letter> letters,
            IReadOnlyList<PdfPath> paths,
            IReadOnlyList<IPdfImage> images,
                int index)
        {
            MarkedContentIdentifier = markedContentIdentifier;
            Tag = tag;
            Language = language;
            ActualText = actualText;
            AlternateDescription = alternateDescription;
            ExpandedForm = expandedForm;
            Properties = properties ?? new DictionaryToken(new Dictionary<NameToken, IToken>());
            IsArtifact = isArtifact;

            Children = children ?? throw new ArgumentNullException(nameof(children));
            Letters = letters ?? throw new ArgumentNullException(nameof(letters));
            Paths = paths ?? throw new ArgumentNullException(nameof(paths));
            Images = images ?? throw new ArgumentNullException(nameof(images));

            Index = index;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Index={Index}, MCID={MarkedContentIdentifier}, Tag={Tag}, Properties={Properties}, Contents={Children.Count}";
        }
    }
}
