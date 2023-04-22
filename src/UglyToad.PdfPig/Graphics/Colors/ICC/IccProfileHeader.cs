using System;
using System.Linq;

namespace IccProfileNet
{
    /// <summary>
    /// ICC profile header.
    /// </summary>
    internal struct IccProfileHeader
    {
        #region ICC Profile header constants
        public const int ProfileSizeOffset = 0;
        public const int ProfileSizeLength = 4;
        public const int CmmOffset = 4;
        public const int CmmLength = 4;
        public const int VersionOffset = 8;
        public const int VersionLength = 4;
        public const int ProfileClassOffset = 12;
        public const int ProfileClassLength = 4;
        public const int ColourSpaceOffset = 16;
        public const int ColourSpaceLength = 4;
        public const int PcsByteOffset = 20;
        public const int PcsByteLength = 4;
        public const int CreatedOffset = 24;
        public const int CreatedLength = 12;
        public const int ProfileSignatureOffset = 36;
        public const int ProfileSignatureLength = 4;
        public const int PrimaryPlatformSignatureOffset = 40;
        public const int PrimaryPlatformSignatureLength = 4;
        public const int ProfileFlagsOffset = 44;
        public const int ProfileFlagsLength = 4;
        public const int DeviceManufacturerOffset = 48;
        public const int DeviceManufacturerLength = 4;
        public const int DeviceModelOffset = 52;
        public const int DeviceModelLength = 4;
        public const int DeviceAttributesOffset = 56;
        public const int DeviceAttributesLength = 8;
        public const int RenderingIntentOffset = 64;
        public const int RenderingIntentLength = 4;
        public const int nCIEXYZOffset = 68;
        public const int nCIEXYZLength = 12;
        public const int ProfileCreatorSignatureOffset = 80;
        public const int ProfileCreatorSignatureLength = 4;
        public const int ProfileIdOffset = 84;
        public const int ProfileIdLength = 16;
        #endregion

        private readonly Lazy<uint> profileSize;
        /// <summary>
        /// Profile size.
        /// </summary>
        public uint ProfileSize => profileSize.Value;

        /// <summary>
        /// Profile major version.
        /// </summary>
        public int VersionMajor { get; }

        /// <summary>
        /// Profile minor version.
        /// </summary>
        public int VersionMinor { get; }

        /// <summary>
        /// Profile bug fix version.
        /// </summary>
        public int VersionBugFix { get; }

        private readonly Lazy<string> cmm;
        /// <summary>
        /// Preferred CMM type.
        /// </summary>
        public string Cmm => cmm.Value;

        private readonly Lazy<IccProfileClass> profileClass;
        /// <summary>
        /// Profile/Device class.
        /// </summary>
        public IccProfileClass ProfileClass => profileClass.Value;

        private readonly Lazy<IccColourSpaceType> colourSpace;
        /// <summary>
        /// Colour space of data (possibly a derived space).
        /// </summary>
        public IccColourSpaceType ColourSpace => colourSpace.Value;

        private readonly Lazy<IccProfileConnectionSpace> pcs;
        /// <summary>
        /// Profile connection space. An abstract color space used to connect the source and destination profiles.
        /// </summary>
        public IccProfileConnectionSpace Pcs => pcs.Value;

        private readonly Lazy<DateTime?> created;
        /// <summary>
        /// Date and time this profile was first created.
        /// </summary>
        public DateTime? Created => created.Value;

        private readonly Lazy<string> profileSignature;
        /// <summary>
        /// profile file signature.
        /// </summary>
        public string ProfileSignature => profileSignature.Value;

        private readonly Lazy<IccPrimaryPlatforms> primaryPlatformSignature;
        /// <summary>
        /// Primary platform signature.
        /// </summary>
        public IccPrimaryPlatforms PrimaryPlatformSignature => primaryPlatformSignature.Value;

        private readonly Lazy<byte[]> profileFlags;
        /// <summary>
        /// Profile flags to indicate various options for the CMM such as distributed
        /// processing and caching options.
        /// </summary>
        public byte[] ProfileFlags => profileFlags.Value;

        private readonly Lazy<string> deviceManufacturer;
        /// <summary>
        /// Device manufacturer of the device for which this profile is created.
        /// </summary>
        public string DeviceManufacturer => deviceManufacturer.Value;

        private readonly Lazy<string> deviceModel;
        /// <summary>
        /// Device model of the device for which this profile is created.
        /// </summary>
        public string DeviceModel => deviceModel.Value;

        private readonly Lazy<byte[]> deviceAttributes;
        /// <summary>
        /// Device attributes unique to the particular device setup such as media type.
        /// </summary>
        public byte[] DeviceAttributes => deviceAttributes.Value;

        private readonly Lazy<IccRenderingIntent> renderingIntent;
        /// <summary>
        /// A particular gamut mapping style or method of converting colors in one gamut to colors in another gamut.
        /// </summary>
        public IccRenderingIntent RenderingIntent => renderingIntent.Value;

        private readonly Lazy<IccXyz> nciexyz;
        /// <summary>
        /// The nCIEXYZ values of the illuminant of the PCS.
        /// </summary>
#pragma warning disable IDE1006 // Naming Styles
        public IccXyz nCIEXYZ => nciexyz.Value;
#pragma warning restore IDE1006 // Naming Styles

        private readonly Lazy<string> profileCreatorSignature;
        /// <summary>
        /// Profile creator signature.
        /// </summary>
        public string ProfileCreatorSignature => profileCreatorSignature.Value;

        private readonly Lazy<byte[]> profileId;
        /// <summary>
        /// Profile ID.
        /// </summary>
        public byte[] ProfileId => profileId.Value;

        public bool IsProfileIdComputed()
        {
            return !ProfileId.All(v => v == 0);
        }

        public IccProfileHeader(byte[] profile)
        {
            // Profile version number
            // 8 to 11
            /*
             * The profile version with which the profile is compliant shall be encoded as binary-coded decimal in the profile
             * version field. The first byte (byte 8) shall identify the major version and byte 9 shall identify the minor version
             * and bug fix version in each 4-bit half of the byte. Bytes 10 and 11 are reserved and shall be set to zero. The
             * major and minor versions are set by the International Color Consortium. The profile version number consistent
             * with this ICC specification is “4.4.0.0” (encoded as 04400000h)
             */
            var profileVersionNumber = profile.Skip(VersionOffset).Take(VersionLength).ToArray();
            VersionMajor = profileVersionNumber[0];
            VersionMinor = (int)((uint)(profileVersionNumber[1] & 0xf0) >> 4);
            VersionBugFix = (int)(uint)(profileVersionNumber[1] & 0x0f);

            profileSize = new Lazy<uint>(() =>
            {
                // Profile size
                // 0 to 3 - UInt32Number
                return IccHelper.ReadUInt32(profile.Skip(ProfileSizeOffset).Take(ProfileSizeLength).ToArray());
            });

            cmm = new Lazy<string>(() =>
            {
                // Preferred CMM type
                // 4 to 7
                return IccHelper.GetString(profile, CmmOffset, CmmLength);
            });

            profileClass = new Lazy<IccProfileClass>(() =>
            {
                // Profile/Device class
                // 12 to 15
                return IccHelper.GetProfileClass(IccHelper.GetString(profile, ProfileClassOffset, ProfileClassLength));
            });

            colourSpace = new Lazy<IccColourSpaceType>(() =>
            {
                // Colour space of data (possibly a derived space)
                // 16 to 19
                return IccHelper.GetColourSpaceType(IccHelper.GetString(profile, ColourSpaceOffset, ColourSpaceLength));
            });

            pcs = new Lazy<IccProfileConnectionSpace>(() =>
            {
                // PCS
                // 20 to 23
                string pcs = IccHelper.GetString(profile, PcsByteOffset, PcsByteLength).Trim();
                switch (pcs)
                {
                    case "Lab":
                        return IccProfileConnectionSpace.PCSLAB;

                    case "XYZ":
                        return IccProfileConnectionSpace.PCSXYZ;

                    default:
                        throw new ArgumentOutOfRangeException($"Invalid PCS value '{pcs}'. Expecting 'Lab' or 'XYZ'.");
                }
            });

            created = new Lazy<DateTime?>(() =>
            {
                // Date and time this profile was first created
                // 24 to 35 - dateTimeNumber
                return IccHelper.ReadDateTime(profile.Skip(CreatedOffset).Take(CreatedLength).ToArray());
            });

            profileSignature = new Lazy<string>(() =>
            {
                // ‘acsp’ (61637370h) profile file signature
                // 36 to 39
                // The profile file signature field shall contain the value “acsp” (61637370h) as a profile file signature.
                return IccHelper.GetString(profile, ProfileSignatureOffset, ProfileSignatureLength);
            });

            primaryPlatformSignature = new Lazy<IccPrimaryPlatforms>(() =>
            {
                // Primary platform signature
                // 40 to 43
                return IccHelper.GetPrimaryPlatforms(IccHelper.GetString(profile, PrimaryPlatformSignatureOffset, PrimaryPlatformSignatureLength));
            });

            profileFlags = new Lazy<byte[]>(() =>
            {
                // Profile flags to indicate various options for the CMM such as distributed
                // processing and caching options
                // 44 to 47
                return profile.Skip(ProfileFlagsOffset).Take(ProfileFlagsLength).ToArray();// TODO
            });

            deviceManufacturer = new Lazy<string>(() =>
            {
                // Device manufacturer of the device for which this profile is created
                // 48 to 51
                return IccHelper.GetString(profile, DeviceManufacturerOffset, DeviceManufacturerLength);
            });

            deviceModel = new Lazy<string>(() =>
            {
                // Device model of the device for which this profile is created
                // 52 to 55
                return IccHelper.GetString(profile, DeviceModelOffset, DeviceModelLength);
            });

            deviceAttributes = new Lazy<byte[]>(() =>
            {
                // Device attributes unique to the particular device setup such as media type
                // 56 to 63
                return profile.Skip(DeviceAttributesOffset).Take(DeviceAttributesLength).ToArray(); // TODO
            });

            renderingIntent = new Lazy<IccRenderingIntent>(() =>
            {
                // Rendering Intent
                // 64 to 67
                return (IccRenderingIntent)IccHelper.ReadUInt32(profile.Skip(RenderingIntentOffset).Take(RenderingIntentLength).ToArray());
            });

            nciexyz = new Lazy<IccXyz>(() =>
            {
                // The nCIEXYZ values of the illuminant of the PCS
                // 68 to 79 - XYZNumber
                // shall be X = 0,964 2, Y = 1,0 and Z = 0,824 9
                // These values are the nCIEXYZ values of CIE illuminant D50
                return IccHelper.ReadXyz(profile.Skip(nCIEXYZOffset).Take(nCIEXYZLength).ToArray());
            });

            profileCreatorSignature = new Lazy<string>(() =>
            {
                // Profile creator signature
                // 80 to 83
                return IccHelper.GetString(profile, ProfileCreatorSignatureOffset, ProfileCreatorSignatureLength);
            });

            profileId = new Lazy<byte[]>(() =>
            {
                // Profile ID
                // 84 to 99
                return profile.Skip(ProfileIdOffset).Take(ProfileIdLength).ToArray();
            });
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{VersionMajor}.{VersionMinor}.{VersionBugFix}";
        }
    }

    internal enum IccProfileConnectionSpace
    {
        PCSXYZ,
        PCSLAB
    }

    /// <summary>
    /// There are three basic classes of device profiles, which are Input, Display and Output. In addition to the three
    /// basic device profile classes, four additional colour processing profiles are defined. These profiles provide a
    /// standard implementation for use by the CMM in general colour processing, or for the convenience of CMMs
    /// which may use these types to store calculated transforms.These four additional profile classes are DeviceLink,
    /// ColorSpace, Abstract and NamedColor.
    /// </summary>
    public enum IccProfileClass
    {
        /// <summary>
        /// Input device profile (‘scnr’).
        /// </summary>
        Input,

        /// <summary>
        /// Display device profile (‘mntr’).
        /// </summary>
        Display,

        /// <summary>
        /// Output device profile (‘prtr’).
        /// </summary>
        Output,

        /// <summary>
        /// DeviceLink profile (‘link’).
        /// </summary>
        DeviceLink,

        /// <summary>
        /// ColorSpace profile (‘spac’).
        /// </summary>
        ColorSpace,

        /// <summary>
        /// Abstract profile (‘abst’).
        /// </summary>
        Abstract,

        /// <summary>
        /// NamedColor profile (‘nmcl’).
        /// </summary>
        NamedColor
    }

    /// <summary>
    /// This field shall contain the signature of the data colour space expected on the A side (device side) of the profile
    /// transforms. The names and signatures of the permitted data colour spaces are shown in Table 19. Signatures
    /// are left justified.
    /// </summary>
    public enum IccColourSpaceType
    {
        /// <summary>
        /// nCIEXYZ or PCSXYZ.
        /// </summary>
        nCIEXYZorPCSXYZ,

        /// <summary>
        /// CIELAB or PCSLAB.
        /// </summary>
        CIELABorPCSLAB,

        /// <summary>
        /// CIELUV.
        /// </summary>
        CIELUV,

        /// <summary>
        /// YCbCr.
        /// </summary>
        YCbCr,

        /// <summary>
        /// CIEYxy.
        /// </summary>
        CIEYxy,

        /// <summary>
        /// RGB.
        /// </summary>
        RGB,

        /// <summary>
        /// Gray.
        /// </summary>
        Gray,

        /// <summary>
        /// HSV.
        /// </summary>
        HSV,

        /// <summary>
        /// HLS.
        /// </summary>
        HLS,

        /// <summary>
        /// CMYK.
        /// </summary>
        CMYK,

        /// <summary>
        /// CMY.
        /// </summary>
        CMY,

        /// <summary>
        /// 2 colour.
        /// </summary>
        Colour2,

        /// <summary>
        /// 3 colour (other than those listed above).
        /// </summary>
        Colour3,

        /// <summary>
        /// 4 colour (other than CMYK).
        /// </summary>
        Colour4,

        /// <summary>
        /// 5 colour.
        /// </summary>
        Colour5,

        /// <summary>
        /// 6 colour.
        /// </summary>
        Colour6,

        /// <summary>
        /// 7 colour.
        /// </summary>
        Colour7,

        /// <summary>
        /// 8 colour.
        /// </summary>
        Colour8,

        /// <summary>
        /// 9 colour.
        /// </summary>
        Colour9,

        /// <summary>
        /// 10 colour.
        /// </summary>
        Colour10,

        /// <summary>
        /// 11 colour.
        /// </summary>
        Colour11,

        /// <summary>
        /// 12 colour.
        /// </summary>
        Colour12,

        /// <summary>
        /// 13 colour.
        /// </summary>
        Colour13,

        /// <summary>
        /// 14 colour.
        /// </summary>
        Colour14,

        /// <summary>
        /// 15 colour.
        /// </summary>
        Colour15,
    }

    /// <summary>
    /// The rendering intent field shall specify the rendering intent which should be used (or, in the case of a DeviceLink
    /// profile, was used) when this profile is (was) combined with another profile. In a sequence of more than two
    /// profiles, it applies to the combination of this profile and the next profile in the sequence and not to the entire
    /// sequence. Typically, the user or application will set the rendering intent dynamically at runtime or embedding
    /// time. Therefore, this flag may not have any meaning until the profile is used in some context, e.g. in a DeviceLink
    /// or an embedded source profile
    /// </summary>
    public enum IccRenderingIntent : uint
    {
        /// <summary>
        /// Perceptual.
        /// </summary>
        Perceptual = 0,

        /// <summary>
        /// Media-relative colorimetric.
        /// </summary>
        MediaRelativeColorimetric = 1,

        /// <summary>
        /// Saturation.
        /// </summary>
        Saturation = 2,

        /// <summary>
        /// ICC-absolute colorimetric.
        /// </summary>
        IccAbsoluteColorimetric = 3
    }

    /// <summary>
    /// Identify the primary platform/operating system framework for which the profile was created.
    /// </summary>
    public enum IccPrimaryPlatforms
    {
        /// <summary>
        /// f there is no primary platform identified, this field shall be set to zero (00000000h)
        /// </summary>
        Unidentified,

        /// <summary>
        /// Apple Computer, Inc.
        /// </summary>
        AppleComputer,

        /// <summary>
        /// Microsoft Corporation.
        /// </summary>
        MicrosoftCorporation,

        /// <summary>
        /// Silicon Graphics, Inc.
        /// </summary>
        SiliconGraphics,

        /// <summary>
        /// Sun Microsystems, Inc.
        /// </summary>
        SunMicrosystems
    }
}
