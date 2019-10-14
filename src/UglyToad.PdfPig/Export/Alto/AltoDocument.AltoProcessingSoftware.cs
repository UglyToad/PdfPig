namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] Information about a software application. Where applicable, the preferred method
        /// for determining this information is by selecting Help -- About.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoProcessingSoftware
        {
            /// <summary>
            /// The name of the organization or company that created the application.
            /// </summary>
            [XmlAttribute("softwareCreator")]
            public string SoftwareCreator { get; set; }

            /// <summary>
            /// The name of the application.
            /// </summary>
            [XmlAttribute("softwareName")]
            public string SoftwareName { get; set; }

            /// <summary>
            /// The version of the application.
            /// </summary>
            [XmlAttribute("softwareVersion")]
            public string SoftwareVersion { get; set; }

            /// <summary>
            /// A description of any important characteristics of the application, especially for
            /// non-commercial applications. For example, if a non-commercial application is built
            /// using commercial components, e.g., an OCR engine SDK. Those components should be mentioned here.
            /// </summary>
            [XmlAttribute("applicationDescription")]
            public string ApplicationDescription { get; set; }
        }


    }
}
