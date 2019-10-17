namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Indicates the alignment of the paragraph. Could be left, right, center or justify.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [XmlType(AnonymousType = true, Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public enum AltoParagraphStyleAlign
        {
            /// <remarks/>
            Left,
            /// <remarks/>
            Right,
            /// <remarks/>
            Center,
            /// <remarks/>
            Block
        }
    }
}
