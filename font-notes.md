# Fonts #

## Types of Font ##

<pre><code>

------	Composite Fonts -------

	Type0 (Composed of glyphs from a CIDFont)

		Children:
		
		CIDFont 	CIDFontType0	(Type 1 font glyph descriptions)
				CIDFontType2	(TrueType font glyph descriptions)
	
------	Simple Fonts Below -------

	Type 1	Type 1 (defines gylphs using type 1 font technology)
		MMType1 (multiple master font - extends type 1 fonts to support many typefaces for a single font)

	Type 3	(defines glyphs with streams of PDF graphics operations)
	
	TrueType (from the TrueType font format)

</code></pre>


## Terminology ##

+ Font dictionary: PDF dictionary with information about the font
+ Font program: Glyph information in specialized font format

## Composite Fonts ##

+ Glyphs are selected from a font-like CIDFont.
+ Has a single CIDFont descendant.
+ Multiple-byte sequences select a single glyph.

Used for multiple-byte character encodings and large numbers of glyphs.

Well suited to Chinese, Japanese and Korean (CJK).

CID stands for character identifier. This is a number used to access glyph descriptions.

The CMap maps between character codes and CID numbers for the glyphs.

A CIDFont file provides the glyph descriptions for a character collection. The glyph descriptions are
identified by CIDs.

CID keyed font combines a CMap with a CIDFont.

The **Encoding** contains the CMap.
The **DescendantFonts** contains the CIDFont to use with the CMap.

### CIDFont ###

A Type0 font descendant (CIDFont) must be either a CIDFontType0 (Adobe Type 1) or CIDFontType2 (TrueType).

For Type 2 CIDFonts (TrueType) the glyphs are identified by a glyph index (GID).

+ If the font program is embedded as a stream the CIDFont dictionary must contain a CIDToGIDMap which maps
from CIDs to Glyph Indexes.

+ If the font program is a predefined external font the CIDFont must not contain a CIDToGIDMap. It
may only use a predefined CMap.

Though a CID may not be used to select the glyph as in the predefined case, it is always used to select glyph
metrics. Every CIDFont must describe CID 0 which is the ```.notdef``` character for missing characters.

### Glyph Metrics in CIDFonts ###

Widths for CIDFonts are defined in the DW and W entries in the CIDFont dictionary.

+ DW provides the default width for glyphs which are not specified individually.
+ W defines widths for individual CIDs.

Vertical writing has other stuff, see the spec.

### CMap ###

The CMap maps from character codes to character selectors (CIDs).

The CMap defines the writing mode horizontal or vertical.

### Type 0 Fonts ###

The **Font dictionary** has the following entries:

+ Type (name): /Font
+ Subtype (name): /Type0
+ BaseFont (name): The PostScript name of the font.
+ Encoding (name/stream R): Name of a predefined CMap or a stream for an embedded CMap.
+ DescendantFonts (array): Single element pointing to the CIDFont.
+ ToUnicode (stream R)?: Stream containing a CMap file to map codes to Unicode.

## Simple Fonts ##

+ Glyphs are selected by single-byte character codes. Index into a 256 entry glyph table.
+ Only supports horizontal writing mode.

## Further Description ##

### Type 1 Fonts ###

The **Font program** is a PostScript program describing glyph shape. See the Adobe Type 1 Font Format specification.

The **Font dictionary** has the following entries:

+ Type (name): /Font
+ Subtype (name): /Type1
+ Name (name?): Font name
+ BaseFont (name): The PostScript name of the font. Equivalent to the FontName value in the **Font program**.
+ FirstChar (int): The first character code in the Widths array.
+ LastChar (int) The last character code in the Widths array.
+ Widths (numeric[] R): An array defining the glyph width in units of 1000 == 1 text space unit.
+ FontDescriptor (Dict<> R): Describes font metrics other than widths.
+ Encoding (name/Dict<> R): Specifies the character encoding if different from default.
+ ToUnicode (stream R): CMap mapping character code to Unicode.