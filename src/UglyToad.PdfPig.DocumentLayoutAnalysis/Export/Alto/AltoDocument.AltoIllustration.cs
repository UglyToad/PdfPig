namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] A picture or image.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoIllustration : AltoBlock
        {
            /// <summary>
            /// A user defined string to identify the type of illustration like photo, map, drawing, chart, ...
            /// </summary>
            [XmlAttribute("TYPE")]
            public string IllustrationType { get; set; }

            /// <summary>
            /// A link to an image which contains only the illustration.
            /// </summary>
            [XmlAttribute("FILEID")]
            public string FileId { get; set; }
        }
    }
}
