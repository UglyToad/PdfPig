namespace UglyToad.PdfPig.Artwork
{
    using Tokens;

    /// <summary>
    /// When 3D artwork is activated and deactivated, and how it is presented while active
    /// (ISO 32000-2, Table 310; for rich media, the activation and deactivation dictionaries of 13.7).
    /// These are the document author's wishes; a consumer is free to activate only on user request.
    /// </summary>
    public sealed class Artwork3DActivation
    {
        /// <summary>
        /// The activation settings that apply when a document specifies none.
        /// </summary>
        public static Artwork3DActivation Default { get; } = new Artwork3DActivation(
            Artwork3DActivationMode.Explicit,
            Artwork3DInstanceState.Live,
            Artwork3DDeactivationMode.PageInvisible,
            Artwork3DInstanceState.Uninstantiated,
            showToolbar: true,
            showNavigationPanel: false,
            Artwork3DPresentationStyle.Embedded,
            window: null,
            transparent: false);

        /// <summary>
        /// The circumstances under which the artwork is activated (<c>A</c>).
        /// </summary>
        public Artwork3DActivationMode Mode { get; }

        /// <summary>
        /// The state of the artwork instance upon activation (<c>AIS</c>): instantiated or live.
        /// </summary>
        public Artwork3DInstanceState ActivationState { get; }

        /// <summary>
        /// The circumstances under which the artwork is deactivated (<c>D</c>).
        /// </summary>
        public Artwork3DDeactivationMode DeactivationMode { get; }

        /// <summary>
        /// The state of the artwork instance upon deactivation (<c>DIS</c>).
        /// </summary>
        public Artwork3DInstanceState DeactivationState { get; }

        /// <summary>
        /// Whether a toolbar is displayed by default when the artwork is activated and given focus (<c>TB</c>).
        /// </summary>
        public bool ShowToolbar { get; }

        /// <summary>
        /// Whether the user interface for navigating views and the model tree is shown when the artwork is
        /// activated (<c>NP</c>).
        /// </summary>
        public bool ShowNavigationPanel { get; }

        /// <summary>
        /// Whether the artwork is presented within the page or in a floating window (<c>Style</c>).
        /// </summary>
        public Artwork3DPresentationStyle Style { get; }

        /// <summary>
        /// The dictionary describing the floating window used with
        /// <see cref="Artwork3DPresentationStyle.Windowed"/> (<c>Window</c>), unparsed, if any.
        /// </summary>
        public DictionaryToken? Window { get; }

        /// <summary>
        /// Whether the page content shows through the transparent areas of the artwork (<c>Transparent</c>).
        /// </summary>
        public bool Transparent { get; }

        internal Artwork3DActivation(
            Artwork3DActivationMode mode,
            Artwork3DInstanceState activationState,
            Artwork3DDeactivationMode deactivationMode,
            Artwork3DInstanceState deactivationState,
            bool showToolbar,
            bool showNavigationPanel,
            Artwork3DPresentationStyle style,
            DictionaryToken? window,
            bool transparent)
        {
            Mode = mode;
            ActivationState = activationState;
            DeactivationMode = deactivationMode;
            DeactivationState = deactivationState;
            ShowToolbar = showToolbar;
            ShowNavigationPanel = showNavigationPanel;
            Style = style;
            Window = window;
            Transparent = transparent;
        }
    }
}
