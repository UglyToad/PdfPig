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
        /// Data for a region that takes on the role of a table cell within a parent table region.
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCode("xsd", "4.6.1055.0")]
        [Serializable()]
        [DebuggerStepThrough()]
        [DesignerCategory("code")]
        [XmlType(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlTableCellRole
        {

            private int rowIndexField;

            private int columnIndexField;

            private int rowSpanField;

            private bool rowSpanFieldSpecified;

            private int colSpanField;

            private bool colSpanFieldSpecified;

            private bool headerField;

            private bool headerFieldSpecified;

            /// <summary>
            /// Cell position in table starting with row 0
            /// </summary>
            [XmlAttribute("rowIndex")]
            public int RowIndex
            {
                get
                {
                    return this.rowIndexField;
                }
                set
                {
                    this.rowIndexField = value;
                }
            }

            /// <summary>
            /// Cell position in table starting with column 0
            /// </summary>
            [XmlAttribute("columnIndex")]
            public int ColumnIndex
            {
                get
                {
                    return this.columnIndexField;
                }
                set
                {
                    this.columnIndexField = value;
                }
            }

            /// <summary>
            /// Number of rows the cell spans (optional; default is 1)
            /// </summary>
            [XmlAttribute("rowSpan")]
            public int RowSpan
            {
                get
                {
                    return this.rowSpanField;
                }
                set
                {
                    this.rowSpanField = value;
                    this.rowSpanFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool RowSpanSpecified
            {
                get
                {
                    return this.rowSpanFieldSpecified;
                }
                set
                {
                    this.rowSpanFieldSpecified = value;
                }
            }

            /// <summary>
            /// Number of columns the cell spans (optional; default is 1)
            /// </summary>
            [XmlAttribute("colSpan")]
            public int ColSpan
            {
                get
                {
                    return this.colSpanField;
                }
                set
                {
                    this.colSpanField = value;
                    this.colSpanFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool ColSpanSpecified
            {
                get
                {
                    return this.colSpanFieldSpecified;
                }
                set
                {
                    this.colSpanFieldSpecified = value;
                }
            }

            /// <summary>
            /// Is the cell a column or row header?
            /// </summary>
            [XmlAttribute("header")]
            public bool Header
            {
                get
                {
                    return this.headerField;
                }
                set
                {
                    this.headerField = value;
                    this.headerFieldSpecified = true;
                }
            }

            /// <remarks/>
            [XmlIgnore()]
            public bool HeaderSpecified
            {
                get
                {
                    return this.headerFieldSpecified;
                }
                set
                {
                    this.headerFieldSpecified = value;
                }
            }
        }
    }
}
