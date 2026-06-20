namespace UglyToad.PdfPig.Graphics.Colors.Icc
{
    using System.Diagnostics.CodeAnalysis;
    using Core;

    /// <summary>
    /// A parsed ICC profile bound to a fixed input-component count, but
    /// independent of rendering intent. Implementations cache per-intent
    /// <see cref="IIccTransform"/> handles internally so repeated calls
    /// with the same intent return the same instance.
    /// Must be safe for concurrent reads (a single instance is shared
    /// across every paint and image operation that references the
    /// corresponding <see cref="ICCBasedColorSpaceDetails"/>).
    /// </summary>
    public interface IIccProfile
    {
        /// <summary>The profile's input component count (1, 3 or 4).</summary>
        int NumberOfComponents { get; }

        /// <summary>
        /// Resolve a transform for the given rendering intent.
        /// Returning <c>false</c> means the backend cannot honour the
        /// requested intent; the caller may retry with
        /// <see cref="RenderingIntent.RelativeColorimetric"/> (the PDF
        /// default) or fall back to the alternate color space.
        /// </summary>
        bool TryGetTransform(
            RenderingIntent intent,
            [NotNullWhen(true)] out IIccTransform? transform);
    }
}