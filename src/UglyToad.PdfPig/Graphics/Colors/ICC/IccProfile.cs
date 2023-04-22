using System;
using System.Collections.Generic;
using System.Linq;
using IccProfileNet.Parsers;
using IccProfileNet.Tags;

namespace IccProfileNet
{
    /// <summary>
    /// ICC profile.
    /// </summary>
    internal class IccProfile
    {
        /// <summary>
        /// ICC profile header.
        /// </summary>
        public IccProfileHeader Header { get; }

        private readonly Lazy<IccTagTableItem[]> tagTable;
        /// <summary>
        /// The tag table acts as a table of contents for the tags and an index into the tag data element in the profiles.
        /// </summary>
        public IccTagTableItem[] TagTable => tagTable.Value;

        private readonly Lazy<IReadOnlyDictionary<string, IccTagTypeBase>> tags;
        public IReadOnlyDictionary<string, IccTagTypeBase> Tags => tags.Value;

        /// <summary>
        /// TODO
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// ICC profile v4.
        /// </summary>
        public IccProfile(byte[] data)
        {
            Data = data;
            Header = new IccProfileHeader(data);
            tagTable = new Lazy<IccTagTableItem[]>(() => ParseTagTable(data.Skip(128).ToArray()));
            tags = new Lazy<IReadOnlyDictionary<string, IccTagTypeBase>>(() => GetTags());
        }

        public byte[] GetOrComputeProfileId()
        {
            if (Header.IsProfileIdComputed())
            {
                return Header.ProfileId;
            }

            return ComputeProfileId();
        }

        public byte[] ComputeProfileId()
        {
            return IccHelper.ComputeProfileId(Data);
        }

        /// <summary>
        /// Validates the profile against the profile id.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the profile id contained in the header matches the re-computed profile id.
        /// <c>false</c> if they don't match.
        /// <c>null</c> if the profile id is not set in the header.
        /// </returns>
        public bool? ValidateProfile()
        {
            if (Header.IsProfileIdComputed())
            {
                return null;
            }

            return Header.ProfileId.SequenceEqual(ComputeProfileId());
        }

        private static IccTagTableItem[] ParseTagTable(byte[] bytes)
        {
            // Tag count (n)
            // 0 to 3
            uint tagCount = IccHelper.ReadUInt32(bytes
                .Skip(IccTagTableItem.TagCountOffset)
                .Take(IccTagTableItem.TagCountLength).ToArray());

            IccTagTableItem[] tagTableItems = new IccTagTableItem[tagCount];

            for (var i = 0; i < tagCount; ++i)
            {
                int currentOffset = i * (IccTagTableItem.TagSignatureLength +
                                         IccTagTableItem.TagOffsetLength +
                                         IccTagTableItem.TagSizeLength);

                // Tag Signature
                // 4 to 7
                string signature = IccHelper.GetString(bytes,
                    currentOffset + IccTagTableItem.TagSignatureOffset, IccTagTableItem.TagSignatureLength);

                // Offset to beginning of tag data element
                // 8 to 11
                uint offset = IccHelper.ReadUInt32(bytes
                    .Skip(currentOffset + IccTagTableItem.TagOffsetOffset)
                    .Take(IccTagTableItem.TagOffsetLength).ToArray());

                // Size of tag data element
                // 12 to 15
                uint size = IccHelper.ReadUInt32(bytes
                    .Skip(currentOffset + IccTagTableItem.TagSizeOffset)
                    .Take(IccTagTableItem.TagSizeLength).ToArray());

                tagTableItems[i] = new IccTagTableItem(signature, offset, size);
            }

            return tagTableItems;
        }

        private IReadOnlyDictionary<string, IccTagTypeBase> GetTags()
        {
            switch (Header.VersionMajor)
            {
                case 4:
                    {
                        var tags = new Dictionary<string, IccTagTypeBase>();
                        for (int t = 0; t < TagTable.Length; t++)
                        {
                            IccTagTableItem tag = TagTable[t];
                            tags.Add(tag.Signature, IccProfileV4TagParser.Parse(Data, tag));
                        }
                        return tags;
                    }

                case 2:
                    {
                        var tags = new Dictionary<string, IccTagTypeBase>();
                        for (int t = 0; t < TagTable.Length; t++)
                        {
                            IccTagTableItem tag = TagTable[t];
                            tags.Add(tag.Signature, IccProfileV2TagParser.Parse(Data, tag));
                        }
                        return tags;
                    }

                default:
                    throw new NotImplementedException($"ICC Profile v{Header.VersionMajor}.{Header.VersionMinor} is not supported.");
            }
        }

        public bool TryProcessOld(double[] input, out double[] output)
        {
            throw new NotImplementedException($"Unsuported ICC profile v{Header.VersionMajor}");

            //switch (Header.VersionMajor)
            //{
            //    case 2:
            //        return TryProcess2(input, out output);

            //    case 4:
            //        return TryProcess4(input, out output);

            //    default:
            //        throw new NotImplementedException($"Unsuported ICC profile v{Header.VersionMajor}");
            //}
        }

        public bool TryProcess(double[] input, out double[] output)
        {
            try
            {
                switch (Header.ProfileClass)
                {
                    case IccProfileClass.Input:
                        {
                            // 8.3 Input profiles
                            // 8.3.2 N-component LUT-based Input profiles
                            if (Tags.TryGetValue(IccTags.AToB0Tag, out var lutAB0TagI) &&
                                lutAB0TagI is IccLutABType lutAb0I)
                            {
                                //output = lutAB.Process(input, Header);
                                //return true;
                            }

                            // 8.3.3 Three-component matrix-based Input profiles
                            if (Tags.TryGetValue(IccTags.RedMatrixColumnTag, out var rmcTagI) &&
                                Tags.TryGetValue(IccTags.RedTRCTag, out var rTrcTagI))
                            {
                                // Check other taggs
                            }

                            // 8.3.4 Monochrome Input profiles
                            if (Tags.TryGetValue(IccTags.GrayTRCTag, out var kTrcTag) && kTrcTag is IccBaseCurveType kTrc)
                            {
                                double v = kTrc.Process(input.Single());
                                output = new double[] { v, v, v };
                                return true;
                            }
                            break;
                        }

                    case IccProfileClass.Display:
                        {
                            // 8.4 Display profiles
                            // 8.4.2 N-Component LUT-based Display profilesS
                            if (Tags.TryGetValue(IccTags.AToB0Tag, out var lutAB0TagD) && lutAB0TagD is IccLutABType lutAb0D &&
                                Tags.TryGetValue(IccTags.BToA0Tag, out var lutBA0TagD) && lutBA0TagD is IccLutABType lutBa0D)
                            {
                                output = lutBa0D.Process(input, Header);
                                return true;
                            }

                            // 8.4.3 Three-component matrix-based Display profiles
                            // See p197 of Wiley book
                            if (Tags.TryGetValue(IccTags.RedMatrixColumnTag, out var rmcTag) && rmcTag is IccXyzType rmc &&
                                Tags.TryGetValue(IccTags.GreenMatrixColumnTag, out var gmcTag) && gmcTag is IccXyzType gmc &&
                                Tags.TryGetValue(IccTags.BlueMatrixColumnTag, out var bmcTag) && bmcTag is IccXyzType bmc &&

                                Tags.TryGetValue(IccTags.RedTRCTag, out var rTrcTag) && rTrcTag is IccBaseCurveType rTrc &&
                                Tags.TryGetValue(IccTags.GreenTRCTag, out var gTrcTag) && gTrcTag is IccBaseCurveType gTrc &&
                                Tags.TryGetValue(IccTags.BlueTRCTag, out var bTrcTag) && bTrcTag is IccBaseCurveType bTrc)
                            {
                                // Optional
                                // Tags.TryGetValue(IccTags.ChromaticAdaptationTag, out var caTag) && caTag is IccS15Fixed16ArrayType ca

                                double channel1 = input[0];
                                double channel2 = input[1];
                                double channel3 = input[2];

                                double lR = rTrc.Process(channel1);
                                double lG = gTrc.Process(channel2);
                                double lB = bTrc.Process(channel3);

                                double cX = rmc.Xyz.X * lR + gmc.Xyz.X * lG + bmc.Xyz.X * lB;
                                double cY = rmc.Xyz.Y * lR + gmc.Xyz.Y * lG + bmc.Xyz.Y * lB;
                                double cZ = rmc.Xyz.Z * lR + gmc.Xyz.Z * lG + bmc.Xyz.Z * lB;

                                output = new double[] { cX, cY, cZ };
                                return true;
                            }

                            // 8.4.4 Monochrome Display profiles
                            if (Tags.TryGetValue(IccTags.GrayTRCTag, out var kTrcTag) && kTrcTag is IccBaseCurveType kTrc)
                            {
                                double v = kTrc.Process(input.Single());
                                output = new double[] { v, v, v };
                                return true;
                            }
                            break;
                        }

                    case IccProfileClass.Output:
                        {
                            // 8.5.2 N-component LUT-based Output profiles
                            if (Tags.TryGetValue(IccTags.AToB0Tag, out var lutAB0Tag) && lutAB0Tag is IIccClutType lutAb0 &&
                                Tags.TryGetValue(IccTags.AToB1Tag, out var lutAB1Tag) && lutAB1Tag is IIccClutType lutAb1 &&
                                Tags.TryGetValue(IccTags.AToB2Tag, out var lutAB2Tag) && lutAB2Tag is IIccClutType lutAb2 &&


                                Tags.TryGetValue(IccTags.BToA0Tag, out var lutBA0Tag) && lutBA0Tag is IIccClutType lutBa0 &&
                                Tags.TryGetValue(IccTags.BToA1Tag, out var lutBA1Tag) && lutBA1Tag is IIccClutType lutBa1 &&
                                Tags.TryGetValue(IccTags.BToA2Tag, out var lutBA2Tag) && lutBA2Tag is IIccClutType lutBa2 &&

                                Tags.TryGetValue(IccTags.GamutTag, out var gamutTag))
                            {
                                // Optional??
                                // Tags.TryGetValue(IccTags.ColorantTableTag, out var colorantTableTag)
                                switch (Header.RenderingIntent)
                                {
                                    case IccRenderingIntent.Perceptual:
                                        output = lutAb0.Process(input, Header);
                                        return true;

                                    case IccRenderingIntent.MediaRelativeColorimetric:
                                        output = lutAb1.Process(input, Header);
                                        return true;

                                    case IccRenderingIntent.Saturation:
                                        output = lutAb2.Process(input, Header);
                                        return true;
                                }
                            }

                            // 8.5.3 Monochrome Output profiles
                            if (Tags.TryGetValue(IccTags.GrayTRCTag, out var tag))
                            {

                            }
                            break;
                        }

                    case IccProfileClass.DeviceLink:
                        {
                            // TODO
                            break;
                        }

                    case IccProfileClass.ColorSpace:
                        {
                            // 8.7 ColorSpace profile
                            if (Tags.TryGetValue(IccTags.AToB0Tag, out var lutAB0Tag) && lutAB0Tag is IccLutABType lutAb0 &&
                                Tags.TryGetValue(IccTags.BToA0Tag, out var lutBA0Tag) && lutBA0Tag is IccLutABType lutBa0)
                            {

                            }
                            break;
                        }

                    case IccProfileClass.Abstract:
                        {
                            // TODOS
                            break;
                        }

                    case IccProfileClass.NamedColor:
                        {
                            // TODO
                            break;
                        }

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception)
            {
                // Ignore
                output = null;
                return false;
            }

            output = null;
            return false;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ICC Profile v{Header}";
        }
    }
}