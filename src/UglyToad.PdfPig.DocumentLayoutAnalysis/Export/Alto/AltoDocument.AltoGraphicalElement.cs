namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] A graphic used to separate blocks. Usually a line or rectangle.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoGraphicalElement : AltoBlock
        {
        }
    }
}
