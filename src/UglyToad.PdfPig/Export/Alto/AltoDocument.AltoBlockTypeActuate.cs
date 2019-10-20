namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto/xlink] Block Type Actuate
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [XmlType(AnonymousType = true, Namespace = "http://www.w3.org/1999/xlink")]
        public enum AltoBlockTypeActuate
        {
            /// <remarks/>
            [XmlEnum("onLoad")]
            OnLoad,

            /// <remarks/>
            [XmlEnum("onRequest")]
            OnRequest,

            /// <remarks/>
            [XmlEnum("other")]
            Other,

            /// <remarks/>
            [XmlEnum("none")]
            None,
        }
    }
}
