namespace UglyToad.Pdf.Core
{
    internal interface IDeepCloneable<out T>
    {
        T DeepClone();
    }
}
