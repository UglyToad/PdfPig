namespace UglyToad.PdfPig.Fonts.TrueType.Parser
{
    using System.Text;
    using Tables;

    internal class Os2TableParser : ITrueTypeTableParser<Os2Table>
    {
        public Os2Table Parse(TrueTypeHeaderTable header, TrueTypeDataBytes data, TableRegister.Builder register)
        {
            data.Seek(header.Offset);

            var version = data.ReadUnsignedShort();

            var xAvgCharWidth = data.ReadSignedShort();
            var weightClass = data.ReadUnsignedShort();
            var widthClass = data.ReadUnsignedShort();
            var typeFlags = data.ReadUnsignedShort();
            var ySubscriptXSize = data.ReadSignedShort();
            var ySubscriptYSize = data.ReadSignedShort();
            var ySubscriptXOffset = data.ReadSignedShort();
            var ySubscriptYOffset = data.ReadSignedShort();
            var ySuperscriptXSize = data.ReadSignedShort();
            var ySuperscriptYSize = data.ReadSignedShort();
            var ySuperscriptXOffset = data.ReadSignedShort();
            var ySuperscriptYOffset = data.ReadSignedShort();
            var yStrikeoutSize = data.ReadSignedShort();
            var yStrikeoutPosition = data.ReadSignedShort();
            var familyClass = data.ReadSignedShort();
            var panose = data.ReadByteArray(10);
            var ulCharRange1 = data.ReadUnsignedInt();
            var ulCharRange2 = data.ReadUnsignedInt();
            var ulCharRange3 = data.ReadUnsignedInt();
            var ulCharRange4 = data.ReadUnsignedInt();
            var vendorIdBytes = data.ReadByteArray(4);
            var selectionFlags = data.ReadUnsignedShort();
            var firstCharacterIndex = data.ReadUnsignedShort();
            var lastCharacterIndex = data.ReadUnsignedShort();
            var unicodeCharRange = new[] { ulCharRange1, ulCharRange2, ulCharRange3, ulCharRange4 };

            var vendorId = Encoding.ASCII.GetString(vendorIdBytes);

            /*
             * Documentation for OS/2 version 0 in Apple’s TrueType Reference Manual stops at the usLastCharIndex field
             * and does not include the last five fields of the table as it was defined by Microsoft.
             * Some legacy TrueType fonts may have been built with a shortened version 0 OS/2 table.
             * Applications should check the table length for a version 0 OS/2 table before reading these fields.
             */
            if (version == 0 && header.Length == 68)
            {
                return new Os2Table(header, version, xAvgCharWidth,
                    weightClass, widthClass, typeFlags, ySubscriptXSize,
                    ySubscriptYSize,
                    ySubscriptXOffset,
                    ySubscriptYOffset,
                    ySuperscriptXSize,
                    ySuperscriptYSize,
                    ySuperscriptXOffset,
                    ySuperscriptYOffset,
                    yStrikeoutSize,
                    yStrikeoutPosition,
                    familyClass,
                    panose,
                    unicodeCharRange,
                    vendorId,
                    selectionFlags,
                    firstCharacterIndex,
                    lastCharacterIndex);
            }

            short sTypoAscender;
            short sTypoDescender;
            short sTypoLineGap;
            ushort usWinAscent;
            ushort usWinDescent;

            try
            {
                sTypoAscender = data.ReadSignedShort();
                sTypoDescender = data.ReadSignedShort();
                sTypoLineGap = data.ReadSignedShort();
                usWinAscent = data.ReadUnsignedShort();
                usWinDescent = data.ReadUnsignedShort();
            }
            catch
            {
                // Font may be invalid. Try falling back to shorter version...
                return new Os2Table(header, version, xAvgCharWidth,
                    weightClass, widthClass, typeFlags, ySubscriptXSize,
                    ySubscriptYSize,
                    ySubscriptXOffset,
                    ySubscriptYOffset,
                    ySuperscriptXSize,
                    ySuperscriptYSize,
                    ySuperscriptXOffset,
                    ySuperscriptYOffset,
                    yStrikeoutSize,
                    yStrikeoutPosition,
                    familyClass,
                    panose,
                    unicodeCharRange,
                    vendorId,
                    selectionFlags,
                    firstCharacterIndex,
                    lastCharacterIndex);
            }

            if (version == 0)
            {
                return new Os2RevisedVersion0Table(header, version, xAvgCharWidth,
                    weightClass, widthClass, typeFlags, ySubscriptXSize,
                    ySubscriptYSize,
                    ySubscriptXOffset,
                    ySubscriptYOffset,
                    ySuperscriptXSize,
                    ySuperscriptYSize,
                    ySuperscriptXOffset,
                    ySuperscriptYOffset,
                    yStrikeoutSize,
                    yStrikeoutPosition,
                    familyClass,
                    panose,
                    unicodeCharRange,
                    vendorId,
                    selectionFlags,
                    firstCharacterIndex,
                    lastCharacterIndex,
                    sTypoAscender,
                    sTypoDescender,
                    sTypoLineGap,
                    usWinAscent,
                    usWinDescent);
            }

            var ulCodePageRange1 = data.ReadUnsignedInt();
            var ulCodePageRange2 = data.ReadUnsignedInt();
            if (version == 1)
            {
                return new Os2Version1Table(header, version, xAvgCharWidth,
                    weightClass, widthClass, typeFlags, ySubscriptXSize,
                    ySubscriptYSize,
                    ySubscriptXOffset,
                    ySubscriptYOffset,
                    ySuperscriptXSize,
                    ySuperscriptYSize,
                    ySuperscriptXOffset,
                    ySuperscriptYOffset,
                    yStrikeoutSize,
                    yStrikeoutPosition,
                    familyClass,
                    panose,
                    unicodeCharRange,
                    vendorId,
                    selectionFlags,
                    firstCharacterIndex,
                    lastCharacterIndex,
                    sTypoAscender,
                    sTypoDescender,
                    sTypoLineGap,
                    usWinAscent,
                    usWinDescent,
                    ulCodePageRange1,
                    ulCodePageRange2);
            }

            var sxHeight = data.ReadSignedShort();
            var sCapHeight = data.ReadSignedShort();
            var usDefaultChar = data.ReadUnsignedShort();
            var usBreakChar = data.ReadUnsignedShort();
            var usMaxContext = data.ReadUnsignedShort();

            if (version < 5)
            {
                return new Os2Version2To4OpenTypeTable(header, version, xAvgCharWidth,
                    weightClass, widthClass, typeFlags, ySubscriptXSize,
                    ySubscriptYSize,
                    ySubscriptXOffset,
                    ySubscriptYOffset,
                    ySuperscriptXSize,
                    ySuperscriptYSize,
                    ySuperscriptXOffset,
                    ySuperscriptYOffset,
                    yStrikeoutSize,
                    yStrikeoutPosition,
                    familyClass,
                    panose,
                    unicodeCharRange,
                    vendorId,
                    selectionFlags,
                    firstCharacterIndex,
                    lastCharacterIndex,
                    sTypoAscender,
                    sTypoDescender,
                    sTypoLineGap,
                    usWinAscent,
                    usWinDescent,
                    ulCodePageRange1,
                    ulCodePageRange2,
                    sxHeight,
                    sCapHeight,
                    usDefaultChar,
                    usBreakChar,
                    usMaxContext);
            }

            var usLowerOpticalPointSize = data.ReadUnsignedShort();
            var usUpperOpticalPointSize = data.ReadUnsignedShort();

            return new Os2Version5OpenTypeTable(header, version, xAvgCharWidth,
                weightClass, widthClass, typeFlags, ySubscriptXSize,
                ySubscriptYSize,
                ySubscriptXOffset,
                ySubscriptYOffset,
                ySuperscriptXSize,
                ySuperscriptYSize,
                ySuperscriptXOffset,
                ySuperscriptYOffset,
                yStrikeoutSize,
                yStrikeoutPosition,
                familyClass,
                panose,
                unicodeCharRange,
                vendorId,
                selectionFlags,
                firstCharacterIndex,
                lastCharacterIndex,
                sTypoAscender,
                sTypoDescender,
                sTypoLineGap,
                usWinAscent,
                usWinDescent,
                ulCodePageRange1,
                ulCodePageRange2,
                sxHeight,
                sCapHeight,
                usDefaultChar,
                usBreakChar,
                usMaxContext,
                usLowerOpticalPointSize,
                usUpperOpticalPointSize);
        }
    }
}