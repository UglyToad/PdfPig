namespace UglyToad.PdfPig.Export.Alto
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public partial class AltoDocument
    {
        /// <summary>
        /// Encapsulates width/height and position data.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Serializable]
        [DebuggerStepThrough]
        public abstract class AltoPositionedElement
        {
            private float height;
            private float width;
            private float horizontalPosition;
            private float verticalPosition;

            /// <summary>
            /// Height.
            /// </summary>
            [XmlAttribute("HEIGHT")]
            public float Height
            {
                get => height;
                set
                {
                    height = value;
                    if (!float.IsNaN(value)) HeightSpecified = true;
                }
            }

            /// <summary>
            /// Whether to include <see cref="Height"/> in the output.
            /// </summary>
            [XmlIgnore]
            public bool HeightSpecified { get; private set; }

            /// <summary>
            /// Width.
            /// </summary>
            [XmlAttribute("WIDTH")]
            public float Width
            {
                get => width;
                set
                {
                    width = value;
                    if (!float.IsNaN(value)) WidthSpecified = true;
                }
            }

            /// <summary>
            /// Whether to include <see cref="Width"/> in the output.
            /// </summary>
            [XmlIgnore]
            public bool WidthSpecified { get; private set; }

            /// <summary>
            /// Horizontal position.
            /// </summary>
            [XmlAttribute("HPOS")]
            public float HorizontalPosition
            {
                get => horizontalPosition;
                set
                {
                    horizontalPosition = value;
                    if (!float.IsNaN(value)) HorizontalPositionSpecified = true;
                }
            }

            /// <summary>
            /// Whether to include <see cref="HorizontalPosition"/> in the output.
            /// </summary>
            [XmlIgnore]
            public bool HorizontalPositionSpecified { get; private set; }

            /// <summary>
            /// Vertical position.
            /// </summary>
            [XmlAttribute("VPOS")]
            public float VerticalPosition
            {
                get => verticalPosition;
                set
                {
                    verticalPosition = value;
                    if (!float.IsNaN(value)) VerticalPositionSpecified = true;
                }
            }

            /// <summary>
            /// Whether to include <see cref="VerticalPosition"/> in the output.
            /// </summary>
            [XmlIgnore]
            public bool VerticalPositionSpecified { get; private set; }
        }


    }
}
