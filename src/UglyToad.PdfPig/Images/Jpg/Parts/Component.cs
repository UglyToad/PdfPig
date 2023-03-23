namespace UglyToad.PdfPig.Images.Jpg.Parts
{
    internal class Component
    {
        internal int cid;
        /// <summary>
        /// Horizontal sampling factor        
        /// Possible Values: 1-4
        /// Specifies the relationship between the component horizontal dimension
        /// and maximum image dimension X; also specifies the number of horizontal data units of component
        /// Ci in each MCU, when more than one component is encoded in a scan.
        /// </summary>
        internal int HSF;
        /// <summary>
        /// Vertical sampling factor         
        /// Specifies the relationship between the component vertical dimension and
        /// maximum image dimension Y; also specifies the number of vertical data units of component Ci in
        /// each MCU, when more than one component is encoded in a scan.
        /// </summary>
        internal int VSF;
        internal int width, height;
        internal int stride;
        internal int qtsel;
        internal int actabsel, dctabsel;
        internal int dcpred;
        internal byte[] pixels;
    }
}
