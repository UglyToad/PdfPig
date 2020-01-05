namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] All measurement values inside the alto file are related to this unit, except the font size.
        /// 
        /// Coordinates as being used in HPOS and VPOS are absolute coordinates referring to the upper-left corner of a page.
        /// The upper left corner of the page is defined as coordinate (0/0). 
        /// 
        /// <para>values meaning:
        /// mm10: 1/10th of millimeter; 
        /// inch1200: 1/1200th of inch; 
        /// pixel: 1 pixel</para>
        /// 
        /// The values for pixel will be related to the resolution of the image based
        /// on which the layout is described. Incase the original image is not known
        /// the scaling factor can be calculated based on total width and height of
        /// the image and the according information of the PAGE element.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public enum AltoMeasurementUnit
        {
            /// <summary>
            /// 1 pixel.
            /// </summary>
            [XmlEnum("pixel")]
            Pixel,
            /// <summary>
            /// 1/10th of millimeter.
            /// </summary>
            [XmlEnum("mm10")]
            Mm10,
            /// <summary>
            /// 1/1200th of inch.
            /// </summary>
            [XmlEnum("inch1200")]
            Inch1200,
        }
    }
}
