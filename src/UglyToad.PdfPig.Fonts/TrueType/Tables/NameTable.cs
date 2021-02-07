namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System;
    using System.Collections.Generic;
    using Names;

    /// <inheritdoc />
    /// <summary>
    /// A name table allows multilingual strings to be associated with the TrueType font.
    /// </summary>
    public class NameTable : ITrueTypeTable
    {
        /// <inheritdoc />
        public string Tag => TrueTypeHeaderTable.Name;

        /// <inheritdoc />
        public TrueTypeHeaderTable DirectoryTable { get; }

        /// <summary>
        /// Font name.
        /// </summary>
        public string FontName { get; }

        /// <summary>
        /// Font family name.
        /// </summary>
        public string FontFamilyName { get; }
        
        /// <summary>
        /// Font sub-family name.
        /// </summary>
        public string FontSubFamilyName { get; }

        /// <summary>
        /// The name records contained in this name table.
        /// </summary>
        public IReadOnlyList<TrueTypeNameRecord> NameRecords { get; }

        /// <summary>
        /// Creaye a new <see cref="NameTable"/>.
        /// </summary>
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
                if (nameRecord is null || nameRecord.NameId != 6)
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
