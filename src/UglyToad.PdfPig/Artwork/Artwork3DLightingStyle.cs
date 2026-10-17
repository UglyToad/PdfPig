namespace UglyToad.PdfPig.Artwork
{
    /// <summary>
    /// The lighting scheme used to render 3D artwork (ISO 32000-2, Table 321).
    /// </summary>
    public enum Artwork3DLightingStyle : byte
    {
        /// <summary>
        /// A lighting scheme this library does not recognise. The lights of the 3D artwork should be used.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The lights specified in the 3D artwork.
        /// </summary>
        Artwork = 1,

        /// <summary>
        /// No lights; lights in the 3D artwork are ignored.
        /// </summary>
        None = 2,

        /// <summary>
        /// Three blue-grey infinite lights.
        /// </summary>
        White = 3,

        /// <summary>
        /// Three light-grey infinite lights.
        /// </summary>
        Day = 4,

        /// <summary>
        /// One yellow, one aqua and one blue infinite light.
        /// </summary>
        Night = 5,
        /// <summary>
        /// Three grey infinite lights.
        /// </summary>
        Hard = 6,

        /// <summary>
        /// One red, one green and one blue infinite light.
        /// </summary>
        Primary = 7,

        /// <summary>
        /// Three blue infinite lights.
        /// </summary>
        Blue = 8,

        /// <summary>
        /// Three red infinite lights.
        /// </summary>
        Red = 9,

        /// <summary>
        /// Six grey infinite lights aligned with the major axes.
        /// </summary>
        Cube = 10,

        /// <summary>
        /// Three grey infinite lights and one light attached to the camera.
        /// </summary>
        CAD = 11,

        /// <summary>
        /// A single infinite light attached to the camera.
        /// </summary>
        Headlamp = 12
    }
}
