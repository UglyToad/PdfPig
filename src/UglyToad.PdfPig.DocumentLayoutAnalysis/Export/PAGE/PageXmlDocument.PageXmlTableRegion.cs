namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.PAGE
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class PageXmlDocument
    {
        /// <summary>
        /// Tabular data in any form is represented with a table
        /// region.Rows and columns may or may not have separator
        /// lines; these lines are not separator regions.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCode("xsd", "4.6.1055.0")]
        [Serializable()]
        [DebuggerStepThrough()]
        [DesignerCategory("code")]
        [XmlType(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlTableRegion : PageXmlRegion
        {
            #region private
            private PageXmlGridPoints[] gridField;

            private float orientationField;

            private bool orientationFieldSpecified;

            private int rowsField;

            private bool rowsFieldSpecified;

            private int columnsField;

            private bool columnsFieldSpecified;

            private PageXmlColourSimpleType lineColourField;

            private bool lineColourFieldSpecified;

            private PageXmlColourSimpleType bgColourField;

            private bool bgColourFieldSpecified;

            private bool lineSeparatorsField;

            private bool lineSeparatorsFieldSpecified;

            private bool embTextField;

            private bool embTextFieldSpecified;
            #endregion

            /// <summary>
            /// Table grid (visible or virtual grid lines)
            /// </summary>
            [XmlArrayItem("GridPoints", IsNullable = false)]
            public PageXmlGridPoints[] Grid
            {
                get
                {
                    return this.gridField;
                }
                set
                {
                    this.gridField = value;
                }
            }

            /// <summary>
            /// The angle the rectangle encapsulating a	region
            /// has to be rotated in clockwise direction
            /// in order to correct the present skew
            /// (negative values indicate anti-clockwise rotation).
            /// Range: -179.999,180
            /// </summary>
            [XmlAttribute("orientation")]
            public float Orientation
            {
                get
                {
                    return this.orientationField;
                }
                set
                {
                    this.orientationField = value;
                    this.orientationFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool OrientationSpecified
            {
                get
                {
                    return this.orientationFieldSpecified;
                }
                set
                {
                    this.orientationFieldSpecified = value;
                }
            }

            /// <summary>
            /// The number of rows present in the table
            /// </summary>
            [XmlAttribute("rows")]
            public int Rows
            {
                get
                {
                    return this.rowsField;
                }
                set
                {
                    this.rowsField = value;
                    this.rowsFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool RowsSpecified
            {
                get
                {
                    return this.rowsFieldSpecified;
                }
                set
                {
                    this.rowsFieldSpecified = value;
                }
            }

            /// <summary>
            /// The number of columns present in the table
            /// </summary>
            [XmlAttribute("columns")]
            public int Columns
            {
                get
                {
                    return this.columnsField;
                }
                set
                {
                    this.columnsField = value;
                    this.columnsFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool ColumnsSpecified
            {
                get
                {
                    return this.columnsFieldSpecified;
                }
                set
                {
                    this.columnsFieldSpecified = value;
                }
            }

            /// <summary>
            /// The colour of the lines used in the region
            /// </summary>
            [XmlAttribute("lineColour")]
            public PageXmlColourSimpleType LineColour
            {
                get
                {
                    return this.lineColourField;
                }
                set
                {
                    this.lineColourField = value;
                    this.lineColourFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool LineColourSpecified
            {
                get
                {
                    return this.lineColourFieldSpecified;
                }
                set
                {
                    this.lineColourFieldSpecified = value;
                }
            }

            /// <summary>
            /// The background colour of the region
            /// </summary>
            [XmlAttribute("bgColour")]
            public PageXmlColourSimpleType BgColour
            {
                get
                {
                    return this.bgColourField;
                }
                set
                {
                    this.bgColourField = value;
                    this.bgColourFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool BgColourSpecified
            {
                get
                {
                    return this.bgColourFieldSpecified;
                }
                set
                {
                    this.bgColourFieldSpecified = value;
                }
            }

            /// <summary>
            /// Specifies the presence of line separators
            /// </summary>
            [XmlAttribute("lineSeparators")]
            public bool LineSeparators
            {
                get
                {
                    return this.lineSeparatorsField;
                }
                set
                {
                    this.lineSeparatorsField = value;
                    this.lineSeparatorsFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool LineSeparatorsSpecified
            {
                get
                {
                    return this.lineSeparatorsFieldSpecified;
                }
                set
                {
                    this.lineSeparatorsFieldSpecified = value;
                }
            }

            /// <summary>
            /// Specifies whether the region also contains text
            /// </summary>
            [XmlAttribute("embText")]
            public bool EmbText
            {
                get
                {
                    return this.embTextField;
                }
                set
                {
                    this.embTextField = value;
                    this.embTextFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool EmbTextSpecified
            {
                get
                {
                    return this.embTextFieldSpecified;
                }
                set
                {
                    this.embTextFieldSpecified = value;
                }
            }
        }
    }
}
