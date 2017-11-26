namespace UglyToad.Pdf.Core
{
    public interface IDeepCloneable<out T>
    {
        T DeepClone();
    }
}
