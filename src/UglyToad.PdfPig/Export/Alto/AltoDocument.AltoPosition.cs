namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Position of the page. Could be lefthanded, righthanded, cover, foldout or single if it has no special position.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public enum AltoPosition
        {
            /// <summary>
            /// Left page.
            /// </summary>
            Left,
            /// <summary>
            /// Right page.
            /// </summary>
            Right,
            /// <summary>
            /// Foldout page.
            /// </summary>
            Foldout,
            /// <summary>
            /// Single page.
            /// </summary>
            Single,
            /// <summary>
            /// Cover page.
            /// </summary>
            Cover
        }
    }
}
