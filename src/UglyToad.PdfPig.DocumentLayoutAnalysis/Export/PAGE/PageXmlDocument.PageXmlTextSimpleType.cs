namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.PAGE
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class PageXmlDocument
    {
        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCode("xsd", "4.6.1055.0")]
        [Serializable()]
        [XmlType(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlTextSimpleType
        {

            /// <summary>
            /// Paragraph
            /// </summary>
            [XmlEnum("paragraph")]
            Paragraph,

            /// <summary>
            /// Heading
            /// </summary>
            [XmlEnum("heading")]
            Heading,

            /// <summary>
            /// Caption
            /// </summary>
            [XmlEnum("caption")]
            Caption,

            /// <summary>
            /// Header
            /// </summary>
            [XmlEnum("header")]
            Header,

            /// <summary>
            /// Footer
            /// </summary>
            [XmlEnum("footer")]
            Footer,

            /// <summary>
            /// Page number
            /// </summary>
            [XmlEnum("page-number")]
            PageNumber,

            /// <summary>
            /// Drop Capital, a letter a the beginning of a word that is bigger than the usual character size. Usually to start a chapter.
            /// </summary>
            [XmlEnum("drop-capital")]
            DropCapital,

            /// <summary>
            /// Credit
            /// </summary>
            [XmlEnum("credit")]
            Credit,

            /// <summary>
            /// Floating
            /// </summary>
            [XmlEnum("floating")]
            Floating,

            /// <summary>
            /// Signature mark
            /// </summary>
            [XmlEnum("signature-mark")]
            SignatureMark,

            /// <summary>
            /// Catch word
            /// </summary>
            [XmlEnum("catch-word")]
            CatchWord,

            /// <summary>
            /// Marginalia
            /// </summary>
            [XmlEnum("marginalia")]
            Marginalia,

            /// <summary>
            /// Foot note
            /// </summary>
            [XmlEnum("footnote")]
            FootNote,

            /// <summary>
            /// Foot note - continued
            /// </summary>
            [XmlEnum("footnote-continued")]
            FootNoteContinued,

            /// <summary>
            /// End note
            /// </summary>
            [XmlEnum("endnote")]
            EndNote,

            /// <summary>
            /// Table of content
            /// </summary>
            [XmlEnum("TOC-entry")]
            TocEntry,

            /// <summary>
            /// List
            /// </summary>
            [XmlEnum("list-label")]
            LisLabel,

            /// <summary>
            /// Other
            /// </summary>
            [XmlEnum("other")]
            Other,
        }
    }
}
