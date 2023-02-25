namespace UglyToad.PdfPig.Graphics
{
    using System;
    using Colors;
    using Core;
    using PdfPig.Core;
    using Tokens;
    using Util.JetBrains.Annotations;
    using XObjects;

    /// <summary>
    /// An XObject content record.
    /// </summary>
    public class XObjectContentRecord
    {
        /// <summary>
        /// The XObject type.
        /// </summary>
        public XObjectType Type { get; }

        /// <summary>
        /// XObject stream.
        /// </summary>
        [NotNull]
        public StreamToken Stream { get; }

        /// <summary>
        /// The applied transformation.
        /// </summary>
        public TransformationMatrix AppliedTransformation { get; }

        /// <summary>
        /// The default rendering intent.
        /// </summary>
        public RenderingIntent DefaultRenderingIntent { get; }

        /// <summary>
        /// The default color space.
        /// </summary>
        public ColorSpace DefaultColorSpace { get; }

        internal XObjectContentRecord(XObjectType type, StreamToken stream, TransformationMatrix appliedTransformation,
            RenderingIntent defaultRenderingIntent,
            ColorSpace defaultColorSpace)
        {
            Type = type;
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            AppliedTransformation = appliedTransformation;
            DefaultRenderingIntent = defaultRenderingIntent;
            DefaultColorSpace = defaultColorSpace;
        }
    }
}
