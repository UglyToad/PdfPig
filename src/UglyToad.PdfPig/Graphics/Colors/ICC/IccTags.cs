namespace IccProfileNet
{
    internal static class IccTags
    {
        // v4.4 and v2.4 tags
        public const string DeviceModelDescTag = "dmdd";
        public const string DeviceMfgDescTag = "dmnd";
        public const string GamutTag = "gamt";
        public const string GreenTRCTag = "gTRC";
        public const string GrayTRCTag = "kTRC";
        public const string LuminanceTag = "lumi";
        public const string MeasurementTag = "meas";
        public const string NamedColor2Tag = "ncl2";
        public const string Preview0Tag = "pre0";
        public const string Preview1Tag = "pre1";
        public const string Preview2Tag = "pre2";
        public const string ProfileSequenceDescTag = "pseq";
        public const string OutputResponseTag = "resp";
        public const string RedTRCTag = "rTRC";
        public const string CharTargetTag = "targ";
        public const string TechnologyTag = "tech";
        public const string ViewingConditionsTag = "view";
        public const string ViewingCondDescTag = "vued";
        public const string MediaWhitePointTag = "wtpt";
        public const string AToB0Tag = "A2B0";
        public const string AToB1Tag = "A2B1";
        public const string AToB2Tag = "A2B2";
        public const string BToA0Tag = "B2A0";
        public const string BToA1Tag = "B2A1";
        public const string BToA2Tag = "B2A2";
        public const string BlueTRCTag = "bTRC";
        public const string CalibrationDateTimeTag = "calt";
        public const string ChromaticAdaptationTag = "chad";
        public const string ChromaticityTag = "chrm";
        public const string CopyrightTag = "cprt";
        public const string ProfileDescriptionTag = "desc";

        // v4.4 and v2.4 tags with different names
        // v4.4 names
        public const string BlueMatrixColumnTag = "bXYZ";
        public const string GreenMatrixColumnTag = "gXYZ";
        public const string RedMatrixColumnTag = "rXYZ";

        // v2.4 names
        public const string BlueColorantTag = "bXYZ";
        public const string GreenColorantTag = "gXYZ";
        public const string RedColorantTag = "rXYZ";

        // v4.4 only tags
        public const string MetadataTag = "meta";
        public const string ProfileSequenceIdentifierTag = "psid";
        public const string PerceptualRenderingIntentGamutTag = "rig0";
        public const string SaturationRenderingIntentGamutTag = "rig2";
        public const string BToD0Tag = "B2D0";
        public const string BToD1Tag = "B2D1";
        public const string BToD2Tag = "B2D2";
        public const string BToD3Tag = "B2D3";
        public const string CicpTag = "cicp";
        public const string ColorimetricIntentImageStateTag = "ciis";
        public const string ColorantTableOutTag = "clot";
        public const string ColorantOrderTag = "clro";
        public const string ColorantTableTag = "clrt";
        public const string DToB0Tag = "D2B0";
        public const string DToB1Tag = "D2B1";
        public const string DToB2Tag = "D2B2";
        public const string DToB3Tag = "D2B3";

        // v2.4 only tags
        public const string NamedColorTag = "ncol";
        public const string Ps2RenderingIntentTag = "ps2i";
        public const string Ps2CSATag = "ps2s";
        public const string Ps2CRD0Tag = "psd0";
        public const string Ps2CRD1Tag = "psd1";
        public const string Ps2CRD2Tag = "psd2";
        public const string Ps2CRD3Tag = "psd3";
        public const string ScreeningDescTag = "scrd";
        public const string ScreeningTag = "scrn";
        public const string UcrbgTag = "bfd ";
        public const string MediaBlackPointTag = "bkpt";
        public const string CrdInfoTag = "crdi";
        public const string DeviceSettingsTag = "devs";
    }
}
