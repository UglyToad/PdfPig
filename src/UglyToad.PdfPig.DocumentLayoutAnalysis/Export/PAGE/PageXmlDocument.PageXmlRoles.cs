namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.PAGE
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class PageXmlDocument
    {
        /// <remarks/>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCode("xsd", "4.6.1055.0")]
        [Serializable()]
        [DebuggerStepThrough()]
        [DesignerCategory("code")]
        [XmlType(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public class PageXmlRoles
        {

            private PageXmlTableCellRole tableCellRoleField;

            /// <summary>
            /// Data for a region that takes on the role of a table cell within a parent table region.
            /// </summary>
            public PageXmlTableCellRole TableCellRole
            {
                get
                {
                    return this.tableCellRoleField;
                }
                set
                {
                    this.tableCellRoleField = value;
                }
            }
        }
    }
}
