namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// [Alto] One page of a document.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        [XmlType(Namespace = "http://www.loc.gov/standards/alto/ns-v4#")]
        public class AltoPage
        {
            private float height;
            private float width;
            private AltoQuality quality;
            private AltoPosition position;
            private float accuracy;
            private float pc;

            /// <summary>
            /// The area between the top line of print and the upper edge of the leaf. It may contain page number or running title.
            /// </summary>
            public AltoPageSpace TopMargin { get; set; }

            /// <summary>
            /// The area between the printspace and the left border of a page. May contain margin notes.
            /// </summary>
            public AltoPageSpace LeftMargin { get; set; }

            /// <summary>
            /// The area between the printspace and the right border of a page. May contain margin notes.
            /// </summary>
            public AltoPageSpace RightMargin { get; set; }

            /// <summary>
            /// The area between the bottom line of letterpress or writing and the bottom edge of the leaf.
            /// It may contain a page number, a signature number or a catch word.
            /// </summary>
            public AltoPageSpace BottomMargin { get; set; }

            /// <summary>
            /// Rectangle covering the printed area of a page. Page number and running title are not part of the print space.
            /// </summary>
            public AltoPageSpace PrintSpace { get; set; }

            /// <remarks/>
            [XmlAttribute("ID", DataType = "ID")]
            public string Id { get; set; }

            /// <summary>
            /// Any user-defined class like title page.
            /// </summary>
            [XmlAttribute("PAGECLASS")]
            public string PageClass { get; set; }

            /// <remarks/>
            [XmlAttribute("STYLEREFS", DataType = "IDREFS")]
            public string StyleRefs { get; set; }

            /// <remarks/>
            [XmlAttribute("PROCESSINGREFS", DataType = "IDREFS")]
            public string ProcessingRefs { get; set; }

            /// <remarks/>
            [XmlAttribute("HEIGHT")]
            public float Height
            {
                get => height;
                set
                {
                    height = value;
                    if (!float.IsNaN(value)) HeightSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool HeightSpecified { get; set; }

            /// <remarks/>
            [XmlAttribute("WIDTH")]
            public float Width
            {
                get => width;
                set
                {
                    width = value;
                    if (!float.IsNaN(value)) WidthSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool WidthSpecified { get; set; }

            /// <summary>
            /// The number of the page within the document.
            /// </summary>
            [XmlAttribute("PHYSICAL_IMG_NR")]
            public float PhysicalImgNr { get; set; }

            /// <summary>
            /// The page number that is printed on the page.
            /// </summary>
            [XmlAttribute("PRINTED_IMG_NR")]
            public string PrintedImgNr { get; set; }

            /// <remarks/>
            [XmlAttribute("QUALITY")]
            public AltoQuality Quality
            {
                get => quality;
                set
                {
                    quality = value;
                    QualitySpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool QualitySpecified { get; set; }

            /// <remarks/>
            [XmlAttribute("QUALITY_DETAIL")]
            public string QualityDetail { get; set; }

            /// <remarks/>
            [XmlAttribute("POSITION")]
            public AltoPosition Position
            {
                get => position;
                set
                {
                    position = value;
                    PositionSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool PositionSpecified { get; set; }

            /// <summary>
            /// A link to the processing description that has been used for this page.
            /// </summary>
            [XmlAttribute("PROCESSING", DataType = "IDREF")]
            public string Processing { get; set; }

            /// <summary>
            /// Estimated percentage of OCR Accuracy in range from 0 to 100
            /// </summary>
            [XmlAttribute("ACCURACY")]
            public float Accuracy
            {
                get => accuracy;
                set
                {
                    accuracy = value;
                    if (!float.IsNaN(value)) AccuracySpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool AccuracySpecified { get; set; }

            /// <summary>
            /// 
            /// </summary>
            [XmlAttribute("PC")]
            public float Pc
            {
                get => pc;
                set
                {
                    pc = value;
                    if (!float.IsNaN(value)) PcSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore]
            public bool PcSpecified { get; set; }
        }
    }
}
