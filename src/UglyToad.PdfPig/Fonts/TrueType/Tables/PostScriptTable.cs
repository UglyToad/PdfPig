namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System;
    using System.Text;

    /// <summary>
    /// This table contains information for TrueType fonts on PostScript printers.
    /// This includes data for the FontInfo dictionary and the PostScript glyph names.
    /// </summary>
    internal class PostScriptTable : ITable
    {
        public string Tag => TrueTypeHeaderTable.Post;

        public TrueTypeHeaderTable DirectoryTable { get; }

        /// <summary>
        /// Format 1 contains the 258 standard Mac TrueType font file.<br/>
        /// Format 2 is the Microsoft font format.<br/>
        /// Format 2.5 is a space optimised subset of the standard Mac glyph set.<br/>
        /// Format 3 enables a special font type which provides no PostScript information.<br/>
        /// </summary>
        public decimal FormatType { get; }

        /// <summary>
        /// Angle in counter-clockwise degrees from vertical. 0 for upright text, negative for right-leaning text.
        /// </summary>
        public decimal ItalicAngle { get; }

        /// <summary>
        /// Suggested values for the underline position with negative values below the baseline.
        /// </summary>
        public short UnderlinePosition { get; }

        /// <summary>
        /// Suggested values for the underline thickness.
        /// </summary>
        public short UnderlineThickness { get; }

        /// <summary>
        /// 0 if the font is proportionally spaced, non-zero for monospace or other
        /// non-proportional spacing.
        /// </summary>
        public long IsFixedPitch { get; }

        /// <summary>
        /// Minimum memory usage when the TrueType font is downloaded.
        /// </summary>
        public long MinimumMemoryType42 { get; }

        /// <summary>
        /// Maximum memory usage when the TrueType font is downloaded.
        /// </summary>
        public long MaximumMemoryType42 { get; }

        /// <summary>
        /// Minimum memory usage when the TrueType font is downloaded as a Type 1 font.
        /// </summary>
        public long MinimumMemoryType1 { get; }

        /// <summary>
        /// Maximum memory usage when the TrueType font is downloaded as a Type 1 font.
        /// </summary>
        public long MaximumMemoryType1 { get; }

        public string[] GlyphNames { get; }

        public PostScriptTable(TrueTypeHeaderTable directoryTable, decimal formatType, decimal italicAngle, short underlinePosition, short underlineThickness, long isFixedPitch, long minimumMemoryType42, long maximumMemoryType42, long minimumMemoryType1, long maximumMemoryType1, string[] glyphNames)
        {
            DirectoryTable = directoryTable;
            FormatType = formatType;
            ItalicAngle = italicAngle;
            UnderlinePosition = underlinePosition;
            UnderlineThickness = underlineThickness;
            IsFixedPitch = isFixedPitch;
            MinimumMemoryType42 = minimumMemoryType42;
            MaximumMemoryType42 = maximumMemoryType42;
            MinimumMemoryType1 = minimumMemoryType1;
            MaximumMemoryType1 = maximumMemoryType1;
            GlyphNames = glyphNames ?? throw new ArgumentNullException(nameof(glyphNames));
        }

        public static PostScriptTable Load(TrueTypeDataBytes data, TrueTypeHeaderTable table, BasicMaximumProfileTable maximumProfileTable)
        {
            data.Seek(table.Offset);
            var formatType = data.Read32Fixed();
            var italicAngle = data.Read32Fixed();
            var underlinePosition = data.ReadSignedShort();
            var underlineThickness = data.ReadSignedShort();
            var isFixedPitch = data.ReadUnsignedInt();
            var minMemType42 = data.ReadUnsignedInt();
            var maxMemType42 = data.ReadUnsignedInt();
            var mimMemType1 = data.ReadUnsignedInt();
            var maxMemType1 = data.ReadUnsignedInt();

            var glyphNames = GetGlyphNamesByFormat(data, maximumProfileTable, formatType);

            return new PostScriptTable(table, (decimal)formatType, (decimal)italicAngle,
                underlinePosition, underlineThickness, isFixedPitch,
                minMemType42, maxMemType42, mimMemType1,
                maxMemType1, glyphNames);
        }

        private static string[] GetGlyphNamesByFormat(TrueTypeDataBytes data, BasicMaximumProfileTable maximumProfileTable,
            float formatType)
        {
            string[] glyphNames;
            if (Math.Abs(formatType - 1) < float.Epsilon)
            {
                glyphNames = new string[WindowsGlyphList4.NumberOfMacGlyphs];
                Array.Copy(WindowsGlyphList4.MacGlyphNames, glyphNames, WindowsGlyphList4.NumberOfMacGlyphs);
            }
            else if (Math.Abs(formatType - 2) < float.Epsilon)
            {
                glyphNames = GetFormat2GlyphNames(data);
            }
            else if (Math.Abs(formatType - 2.5) < float.Epsilon)
            {
                var glyphNameIndex = new int[maximumProfileTable?.NumberOfGlyphs ?? 0];

                for (var i = 0; i < glyphNameIndex.Length; i++)
                {
                    var offset = data.ReadSignedByte();
                    glyphNameIndex[i] = i + 1 + offset;
                }

                glyphNames = new string[glyphNameIndex.Length];

                for (var i = 0; i < glyphNames.Length; i++)
                {
                    var name = WindowsGlyphList4.MacGlyphNames[glyphNameIndex[i]];

                    if (name != null)
                    {
                        glyphNames[i] = name;
                    }
                }
            }
            else if (Math.Abs(formatType - 3) < float.Epsilon)
            {
                glyphNames = new string[0];
            }
            else
            {
                throw new InvalidOperationException($"Format type {formatType} is not supported for the PostScript table.");
            }

            return glyphNames;
        }

        private static string[] GetFormat2GlyphNames(TrueTypeDataBytes data)
        {
            const int reservedIndexStart = 32768;

            var numberOfGlyphs = data.ReadUnsignedShort();

            var glyphNameIndex = new int[numberOfGlyphs];

            var glyphNames = new string[numberOfGlyphs];

            var maxIndex = int.MinValue;
            for (var i = 0; i < numberOfGlyphs; i++)
            {
                var index = data.ReadUnsignedShort();

                glyphNameIndex[i] = index;

                if (index < reservedIndexStart)
                {
                    maxIndex = Math.Max(maxIndex, index);
                }
            }

            var nameArray = default(string[]);

            if (maxIndex >= WindowsGlyphList4.NumberOfMacGlyphs)
            {
                var namesLength = maxIndex - WindowsGlyphList4.NumberOfMacGlyphs + 1;
                nameArray = new string[namesLength];

                for (var i = 0; i < namesLength; i++)
                {
                    var numberOfCharacters = data.ReadUnsignedByte();
                    nameArray[i] = data.ReadString(numberOfCharacters, Encoding.UTF8);
                }
            }

            for (int i = 0; i < numberOfGlyphs; i++)
            {
                var index = glyphNameIndex[i];
                if (index < WindowsGlyphList4.NumberOfMacGlyphs)
                {
                    glyphNames[i] = WindowsGlyphList4.MacGlyphNames[index];
                }
                else if (index >= WindowsGlyphList4.NumberOfMacGlyphs && index < reservedIndexStart)
                {
                    if (nameArray == null)
                    {
                        throw new InvalidOperationException("The name array was null despite the number of glyphs exceeding the maximum Mac Glyphs.");
                    }

                    glyphNames[i] = nameArray[index - WindowsGlyphList4.NumberOfMacGlyphs];
                }
                else
                {
                    glyphNames[i] = ".undefined";
                }
            }

            return glyphNames;
        }
    }
}
