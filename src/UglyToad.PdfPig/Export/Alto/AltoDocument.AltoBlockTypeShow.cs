namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto/xlink] Block Type Show.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [XmlType(AnonymousType = true, Namespace = "http://www.w3.org/1999/xlink")]
        public enum AltoBlockTypeShow
        {
            /// <remarks/>
            [XmlEnum("new")]
            New,
            /// <remarks/>
            [XmlEnum("replace")]
            Replace,
            /// <remarks/>
            [XmlEnum("embed")]
            Embed,
            /// <remarks/>
            [XmlEnum("other")]
            Other,
            /// <remarks/>
            [XmlEnum("none")]
            None
        }
    }
}
