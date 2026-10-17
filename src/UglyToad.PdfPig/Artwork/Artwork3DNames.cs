namespace UglyToad.PdfPig.Artwork
{
    using Tokens;

    /// <summary>
    /// Dictionary keys used by 3D artwork (ISO 32000-2, 13.6) and rich media (13.7) that are not shared with
    /// the rest of the library.
    /// </summary>
    internal static class Artwork3DNames
    {
        // 3D annotation (Table 309)
        public static readonly NameToken ThreeDD = NameToken.Create("3DD");
        public static readonly NameToken ThreeDV = NameToken.Create("3DV");
        public static readonly NameToken ThreeDA = NameToken.Create("3DA");
        public static readonly NameToken ThreeDI = NameToken.Create("3DI");
        public static readonly NameToken ThreeDB = NameToken.Create("3DB");
        public static readonly NameToken ThreeDU = NameToken.Create("3DU");

        // 3D stream (Table 311) and animation style (Table 312)
        public static readonly NameToken VA = NameToken.Create("VA");
        public static readonly NameToken DV = NameToken.Create("DV");
        public static readonly NameToken AN = NameToken.Create("AN");
        public static readonly NameToken OnInstantiate = NameToken.Create("OnInstantiate");
        public static readonly NameToken PC = NameToken.Create("PC");
        public static readonly NameToken TM = NameToken.Create("TM");

        // Activation (Table 310)
        public static readonly NameToken AIS = NameToken.Create("AIS");
        public static readonly NameToken DIS = NameToken.Create("DIS");
        public static readonly NameToken TB = NameToken.Create("TB");
        public static readonly NameToken NP = NameToken.Create("NP");
        public static readonly NameToken Style = NameToken.Create("Style");
        public static readonly NameToken Window = NameToken.Create("Window");
        public static readonly NameToken Transparent = NameToken.Create("Transparent");

        // View (Table 315)
        public static readonly NameToken XN = NameToken.Create("XN");
        public static readonly NameToken IN = NameToken.Create("IN");
        public static readonly NameToken MS = NameToken.Create("MS");
        public static readonly NameToken C2W = NameToken.Create("C2W");
        public static readonly NameToken U3DPath = NameToken.Create("U3DPath");
        public static readonly NameToken CO = NameToken.Create("CO");
        public static readonly NameToken BG = NameToken.Create("BG");
        public static readonly NameToken RM = NameToken.Create("RM");
        public static readonly NameToken LS = NameToken.Create("LS");
        public static readonly NameToken SA = NameToken.Create("SA");
        public static readonly NameToken NA = NameToken.Create("NA");
        public static readonly NameToken NR = NameToken.Create("NR");
        public static readonly NameToken MA = NameToken.Create("MA");

        // Projection (Table 316), background (317), render mode (318)
        public static readonly NameToken CS = NameToken.Create("CS");
        public static readonly NameToken FOV = NameToken.Create("FOV");
        public static readonly NameToken PS = NameToken.Create("PS");
        public static readonly NameToken OS = NameToken.Create("OS");
        public static readonly NameToken OB = NameToken.Create("OB");
        public static readonly NameToken EA = NameToken.Create("EA");
        public static readonly NameToken AC = NameToken.Create("AC");
        public static readonly NameToken FC = NameToken.Create("FC");
        public static readonly NameToken CV = NameToken.Create("CV");

        // Cross section (Table 322) and node (Table 323)
        public static readonly NameToken PO = NameToken.Create("PO");
        public static readonly NameToken IV = NameToken.Create("IV");
        public static readonly NameToken IC = NameToken.Create("IC");
        public static readonly NameToken ST = NameToken.Create("ST");
        public static readonly NameToken SC = NameToken.Create("SC");
        public static readonly NameToken Instance = NameToken.Create("Instance");
        public static readonly NameToken Data = NameToken.Create("Data");

        // Rich media (13.7)
        public static readonly NameToken RichMediaContent = NameToken.Create("RichMediaContent");
        public static readonly NameToken RichMediaSettings = NameToken.Create("RichMediaSettings");
        public static readonly NameToken Configurations = NameToken.Create("Configurations");
        public static readonly NameToken Instances = NameToken.Create("Instances");
        public static readonly NameToken Asset = NameToken.Create("Asset");
        public static readonly NameToken Views = NameToken.Create("Views");
        public static readonly NameToken Activation = NameToken.Create("Activation");
        public static readonly NameToken Deactivation = NameToken.Create("Deactivation");
        public static readonly NameToken Condition = NameToken.Create("Condition");
        public static readonly NameToken Presentation = NameToken.Create("Presentation");
        public static readonly NameToken Toolbar = NameToken.Create("Toolbar");
        public static readonly NameToken NavigationPane = NameToken.Create("NavigationPane");
        public static readonly NameToken View = NameToken.Create("View");

        public static readonly NameToken RichMediaAnimation = NameToken.Create("Animation");
        public static readonly NameToken RichMediaPlayCount = NameToken.Create("PlayCount");
        public static readonly NameToken RichMediaSpeed = NameToken.Create("Speed");
    }
}
