namespace UglyToad.PdfPig.Graphics.Colors
{
    using Core;
    using System;
    using Functions;

    /// <summary>
    /// <see cref="ColorSpaceDetails"/> that have a tint function: <see cref="SeparationColorSpaceDetails"/> and <see cref="DeviceNColorSpaceDetails"/>.
    /// </summary>
    internal static class TintColorSpaceDetailsHelper
    {
        /// <summary>
        /// Evaluate <paramref name="tintInput"/> through a tint <paramref name="tint"/> function and write exactly
        /// <paramref name="alternate"/>'s <see cref="NumberOfColorComponents"/> output values into
        /// <paramref name="buffer"/>, which must be at least <see cref="TintBufferSize"/> long.
        /// </summary>
        /// <returns>The slice of <paramref name="buffer"/> holding the alternate space's component values.</returns>
        private static ReadOnlySpan<double> EvalTint(PdfFunction tint, ColorSpaceDetails alternate,
            ReadOnlySpan<double> tintInput, Span<double> buffer)
        {
            int alternateComponents = alternate.NumberOfColorComponents;
            int written = tint.Eval(tintInput, buffer);
            
            var destination = buffer.Slice(0, alternateComponents);
            ColorSpaceDetails.Normalise(buffer.Slice(0, written), destination);
            return destination;
        }

        /// <summary>
        /// Evaluate <paramref name="tintInput"/> through a tint <paramref name="tint"/> function whose output values
        /// are then mapped to RGB by <paramref name="alternate"/>'s <see cref="GetRgb"/>. Allocation-free for the
        /// typical case where the alternate colour space has at most <see cref="MaxStackallocComponents"/> components.
        /// </summary>
        public static void GetRgbViaTint(PdfFunction tint, ColorSpaceDetails alternate,
            ReadOnlySpan<double> tintInput, RenderingIntent intent, out double r, out double g, out double b)
        {
            int max = Math.Max(tint.MaxOutputComponentCount, alternate.NumberOfColorComponents);
            Span<double> buffer = max <= 32 ? stackalloc double[max] : new double[max];
            alternate.GetRgb(EvalTint(tint, alternate, tintInput, buffer), intent, out r, out g, out b);
        }

        /// <summary>
        /// Evaluate <paramref name="tintInput"/> through a tint <paramref name="tint"/> function whose output values
        /// are then mapped to a colour by <paramref name="alternate"/>'s <see cref="GetColor"/>. The
        /// <see cref="GetRgb"/> equivalent is <see cref="GetRgbViaTint"/>; the two must stay in step so that a
        /// colour space renders the same whichever path it is reached through.
        /// </summary>
        public static IColor GetColorViaTint(PdfFunction tint, ColorSpaceDetails alternate,
            ReadOnlySpan<double> tintInput, RenderingIntent intent)
        {
            int max = Math.Max(tint.MaxOutputComponentCount, alternate.NumberOfColorComponents);
            Span<double> buffer = max <= 32 ? stackalloc double[max] : new double[max];
            return alternate.GetColor(EvalTint(tint, alternate, tintInput, buffer), intent);
        }
    }
}
