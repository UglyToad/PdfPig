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
    /// XObjectContentRecord
    /// </summary>
    public class XObjectContentRecord
    {
        /// <summary>
        /// Type
        /// </summary>
        public XObjectType Type { get; }

        /// <summary>
        /// Stream
        /// </summary>
        [NotNull]
        public StreamToken Stream { get; }

        /// <summary>
        /// AppliedTransformation
        /// </summary>
        public TransformationMatrix AppliedTransformation { get; }

        /// <summary>
        /// DefaultRenderingIntent
        /// </summary>
        public RenderingIntent DefaultRenderingIntent { get; }

        /// <summary>
        /// DefaultColorSpace
        /// </summary>
        public ColorSpace DefaultColorSpace { get; }

        /// <summary>
        /// XObjectContentRecord
        /// </summary>
        /// <param name="type"></param>
        /// <param name="stream"></param>
        /// <param name="appliedTransformation"></param>
        /// <param name="defaultRenderingIntent"></param>
        /// <param name="defaultColorSpace"></param>
        public XObjectContentRecord(XObjectType type, StreamToken stream, TransformationMatrix appliedTransformation,
            RenderingIntent defaultRenderingIntent, ColorSpace defaultColorSpace)
        {
            Type = type;
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            AppliedTransformation = appliedTransformation;
            DefaultRenderingIntent = defaultRenderingIntent;
            DefaultColorSpace = defaultColorSpace;
        }
    }
}
