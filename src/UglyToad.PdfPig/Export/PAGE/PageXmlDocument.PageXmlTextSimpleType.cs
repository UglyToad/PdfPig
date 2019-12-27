namespace UglyToad.PdfPig.Export.PAGE
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class PageXmlDocument
    {
        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlTextSimpleType
        {

            /// <summary>
            /// Paragraph
            /// </summary>
            [XmlEnumAttribute("paragraph")]
            Paragraph,

            /// <summary>
            /// Heading
            /// </summary>
            [XmlEnumAttribute("heading")]
            Heading,

            /// <summary>
            /// Caption
            /// </summary>
            [XmlEnumAttribute("caption")]
            Caption,

            /// <summary>
            /// Header
            /// </summary>
            [XmlEnumAttribute("header")]
            Header,

            /// <summary>
            /// Footer
            /// </summary>
            [XmlEnumAttribute("footer")]
            Footer,

            /// <summary>
            /// Page number
            /// </summary>
            [XmlEnumAttribute("page-number")]
            PageNumber,

            /// <summary>
            /// Drop Capital, a letter a the beginning of a word that is bigger than the usual character size. Usually to start a chapter.
            /// </summary>
            [XmlEnumAttribute("drop-capital")]
            DropCapital,

            /// <summary>
            /// Credit
            /// </summary>
            [XmlEnumAttribute("credit")]
            Credit,

            /// <summary>
            /// Floating
            /// </summary>
            [XmlEnumAttribute("floating")]
            Floating,

            /// <summary>
            /// Signature mark
            /// </summary>
            [XmlEnumAttribute("signature-mark")]
            SignatureMark,

            /// <summary>
            /// Catch word
            /// </summary>
            [XmlEnumAttribute("catch-word")]
            CatchWord,

            /// <summary>
            /// Marginalia
            /// </summary>
            [XmlEnumAttribute("marginalia")]
            Marginalia,

            /// <summary>
            /// Foot note
            /// </summary>
            [XmlEnumAttribute("footnote")]
            FootNote,

            /// <summary>
            /// Foot note - continued
            /// </summary>
            [XmlEnumAttribute("footnote-continued")]
            FootNoteContinued,

            /// <summary>
            /// End note
            /// </summary>
            [XmlEnumAttribute("endnote")]
            EndNote,

            /// <summary>
            /// Table of content
            /// </summary>
            [XmlEnumAttribute("TOC-entry")]
            TocEntry,

            /// <summary>
            /// List
            /// </summary>
            [XmlEnumAttribute("list-label")]
            LisLabel,

            /// <summary>
            /// Other
            /// </summary>
            [XmlEnumAttribute("other")]
            Other,
        }
    }
}
