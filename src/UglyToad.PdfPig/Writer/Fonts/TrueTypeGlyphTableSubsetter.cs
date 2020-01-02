namespace UglyToad.PdfPig.Writer.Fonts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using PdfPig.Fonts.TrueType;
    using PdfPig.Fonts.TrueType.Glyphs;
    using Util;
    using IndexMap = TrueTypeSubsetter.OldToNewGlyphIndex;

    internal static class TrueTypeGlyphTableSubsetter
    {
        public static NewGlyphTable SubsetGlyphTable(TrueTypeFontProgram font, byte[] fontBytes, IndexMap[] mapping)
        {
            var data = new TrueTypeDataBytes(fontBytes);

            var existingGlyphs = GetGlyphRecordsInFont(font, data);

            var glyphsToCopy = new List<GlyphRecord>(mapping.Length);
            var glyphsToCopyOriginalIndex = new List<int>(mapping.Length);

            // Extract the glyphs required for this subset from the original table.
            for (var i = 0; i < mapping.Length; i++)
            {
                var map = mapping[i];
                var record = existingGlyphs[map.OldIndex];

                glyphsToCopy.Add(record);
                glyphsToCopyOriginalIndex.Add(map.OldIndex);
            }

            var glyphLocations = new List<uint>();

            var compositeIndicesToReplace = new List<(uint offset, ushort newIndex)>();

            using (var stream = new MemoryStream())
            {
                for (var i = 0; i < glyphsToCopy.Count; i++)
                {
                    compositeIndicesToReplace.Clear();

                    var newRecord = glyphsToCopy[i];

                    if (newRecord.Type == GlyphType.Composite)
                    {
                        // Any glyphs a composite glyph depends on must also be included.
                        for (var j = 0; j < newRecord.DependencyIndices.Count; j++)
                        {
                            // Get the indices of the dependency glyphs from the original font file.
                            var dependency = newRecord.DependencyIndices[j];

                            // If the dependency has already been included we can skip copying it again.
                            var newDependencyIndex = GetAlreadyCopiedDependencyIndex(dependency, glyphsToCopyOriginalIndex);

                            if (!newDependencyIndex.HasValue)
                            {
                                // Else we need to copy the dependency glyph from the original.
                                var actualDependencyRecord = existingGlyphs[dependency.Index];
                                
                                // We need to add it to the set of glyphs to copy.
                                newDependencyIndex = glyphsToCopy.Count;
                                glyphsToCopy.Add(actualDependencyRecord);
                                glyphsToCopyOriginalIndex.Add((int)dependency.Index);
                            }
                            
                            var withinGlyphDataIndexOffset = dependency.OffsetOfIndexWithinData - newRecord.Offset;

                            compositeIndicesToReplace.Add(((uint)withinGlyphDataIndexOffset, (ushort)newDependencyIndex));
                        }
                    }

                    // Record the glyph location.
                    glyphLocations.Add((uint)stream.Position);

                    if (newRecord.Type == GlyphType.Empty)
                    {
                        // TODO: if this is the last glyph this might be a problem.
                        continue;
                    }

                    data.Seek(newRecord.Offset);

                    var glyphBytes = data.ReadByteArray(newRecord.DataLength);

                    // Update any indices referenced by composite glyphs to match the new index of the dependency.
                    foreach (var toReplace in compositeIndicesToReplace)
                    {
                        glyphBytes[toReplace.offset] = (byte)(toReplace.newIndex >> 8);
                        glyphBytes[toReplace.offset + 1] = (byte)toReplace.newIndex;
                    }

                    // Each glyph description must start at a 4 byte boundary.
                    stream.Write(glyphBytes, 0, glyphBytes.Length);
                }

                var output = stream.ToArray();

                glyphLocations.Add((uint)output.Length);
                var offsets = glyphLocations.ToArray();

                return new NewGlyphTable(output, offsets);
            }
        }

        private static GlyphRecord[] GetGlyphRecordsInFont(TrueTypeFontProgram font, TrueTypeDataBytes data)
        {
            var indexToLocationTable = font.TableRegister.IndexToLocationTable;

            var numGlyphs = indexToLocationTable.GlyphOffsets.Length - 1;

            var glyphDirectory = font.TableRegister.GlyphTable.DirectoryTable;

            data.Seek(glyphDirectory.Offset);

            var glyphRecords = new GlyphRecord[numGlyphs];

            for (var i = 0; i < numGlyphs; i++)
            {
                var glyphOffset = (int)(glyphDirectory.Offset + indexToLocationTable.GlyphOffsets[i]);

                if (indexToLocationTable.GlyphOffsets[i + 1] <= indexToLocationTable.GlyphOffsets[i])
                {
                    glyphRecords[i] = new GlyphRecord(i, glyphOffset);
                    continue;
                }

                data.Seek(glyphOffset);

                if (glyphOffset >= glyphDirectory.Offset + glyphDirectory.Length)
                {
                    throw new InvalidOperationException($"Failed to read expected number of glyphs {numGlyphs}, only got to index {i} before reaching end of input.");
                }

                var numberOfContours = data.ReadSignedShort();
                var type = numberOfContours >= 0 ? GlyphType.Simple : GlyphType.Composite;

                // Read bounds.
                data.ReadSignedShort();
                data.ReadSignedShort();
                data.ReadSignedShort();
                data.ReadSignedShort();

                if (type == GlyphType.Simple)
                {
                    ReadSimpleGlyph(data, numberOfContours);
                    glyphRecords[i] = new GlyphRecord(i, glyphOffset, type, (int)(data.Position - glyphOffset));
                }
                else
                {
                    var glyphIndices = ReadCompositeGlyph(data);

                    glyphRecords[i] = new GlyphRecord(i, glyphOffset, type, (int)(data.Position - glyphOffset), glyphIndices);
                }
            }

            return glyphRecords;
        }

        private static int? GetAlreadyCopiedDependencyIndex(CompositeGlyphIndexReference dependency, IReadOnlyList<int> copiedGlyphOriginalIndices)
        {
            for (var i = 0; i < copiedGlyphOriginalIndices.Count; i++)
            {
                var originalIndexAtK = copiedGlyphOriginalIndices[i];

                if (originalIndexAtK == dependency.Index)
                {
                    return i;
                }
            }

            return null;
        }

        private static void ReadSimpleGlyph(TrueTypeDataBytes data, int numberOfContours)
        {
            bool HasFlag(SimpleGlyphFlags flags, SimpleGlyphFlags value)
            {
                return (flags & value) != 0;
            }

            if (numberOfContours == 0)
            {
                return;
            }

            var endPointsOfContours = new ushort[numberOfContours];
            for (var i = 0; i < numberOfContours; i++)
            {
                endPointsOfContours[i] = data.ReadUnsignedShort();
            }

            var instructionLength = data.ReadUnsignedShort();

            var instructions = new byte[instructionLength];

            for (var i = 0; i < instructionLength; i++)
            {
                instructions[i] = data.ReadByte();
            }

            var lastPointIndex = endPointsOfContours[numberOfContours - 1];

            var pointCount = lastPointIndex + 1;

            var perPointFlags = new SimpleGlyphFlags[pointCount];
            for (var i = 0; i < pointCount; i++)
            {
                var flags = (SimpleGlyphFlags)data.ReadByte();

                perPointFlags[i] = flags;
                if (!HasFlag(flags, SimpleGlyphFlags.Repeat))
                {
                    continue;
                }

                var numberOfRepeats = data.ReadByte();
                for (var r = 0; r < numberOfRepeats; r++)
                {
                    i++;
                    perPointFlags[i] = flags;
                }
            }

            var xCoordinates = ReadCoordinates(perPointFlags, data, SimpleGlyphFlags.XSingleByte, SimpleGlyphFlags.ThisXIsTheSame);

            var yCoordinates = ReadCoordinates(perPointFlags, data, SimpleGlyphFlags.YSingleByte, SimpleGlyphFlags.ThisYIsTheSame);
        }

        private static short[] ReadCoordinates(SimpleGlyphFlags[] flags, TrueTypeDataBytes data,
            SimpleGlyphFlags isSingleByte, SimpleGlyphFlags isTheSameAsPrevious)
        {
            bool HasFlag(SimpleGlyphFlags set, SimpleGlyphFlags f)
            {
                return (set & f) != 0;
            }

            var coordinates = new short[flags.Length];
            var value = 0;
            for (var i = 0; i < flags.Length; i++)
            {
                var flag = flags[i];
                if (HasFlag(flag, isSingleByte))
                {
                    var b = data.ReadByte();

                    // If SingleByte is set, the IsTheSame flag describes the sign of the value, 
                    // with a value of 1 equalling positive and a zero value negative.
                    if (HasFlag(flag, isTheSameAsPrevious))
                    {
                        value += b;
                    }
                    else
                    {
                        value -= b;
                    }
                }
                else
                {
                    short delta;

                    // If this flag is set, then the current coordinate is the same as the previous coordinate.
                    if (HasFlag(flag, isTheSameAsPrevious))
                    {
                        delta = 0;
                    }
                    else
                    {
                        // If the IsTheSame flag is not set, the current coordinate is a signed 16-bit delta vector.
                        delta = data.ReadSignedShort();
                    }

                    value += delta;
                }

                coordinates[i] = (short)value;
            }

            return coordinates;
        }

        private static IReadOnlyList<CompositeGlyphIndexReference> ReadCompositeGlyph(TrueTypeDataBytes data)
        {
            bool HasFlag(CompositeGlyphFlags actual, CompositeGlyphFlags value)
            {
                return (actual & value) != 0;
            }

            var glyphIndices = new List<CompositeGlyphIndexReference>();
            CompositeGlyphFlags flags;

            do
            {
                flags = (CompositeGlyphFlags)data.ReadUnsignedShort();
                var indexOffset = data.Position;
                var glyphIndex = data.ReadUnsignedShort();
                glyphIndices.Add(new CompositeGlyphIndexReference(glyphIndex, (uint)indexOffset));

                if (HasFlag(flags, CompositeGlyphFlags.Args1And2AreWords))
                {
                    data.ReadSignedShort();
                    data.ReadSignedShort();
                }
                else
                {
                    data.ReadByte();
                    data.ReadByte();
                }

                if (HasFlag(flags, CompositeGlyphFlags.WeHaveAScale))
                {
                    data.ReadSignedShort();
                }
                else if (HasFlag(flags, CompositeGlyphFlags.WeHaveAnXAndYScale))
                {
                    data.ReadSignedShort();
                    data.ReadSignedShort();
                }
                else if (HasFlag(flags, CompositeGlyphFlags.WeHaveATwoByTwo))
                {
                    data.ReadSignedShort();
                    data.ReadSignedShort();
                    data.ReadSignedShort();
                    data.ReadSignedShort();
                }
            } while (HasFlag(flags, CompositeGlyphFlags.MoreComponents));

            return glyphIndices;
        }

        private class GlyphRecord
        {
            public int Index { get; }

            public int Offset { get; }

            public GlyphType Type { get; }

            public int DataLength { get; }

            /// <summary>
            /// Indices of any glyphs this glyph depends on, if it's a composite glyph.
            /// </summary>
            public IReadOnlyList<CompositeGlyphIndexReference> DependencyIndices { get; }

            public GlyphRecord(int index, int offset, GlyphType type, int dataLength,
                IReadOnlyList<CompositeGlyphIndexReference> dependentIndices = null)
            {
                Index = index;
                Offset = offset;
                Type = type;
                DataLength = dataLength;
                DependencyIndices = dependentIndices ?? EmptyArray<CompositeGlyphIndexReference>.Instance;
            }

            public GlyphRecord(int index, int offset)
            {
                Index = index;
                Offset = offset;
                Type = GlyphType.Empty;
                DataLength = 0;
                DependencyIndices = EmptyArray<CompositeGlyphIndexReference>.Instance;
            }
        }

        private enum GlyphType
        {
            Empty,
            Simple,
            Composite
        }

        private struct CompositeGlyphIndexReference
        {
            /// <summary>
            /// The index of the glyph reference by this composite glyph.
            /// </summary>
            public uint Index { get; }

            /// <summary>
            /// The offset of the index value in the data which this composite glyph was read from.
            /// </summary>
            public uint OffsetOfIndexWithinData { get; }

            public CompositeGlyphIndexReference(uint index, uint offsetOfIndexWithinData)
            {
                Index = index;
                OffsetOfIndexWithinData = offsetOfIndexWithinData;
            }
        }

        public class NewGlyphTable
        {
            public byte[] Bytes { get; }

            public uint[] GlyphOffsets { get; }

            public NewGlyphTable(byte[] bytes, uint[] glyphOffsets)
            {
                Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
                GlyphOffsets = glyphOffsets ?? throw new ArgumentNullException(nameof(glyphOffsets));
            }
        }
    }
}
