namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System;
    using System.Collections.Generic;
    using Names;

    internal class NameTable : ITrueTypeTable
    {
        public string Tag => TrueTypeHeaderTable.Name;

        public TrueTypeHeaderTable DirectoryTable { get; }

        public string FontName { get; }

        public string FontFamilyName { get; }

        public string FontSubFamilyName { get; }

        public IReadOnlyList<TrueTypeNameRecord> NameRecords { get; }

        public NameTable(TrueTypeHeaderTable directoryTable, 
            string fontName,
            string fontFamilyName, 
            string fontSubFamilyName,
            IReadOnlyList<TrueTypeNameRecord> nameRecords)
        {
            DirectoryTable = directoryTable;
            FontName = fontName;
            FontFamilyName = fontFamilyName;
            FontSubFamilyName = fontSubFamilyName;
            NameRecords = nameRecords ?? throw new ArgumentNullException(nameof(nameRecords));
        }

        /// <summary>
        /// Gets the PostScript name for the font if specified, preferring the Windows platform name if present.
        /// </summary>
        /// <returns>The PostScript name for the font if found or <see langword="null"/>.</returns>
        public string GetPostscriptName()
        {
            string any = null;
            foreach (var nameRecord in NameRecords)
            {
                if (nameRecord.NameId != 6)
                {
                    continue;
                }

                if (nameRecord.PlatformId == TrueTypePlatformIdentifier.Windows)
                {
                    return nameRecord.Value;
                }

                any = nameRecord.Value;
            }

            return any;
        }
    }
}
