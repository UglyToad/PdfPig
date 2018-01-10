namespace UglyToad.PdfPig.Core
{
    internal interface IDeepCloneable<out T>
    {
        T DeepClone();
    }
}
