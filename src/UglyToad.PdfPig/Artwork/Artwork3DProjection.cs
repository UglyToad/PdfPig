namespace UglyToad.PdfPig.Artwork
{
    /// <summary>
    /// The mapping of 3D camera coordinates onto the target coordinate system of the annotation
    /// (ISO 32000-2, Table 316).
    /// </summary>
    public sealed class Artwork3DProjection
    {
        /// <summary>
        /// The type of projection (<c>Subtype</c>).
        /// </summary>
        public Artwork3DProjectionType Type { get; }

        /// <summary>
        /// The clipping style (<c>CS</c>).
        /// </summary>
        public Artwork3DClippingStyle ClippingStyle { get; }

        /// <summary>
        /// The far clipping distance (<c>F</c>), meaningful only with
        /// <see cref="Artwork3DClippingStyle.ExplicitNearFar"/>. <see langword="null"/> when absent.
        /// </summary>
        public double? Far { get; }

        /// <summary>
        /// The near clipping distance (<c>N</c>), meaningful only with
        /// <see cref="Artwork3DClippingStyle.ExplicitNearFar"/>. <see langword="null"/> when absent.
        /// </summary>
        public double? Near { get; }

        /// <summary>
        /// The field of view of the virtual camera in degrees, between 0 and 180 (<c>FOV</c>). Used by
        /// perspective projections. <see langword="null"/> when absent.
        /// </summary>
        public double? FieldOfView { get; }

        /// <summary>
        /// How a perspective projection is scaled to the 3D view box (<c>PS</c>):
        /// <see cref="Artwork3DScaling.Width"/> (the default), <see cref="Artwork3DScaling.Height"/>,
        /// <see cref="Artwork3DScaling.Min"/>, <see cref="Artwork3DScaling.Max"/>, or
        /// <see cref="Artwork3DScaling.Value"/> when an explicit diameter is given in
        /// <see cref="PerspectiveScalingValue"/>.
        /// </summary>
        public Artwork3DScaling PerspectiveScaling { get; }

        /// <summary>
        /// The explicit diameter, in the annotation's target coordinate system, of the circle formed by the
        /// near plane and the field-of-view cone. Set only when <see cref="PerspectiveScaling"/> is
        /// <see cref="Artwork3DScaling.Value"/>.
        /// </summary>
        public double? PerspectiveScalingValue { get; }

        /// <summary>
        /// The scale factor applied to x and y by an orthographic projection (<c>OS</c>). Default 1.
        /// </summary>
        public double OrthographicScale { get; }

        /// <summary>
        /// How an orthographic projection is bound to the annotation, in addition to
        /// <see cref="OrthographicScale"/> (<c>OB</c>). Default <see cref="Artwork3DScaling.Absolute"/>.
        /// </summary>
        public Artwork3DScaling OrthographicBinding { get; }

        internal Artwork3DProjection(
            Artwork3DProjectionType type,
            Artwork3DClippingStyle clippingStyle,
            double? far,
            double? near,
            double? fieldOfView,
            Artwork3DScaling perspectiveScaling,
            double? perspectiveScalingValue,
            double orthographicScale,
            Artwork3DScaling orthographicBinding)
        {
            Type = type;
            ClippingStyle = clippingStyle;
            Far = far;
            Near = near;
            FieldOfView = fieldOfView;
            PerspectiveScaling = perspectiveScaling;
            PerspectiveScalingValue = perspectiveScalingValue;
            OrthographicScale = orthographicScale;
            OrthographicBinding = orthographicBinding;
        }
    }
}
