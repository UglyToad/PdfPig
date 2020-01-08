namespace UglyToad.PdfPig.Content
{
    using System.Collections.Generic;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Graphics;
    using UglyToad.PdfPig.Tokens;
    using UglyToad.PdfPig.Util;

    /// <summary>
    /// 
    /// </summary>
    public class PdfMarkedContent
    {
        private readonly List<IPdfImage> images = new List<IPdfImage>();
        private readonly List<PdfPath> pdfPaths = new List<PdfPath>();
        private readonly List<Letter> letters = new List<Letter>();
        private readonly List<XObjectContentRecord> xObjectContentRecords = new List<XObjectContentRecord>();

        internal PdfMarkedContent(int id, NameToken tag, DictionaryToken properties)
        {
            this.Id = id;
            this.Tag = tag;
            this.Properties = properties;
            this.ChildContents = new List<PdfMarkedContent>();
        }

        /// <summary>
        /// Is the marked content an artifact.
        /// </summary>
        public bool IsArtifact { get; internal set; }

        /// <summary>
        /// Internal Id for top marked content. Child marked contents will share the same Id as the parent.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Marked-content identifier.
        /// </summary>
        public int MCID
        {
            get
            {
                if (Properties == null) return -1;
                if (Properties.ContainsKey(NameToken.Mcid))
                {
                    return Properties.GetInt(NameToken.Mcid);
                }
                return -1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Tag { get; }

        /// <summary>
        /// Properties.
        /// </summary>
        public DictionaryToken Properties { get; }

        /// <summary>
        /// Child contents.
        /// </summary>
        public List<PdfMarkedContent> ChildContents { get; }

        /// <summary>
        /// The natural language specification.
        /// </summary>
        public string Language
        {
            get
            {
                if (Properties == null) return null;
                if (Properties.TryGet(NameToken.Lang, out IDataToken<string> langToken))
                {
                    return langToken.Data;
                }
                return null;
            }
        }

        /// <summary>
        /// The replacement text.
        /// </summary>
        public string ActualText
        {
            get
            {
                if (Properties == null) return null;
                if (Properties.TryGet(NameToken.ActualText, out IDataToken<string> textToken))
                {
                    return textToken.Data;
                }
                return null;
            }
        }

        /// <summary>
        /// The alternate description.
        /// </summary>
        public string AlternateDescription
        {
            get
            {
                if (Properties == null) return null;
                if (Properties.TryGet(NameToken.Alternate, out IDataToken<string> textToken))
                {
                    return textToken.Data;
                }
                return null;
            }
        }

        /// <summary>
        /// The abbreviation expansion text.
        /// </summary>
        public string ExpandedForm
        {
            get
            {
                if (Properties == null) return null;
                if (Properties.TryGet(NameToken.E, out IDataToken<string> textToken))
                {
                    return textToken.Data;
                }
                return null;
            }
        }

        /// <summary>
        /// The marked content's images.
        /// </summary>
        public IReadOnlyList<IPdfImage> Images => images;

        /// <summary>
        /// The marked content's paths.
        /// </summary>
        public IReadOnlyList<PdfPath> PdfPaths => pdfPaths;

        /// <summary>
        /// The marked content's letters.
        /// </summary>
        public IReadOnlyList<Letter> Letters => letters;

        internal void Add(IPdfImage pdfImage)
        {
            images.Add(pdfImage);
        }

        internal void Add(PdfPath pdfPath)
        {
            pdfPaths.Add(pdfPath);
        }

        internal void Add(Letter letter)
        {
            letters.Add(letter);
        }

        internal void Add(XObjectContentRecord xObjectContentRecord)
        {
            xObjectContentRecords.Add(xObjectContentRecord);
        }

        internal void Add(PdfMarkedContent markedContent)
        {
            ChildContents.Add(markedContent);
        }

        internal static PdfMarkedContent Create(int id, NameToken name, DictionaryToken properties)
        {
            if (name.Equals(NameToken.Artifact))
            {
                return new PdfArtifactMarkedContent(id, properties);
            }
            else
            {
                return new PdfMarkedContent(id, name, properties);
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "Id=" + Id + ", Tag=" + this.Tag + ", Properties=" + this.Properties + ", Contents=" + this.ChildContents.Count;
        }
    }
}
