namespace UglyToad.PdfPig.Cos
{
    internal abstract class CosBase : ICosObject
    {
        public bool Direct { get; set; }

        public CosBase GetCosObject()
        {
            return this;
        }

        public abstract object Accept(ICosVisitor visitor);
    }

    internal interface ICosObject
    {
        CosBase GetCosObject();
    }
}
