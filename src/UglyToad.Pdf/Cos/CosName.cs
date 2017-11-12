using System;
using System.Collections.Generic;
using System.Text;

namespace UglyToad.Pdf.Cos
{
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using Core;
    using Util;
    using Util.JetBrains.Annotations;

    /**
     * A PDF Name object.
     *
     * @author Ben Litchfield
     */
    public class CosName : CosBase, IComparable<CosName>, ICosStreamWriter
    {
        // using ConcurrentHashMap because this can be accessed by multiple threads
        private static readonly ConcurrentDictionary<string, CosName> NameMap = new ConcurrentDictionary<string, CosName>();

        // all common CosName values are stored in this HashMap
        // they are already defined as static constants and don't need to be synchronized
        private static readonly Dictionary<string, CosName> CommonNameMap = new Dictionary<string, CosName>(768);

        //
        // IMPORTANT: this list is *alphabetized* and does not need any JavaDoc
        //

        // A
        public static readonly CosName A = new CosName("A");
        public static readonly CosName AA = new CosName("AA");
        public static readonly CosName ACRO_FORM = new CosName("AcroForm");
        public static readonly CosName ACTUAL_TEXT = new CosName("ActualText");
        public static readonly CosName ADBE_PKCS7_DETACHED = new CosName("adbe.pkcs7.detached");
        public static readonly CosName ADBE_PKCS7_SHA1 = new CosName("adbe.pkcs7.sha1");
        public static readonly CosName ADBE_X509_RSA_SHA1 = new CosName("adbe.x509.rsa_sha1");
        public static readonly CosName ADOBE_PPKLITE = new CosName("Adobe.PPKLite");
        public static readonly CosName AESV2 = new CosName("AESV2");
        public static readonly CosName AESV3 = new CosName("AESV3");
        public static readonly CosName AFTER = new CosName("After");
        public static readonly CosName AIS = new CosName("AIS");
        public static readonly CosName ALT = new CosName("Alt");
        public static readonly CosName ALPHA = new CosName("Alpha");
        public static readonly CosName ALTERNATE = new CosName("Alternate");
        public static readonly CosName ANNOT = new CosName("Annot");
        public static readonly CosName ANNOTS = new CosName("Annots");
        public static readonly CosName ANTI_ALIAS = new CosName("AntiAlias");
        public static readonly CosName AP = new CosName("AP");
        public static readonly CosName AP_REF = new CosName("APRef");
        public static readonly CosName APP = new CosName("App");
        public static readonly CosName ART_BOX = new CosName("ArtBox");
        public static readonly CosName ARTIFACT = new CosName("Artifact");
        public static readonly CosName AS = new CosName("AS");
        public static readonly CosName ASCENT = new CosName("Ascent");
        public static readonly CosName ASCII_HEX_DECODE = new CosName("ASCIIHexDecode");
        public static readonly CosName ASCII_HEX_DECODE_ABBREVIATION = new CosName("AHx");
        public static readonly CosName ASCII85_DECODE = new CosName("ASCII85Decode");
        public static readonly CosName ASCII85_DECODE_ABBREVIATION = new CosName("A85");
        public static readonly CosName ATTACHED = new CosName("Attached");
        public static readonly CosName AUTHOR = new CosName("Author");
        public static readonly CosName AVG_WIDTH = new CosName("AvgWidth");
        // B
        public static readonly CosName B = new CosName("B");
        public static readonly CosName BACKGROUND = new CosName("Background");
        public static readonly CosName BASE_ENCODING = new CosName("BaseEncoding");
        public static readonly CosName BASE_FONT = new CosName("BaseFont");
        public static readonly CosName BASE_STATE = new CosName("BaseState");
        public static readonly CosName BBOX = new CosName("BBox");
        public static readonly CosName BC = new CosName("BC");
        public static readonly CosName BE = new CosName("BE");
        public static readonly CosName BEFORE = new CosName("Before");
        public static readonly CosName BG = new CosName("BG");
        public static readonly CosName BITS_PER_COMPONENT = new CosName("BitsPerComponent");
        public static readonly CosName BITS_PER_COORDINATE = new CosName("BitsPerCoordinate");
        public static readonly CosName BITS_PER_FLAG = new CosName("BitsPerFlag");
        public static readonly CosName BITS_PER_SAMPLE = new CosName("BitsPerSample");
        public static readonly CosName BLACK_IS_1 = new CosName("BlackIs1");
        public static readonly CosName BLACK_POINT = new CosName("BlackPoint");
        public static readonly CosName BLEED_BOX = new CosName("BleedBox");
        public static readonly CosName BM = new CosName("BM");
        public static readonly CosName BORDER = new CosName("Border");
        public static readonly CosName BOUNDS = new CosName("Bounds");
        public static readonly CosName BPC = new CosName("BPC");
        public static readonly CosName BS = new CosName("BS");
        //** Acro form field type for button fields.
        public static readonly CosName BTN = new CosName("Btn");
        public static readonly CosName BYTERANGE = new CosName("ByteRange");
        // C
        public static readonly CosName C = new CosName("C");
        public static readonly CosName C0 = new CosName("C0");
        public static readonly CosName C1 = new CosName("C1");
        public static readonly CosName CA = new CosName("CA");
        public static readonly CosName CA_NS = new CosName("ca");
        public static readonly CosName CALGRAY = new CosName("CalGray");
        public static readonly CosName CALRGB = new CosName("CalRGB");
        public static readonly CosName CAP = new CosName("Cap");
        public static readonly CosName CAP_HEIGHT = new CosName("CapHeight");
        public static readonly CosName CATALOG = new CosName("Catalog");
        public static readonly CosName CCITTFAX_DECODE = new CosName("CCITTFaxDecode");
        public static readonly CosName CCITTFAX_DECODE_ABBREVIATION = new CosName("CCF");
        public static readonly CosName CENTER_WINDOW = new CosName("CenterWindow");
        public static readonly CosName CF = new CosName("CF");
        public static readonly CosName CFM = new CosName("CFM");
        //** Acro form field type for choice fields.
        public static readonly CosName CH = new CosName("Ch");
        public static readonly CosName CHAR_PROCS = new CosName("CharProcs");
        public static readonly CosName CHAR_SET = new CosName("CharSet");
        public static readonly CosName CICI_SIGNIT = new CosName("CICI.SignIt");
        public static readonly CosName CID_FONT_TYPE0 = new CosName("CIDFontType0");
        public static readonly CosName CID_FONT_TYPE2 = new CosName("CIDFontType2");
        public static readonly CosName CID_TO_GID_MAP = new CosName("CIDToGIDMap");
        public static readonly CosName CID_SET = new CosName("CIDSet");
        public static readonly CosName CIDSYSTEMINFO = new CosName("CIDSystemInfo");
        public static readonly CosName CL = new CosName("CL");
        public static readonly CosName CLR_F = new CosName("ClrF");
        public static readonly CosName CLR_FF = new CosName("ClrFf");
        public static readonly CosName CMAP = new CosName("CMap");
        public static readonly CosName CMAPNAME = new CosName("CMapName");
        public static readonly CosName CMYK = new CosName("CMYK");
        public static readonly CosName CO = new CosName("CO");
        public static readonly CosName COLOR_BURN = new CosName("ColorBurn");
        public static readonly CosName COLOR_DODGE = new CosName("ColorDodge");
        public static readonly CosName COLORANTS = new CosName("Colorants");
        public static readonly CosName COLORS = new CosName("Colors");
        public static readonly CosName COLORSPACE = new CosName("ColorSpace");
        public static readonly CosName COLUMNS = new CosName("Columns");
        public static readonly CosName COMPATIBLE = new CosName("Compatible");
        public static readonly CosName COMPONENTS = new CosName("Components");
        public static readonly CosName CONTACT_INFO = new CosName("ContactInfo");
        public static readonly CosName CONTENTS = new CosName("Contents");
        public static readonly CosName COORDS = new CosName("Coords");
        public static readonly CosName COUNT = new CosName("Count");
        public static readonly CosName CP = new CosName("CP");
        public static readonly CosName CREATION_DATE = new CosName("CreationDate");
        public static readonly CosName CREATOR = new CosName("Creator");
        public static readonly CosName CROP_BOX = new CosName("CropBox");
        public static readonly CosName CRYPT = new CosName("Crypt");
        public static readonly CosName CS = new CosName("CS");
        // D
        public static readonly CosName D = new CosName("D");
        public static readonly CosName DA = new CosName("DA");
        public static readonly CosName DARKEN = new CosName("Darken");
        public static readonly CosName DATE = new CosName("Date");
        public static readonly CosName DCT_DECODE = new CosName("DCTDecode");
        public static readonly CosName DCT_DECODE_ABBREVIATION = new CosName("DCT");
        public static readonly CosName DECODE = new CosName("Decode");
        public static readonly CosName DECODE_PARMS = new CosName("DecodeParms");
        public static readonly CosName DEFAULT = new CosName("default");
        public static readonly CosName DEFAULT_CMYK = new CosName("DefaultCMYK");
        public static readonly CosName DEFAULT_GRAY = new CosName("DefaultGray");
        public static readonly CosName DEFAULT_RGB = new CosName("DefaultRGB");
        public static readonly CosName DESC = new CosName("Desc");
        public static readonly CosName DESCENDANT_FONTS = new CosName("DescendantFonts");
        public static readonly CosName DESCENT = new CosName("Descent");
        public static readonly CosName DEST = new CosName("Dest");
        public static readonly CosName DEST_OUTPUT_PROFILE = new CosName("DestOutputProfile");
        public static readonly CosName DESTS = new CosName("Dests");
        public static readonly CosName DEVICECMYK = new CosName("DeviceCMYK");
        public static readonly CosName DEVICEGRAY = new CosName("DeviceGray");
        public static readonly CosName DEVICEN = new CosName("DeviceN");
        public static readonly CosName DEVICERGB = new CosName("DeviceRGB");
        public static readonly CosName DI = new CosName("Di");
        public static readonly CosName DIFFERENCE = new CosName("Difference");
        public static readonly CosName DIFFERENCES = new CosName("Differences");
        public static readonly CosName DIGEST_METHOD = new CosName("DigestMethod");
        public static readonly CosName DIGEST_RIPEMD160 = new CosName("RIPEMD160");
        public static readonly CosName DIGEST_SHA1 = new CosName("SHA1");
        public static readonly CosName DIGEST_SHA256 = new CosName("SHA256");
        public static readonly CosName DIGEST_SHA384 = new CosName("SHA384");
        public static readonly CosName DIGEST_SHA512 = new CosName("SHA512");
        public static readonly CosName DIRECTION = new CosName("Direction");
        public static readonly CosName DISPLAY_DOC_TITLE = new CosName("DisplayDocTitle");
        public static readonly CosName DL = new CosName("DL");
        public static readonly CosName DM = new CosName("Dm");
        public static readonly CosName DOC = new CosName("Doc");
        public static readonly CosName DOC_CHECKSUM = new CosName("DocChecksum");
        public static readonly CosName DOC_TIME_STAMP = new CosName("DocTimeStamp");
        public static readonly CosName DOCMDP = new CosName("DocMDP");
        public static readonly CosName DOMAIN = new CosName("Domain");
        public static readonly CosName DOS = new CosName("DOS");
        public static readonly CosName DP = new CosName("DP");
        public static readonly CosName DR = new CosName("DR");
        public static readonly CosName DS = new CosName("DS");
        public static readonly CosName DUPLEX = new CosName("Duplex");
        public static readonly CosName DUR = new CosName("Dur");
        public static readonly CosName DV = new CosName("DV");
        public static readonly CosName DW = new CosName("DW");
        public static readonly CosName DW2 = new CosName("DW2");
        // E
        public static readonly CosName E = new CosName("E");
        public static readonly CosName EARLY_CHANGE = new CosName("EarlyChange");
        public static readonly CosName EF = new CosName("EF");
        public static readonly CosName EMBEDDED_FDFS = new CosName("EmbeddedFDFs");
        public static readonly CosName EMBEDDED_FILES = new CosName("EmbeddedFiles");
        public static readonly CosName EMPTY = new CosName("");
        public static readonly CosName ENCODE = new CosName("Encode");
        public static readonly CosName ENCODED_BYTE_ALIGN = new CosName("EncodedByteAlign");
        public static readonly CosName ENCODING = new CosName("Encoding");
        public static readonly CosName ENCODING_90MS_RKSJ_H = new CosName("90ms-RKSJ-H");
        public static readonly CosName ENCODING_90MS_RKSJ_V = new CosName("90ms-RKSJ-V");
        public static readonly CosName ENCODING_ETEN_B5_H = new CosName("ETen-B5-H");
        public static readonly CosName ENCODING_ETEN_B5_V = new CosName("ETen-B5-V");
        public static readonly CosName ENCRYPT = new CosName("Encrypt");
        public static readonly CosName ENCRYPT_META_DATA = new CosName("EncryptMetadata");
        public static readonly CosName END_OF_LINE = new CosName("EndOfLine");
        public static readonly CosName ENTRUST_PPKEF = new CosName("Entrust.PPKEF");
        public static readonly CosName EXCLUSION = new CosName("Exclusion");
        public static readonly CosName EXT_G_STATE = new CosName("ExtGState");
        public static readonly CosName EXTEND = new CosName("Extend");
        public static readonly CosName EXTENDS = new CosName("Extends");
        // F
        public static readonly CosName F = new CosName("F");
        public static readonly CosName F_DECODE_PARMS = new CosName("FDecodeParms");
        public static readonly CosName F_FILTER = new CosName("FFilter");
        public static readonly CosName FB = new CosName("FB");
        public static readonly CosName FDF = new CosName("FDF");
        public static readonly CosName FF = new CosName("Ff");
        public static readonly CosName FIELDS = new CosName("Fields");
        public static readonly CosName FILESPEC = new CosName("Filespec");
        public static readonly CosName FILTER = new CosName("Filter");
        public static readonly CosName FIRST = new CosName("First");
        public static readonly CosName FIRST_CHAR = new CosName("FirstChar");
        public static readonly CosName FIT_WINDOW = new CosName("FitWindow");
        public static readonly CosName FL = new CosName("FL");
        public static readonly CosName FLAGS = new CosName("Flags");
        public static readonly CosName FLATE_DECODE = new CosName("FlateDecode");
        public static readonly CosName FLATE_DECODE_ABBREVIATION = new CosName("Fl");
        public static readonly CosName FONT = new CosName("Font");
        public static readonly CosName FONT_BBOX = new CosName("FontBBox");
        public static readonly CosName FONT_DESC = new CosName("FontDescriptor");
        public static readonly CosName FONT_FAMILY = new CosName("FontFamily");
        public static readonly CosName FONT_FILE = new CosName("FontFile");
        public static readonly CosName FONT_FILE2 = new CosName("FontFile2");
        public static readonly CosName FONT_FILE3 = new CosName("FontFile3");
        public static readonly CosName FONT_MATRIX = new CosName("FontMatrix");
        public static readonly CosName FONT_NAME = new CosName("FontName");
        public static readonly CosName FONT_STRETCH = new CosName("FontStretch");
        public static readonly CosName FONT_WEIGHT = new CosName("FontWeight");
        public static readonly CosName FORM = new CosName("Form");
        public static readonly CosName FORMTYPE = new CosName("FormType");
        public static readonly CosName FRM = new CosName("FRM");
        public static readonly CosName FT = new CosName("FT");
        public static readonly CosName FUNCTION = new CosName("Function");
        public static readonly CosName FUNCTION_TYPE = new CosName("FunctionType");
        public static readonly CosName FUNCTIONS = new CosName("Functions");
        // G
        public static readonly CosName G = new CosName("G");
        public static readonly CosName GAMMA = new CosName("Gamma");
        public static readonly CosName GROUP = new CosName("Group");
        public static readonly CosName GTS_PDFA1 = new CosName("GTS_PDFA1");
        // H
        public static readonly CosName H = new CosName("H");
        public static readonly CosName HARD_LIGHT = new CosName("HardLight");
        public static readonly CosName HEIGHT = new CosName("Height");
        public static readonly CosName HIDE_MENUBAR = new CosName("HideMenubar");
        public static readonly CosName HIDE_TOOLBAR = new CosName("HideToolbar");
        public static readonly CosName HIDE_WINDOWUI = new CosName("HideWindowUI");
        // I
        public static readonly CosName I = new CosName("I");
        public static readonly CosName IC = new CosName("IC");
        public static readonly CosName ICCBASED = new CosName("ICCBased");
        public static readonly CosName ID = new CosName("ID");
        public static readonly CosName ID_TREE = new CosName("IDTree");
        public static readonly CosName IDENTITY = new CosName("Identity");
        public static readonly CosName IDENTITY_H = new CosName("Identity-H");
        public static readonly CosName IDENTITY_V = new CosName("Identity-V");
        public static readonly CosName IF = new CosName("IF");
        public static readonly CosName IM = new CosName("IM");
        public static readonly CosName IMAGE = new CosName("Image");
        public static readonly CosName IMAGE_MASK = new CosName("ImageMask");
        public static readonly CosName INDEX = new CosName("Index");
        public static readonly CosName INDEXED = new CosName("Indexed");
        public static readonly CosName INFO = new CosName("Info");
        public static readonly CosName INKLIST = new CosName("InkList");
        public static readonly CosName INTERPOLATE = new CosName("Interpolate");
        public static readonly CosName IT = new CosName("IT");
        public static readonly CosName ITALIC_ANGLE = new CosName("ItalicAngle");
        // J
        public static readonly CosName JAVA_SCRIPT = new CosName("JavaScript");
        public static readonly CosName JBIG2_DECODE = new CosName("JBIG2Decode");
        public static readonly CosName JBIG2_GLOBALS = new CosName("JBIG2Globals");
        public static readonly CosName JPX_DECODE = new CosName("JPXDecode");
        public static readonly CosName JS = new CosName("JS");
        // K
        public static readonly CosName K = new CosName("K");
        public static readonly CosName KEYWORDS = new CosName("Keywords");
        public static readonly CosName KIDS = new CosName("Kids");
        // L
        public static readonly CosName L = new CosName("L");
        public static readonly CosName LAB = new CosName("Lab");
        public static readonly CosName LANG = new CosName("Lang");
        public static readonly CosName LAST = new CosName("Last");
        public static readonly CosName LAST_CHAR = new CosName("LastChar");
        public static readonly CosName LAST_MODIFIED = new CosName("LastModified");
        public static readonly CosName LC = new CosName("LC");
        public static readonly CosName LE = new CosName("LE");
        public static readonly CosName LEADING = new CosName("Leading");
        public static readonly CosName LEGAL_ATTESTATION = new CosName("LegalAttestation");
        public static readonly CosName LENGTH = new CosName("Length");
        public static readonly CosName LENGTH1 = new CosName("Length1");
        public static readonly CosName LENGTH2 = new CosName("Length2");
        public static readonly CosName LIGHTEN = new CosName("Lighten");
        public static readonly CosName LIMITS = new CosName("Limits");
        public static readonly CosName LJ = new CosName("LJ");
        public static readonly CosName LL = new CosName("LL");
        public static readonly CosName LLE = new CosName("LLE");
        public static readonly CosName LLO = new CosName("LLO");
        public static readonly CosName LOCATION = new CosName("Location");
        public static readonly CosName LUMINOSITY = new CosName("Luminosity");
        public static readonly CosName LW = new CosName("LW");
        public static readonly CosName LZW_DECODE = new CosName("LZWDecode");
        public static readonly CosName LZW_DECODE_ABBREVIATION = new CosName("LZW");
        // M
        public static readonly CosName M = new CosName("M");
        public static readonly CosName MAC = new CosName("Mac");
        public static readonly CosName MAC_EXPERT_ENCODING = new CosName("MacExpertEncoding");
        public static readonly CosName MAC_ROMAN_ENCODING = new CosName("MacRomanEncoding");
        public static readonly CosName MARK_INFO = new CosName("MarkInfo");
        public static readonly CosName MASK = new CosName("Mask");
        public static readonly CosName MATRIX = new CosName("Matrix");
        public static readonly CosName MAX_LEN = new CosName("MaxLen");
        public static readonly CosName MAX_WIDTH = new CosName("MaxWidth");
        public static readonly CosName MCID = new CosName("MCID");
        public static readonly CosName MDP = new CosName("MDP");
        public static readonly CosName MEDIA_BOX = new CosName("MediaBox");
        public static readonly CosName METADATA = new CosName("Metadata");
        public static readonly CosName MISSING_WIDTH = new CosName("MissingWidth");
        public static readonly CosName MIX = new CosName("Mix");
        public static readonly CosName MK = new CosName("MK");
        public static readonly CosName ML = new CosName("ML");
        public static readonly CosName MM_TYPE1 = new CosName("MMType1");
        public static readonly CosName MOD_DATE = new CosName("ModDate");
        public static readonly CosName MULTIPLY = new CosName("Multiply");
        // N
        public static readonly CosName N = new CosName("N");
        public static readonly CosName NAME = new CosName("Name");
        public static readonly CosName NAMES = new CosName("Names");
        public static readonly CosName NEED_APPEARANCES = new CosName("NeedAppearances");
        public static readonly CosName NEXT = new CosName("Next");
        public static readonly CosName NM = new CosName("NM");
        public static readonly CosName NON_EFONT_NO_WARN = new CosName("NonEFontNoWarn");
        public static readonly CosName NON_FULL_SCREEN_PAGE_MODE = new CosName("NonFullScreenPageMode");
        public static readonly CosName NONE = new CosName("None");
        public static readonly CosName NORMAL = new CosName("Normal");
        public static readonly CosName NUMS = new CosName("Nums");
        // O
        public static readonly CosName O = new CosName("O");
        public static readonly CosName OBJ = new CosName("Obj");
        public static readonly CosName OBJ_STM = new CosName("ObjStm");
        public static readonly CosName OC = new CosName("OC");
        public static readonly CosName OCG = new CosName("OCG");
        public static readonly CosName OCGS = new CosName("OCGs");
        public static readonly CosName OCPROPERTIES = new CosName("OCProperties");
        public static readonly CosName OE = new CosName("OE");

        /**
         * "OFF", to be used for OCGs, not for Acroform
         */
        public static readonly CosName OFF = new CosName("OFF");

        /**
         * "Off", to be used for Acroform, not for OCGs
         */
        public static readonly CosName Off = new CosName("Off");

        public static readonly CosName ON = new CosName("ON");
        public static readonly CosName OP = new CosName("OP");
        public static readonly CosName OP_NS = new CosName("op");
        public static readonly CosName OPEN_ACTION = new CosName("OpenAction");
        public static readonly CosName OPEN_TYPE = new CosName("OpenType");
        public static readonly CosName OPM = new CosName("OPM");
        public static readonly CosName OPT = new CosName("Opt");
        public static readonly CosName ORDER = new CosName("Order");
        public static readonly CosName ORDERING = new CosName("Ordering");
        public static readonly CosName OS = new CosName("OS");
        public static readonly CosName OUTLINES = new CosName("Outlines");
        public static readonly CosName OUTPUT_CONDITION = new CosName("OutputCondition");
        public static readonly CosName OUTPUT_CONDITION_IDENTIFIER = new CosName(
            "OutputConditionIdentifier");
        public static readonly CosName OUTPUT_INTENT = new CosName("OutputIntent");
        public static readonly CosName OUTPUT_INTENTS = new CosName("OutputIntents");
        public static readonly CosName OVERLAY = new CosName("Overlay");
        // P
        public static readonly CosName P = new CosName("P");
        public static readonly CosName PAGE = new CosName("Page");
        public static readonly CosName PAGE_LABELS = new CosName("PageLabels");
        public static readonly CosName PAGE_LAYOUT = new CosName("PageLayout");
        public static readonly CosName PAGE_MODE = new CosName("PageMode");
        public static readonly CosName PAGES = new CosName("Pages");
        public static readonly CosName PAINT_TYPE = new CosName("PaintType");
        public static readonly CosName PANOSE = new CosName("Panose");
        public static readonly CosName PARAMS = new CosName("Params");
        public static readonly CosName PARENT = new CosName("Parent");
        public static readonly CosName PARENT_TREE = new CosName("ParentTree");
        public static readonly CosName PARENT_TREE_NEXT_KEY = new CosName("ParentTreeNextKey");
        public static readonly CosName PATTERN = new CosName("Pattern");
        public static readonly CosName PATTERN_TYPE = new CosName("PatternType");
        public static readonly CosName PDF_DOC_ENCODING = new CosName("PDFDocEncoding");
        public static readonly CosName PERMS = new CosName("Perms");
        public static readonly CosName PG = new CosName("Pg");
        public static readonly CosName PRE_RELEASE = new CosName("PreRelease");
        public static readonly CosName PREDICTOR = new CosName("Predictor");
        public static readonly CosName PREV = new CosName("Prev");
        public static readonly CosName PRINT_AREA = new CosName("PrintArea");
        public static readonly CosName PRINT_CLIP = new CosName("PrintClip");
        public static readonly CosName PRINT_SCALING = new CosName("PrintScaling");
        public static readonly CosName PROC_SET = new CosName("ProcSet");
        public static readonly CosName PROCESS = new CosName("Process");
        public static readonly CosName PRODUCER = new CosName("Producer");
        public static readonly CosName PROP_BUILD = new CosName("Prop_Build");
        public static readonly CosName PROPERTIES = new CosName("Properties");
        public static readonly CosName PS = new CosName("PS");
        public static readonly CosName PUB_SEC = new CosName("PubSec");
        // Q
        public static readonly CosName Q = new CosName("Q");
        public static readonly CosName QUADPOINTS = new CosName("QuadPoints");
        // R
        public static readonly CosName R = new CosName("R");
        public static readonly CosName RANGE = new CosName("Range");
        public static readonly CosName RC = new CosName("RC");
        public static readonly CosName RD = new CosName("RD");
        public static readonly CosName REASON = new CosName("Reason");
        public static readonly CosName REASONS = new CosName("Reasons");
        public static readonly CosName REPEAT = new CosName("Repeat");
        public static readonly CosName RECIPIENTS = new CosName("Recipients");
        public static readonly CosName RECT = new CosName("Rect");
        public static readonly CosName REGISTRY = new CosName("Registry");
        public static readonly CosName REGISTRY_NAME = new CosName("RegistryName");
        public static readonly CosName RENAME = new CosName("Rename");
        public static readonly CosName RESOURCES = new CosName("Resources");
        public static readonly CosName RGB = new CosName("RGB");
        public static readonly CosName RI = new CosName("RI");
        public static readonly CosName ROLE_MAP = new CosName("RoleMap");
        public static readonly CosName ROOT = new CosName("Root");
        public static readonly CosName ROTATE = new CosName("Rotate");
        public static readonly CosName ROWS = new CosName("Rows");
        public static readonly CosName RUN_LENGTH_DECODE = new CosName("RunLengthDecode");
        public static readonly CosName RUN_LENGTH_DECODE_ABBREVIATION = new CosName("RL");
        public static readonly CosName RV = new CosName("RV");
        // S
        public static readonly CosName S = new CosName("S");
        public static readonly CosName SA = new CosName("SA");
        public static readonly CosName SCREEN = new CosName("Screen");
        public static readonly CosName SE = new CosName("SE");
        public static readonly CosName SEPARATION = new CosName("Separation");
        public static readonly CosName SET_F = new CosName("SetF");
        public static readonly CosName SET_FF = new CosName("SetFf");
        public static readonly CosName SHADING = new CosName("Shading");
        public static readonly CosName SHADING_TYPE = new CosName("ShadingType");
        public static readonly CosName SIG = new CosName("Sig");
        public static readonly CosName SIG_FLAGS = new CosName("SigFlags");
        public static readonly CosName SIZE = new CosName("Size");
        public static readonly CosName SM = new CosName("SM");
        public static readonly CosName SMASK = new CosName("SMask");
        public static readonly CosName SOFT_LIGHT = new CosName("SoftLight");
        public static readonly CosName SOUND = new CosName("Sound");
        public static readonly CosName SS = new CosName("SS");
        public static readonly CosName ST = new CosName("St");
        public static readonly CosName STANDARD_ENCODING = new CosName("StandardEncoding");
        public static readonly CosName STATE = new CosName("State");
        public static readonly CosName STATE_MODEL = new CosName("StateModel");
        public static readonly CosName STATUS = new CosName("Status");
        public static readonly CosName STD_CF = new CosName("StdCF");
        public static readonly CosName STEM_H = new CosName("StemH");
        public static readonly CosName STEM_V = new CosName("StemV");
        public static readonly CosName STM_F = new CosName("StmF");
        public static readonly CosName STR_F = new CosName("StrF");
        public static readonly CosName STRUCT_PARENT = new CosName("StructParent");
        public static readonly CosName STRUCT_PARENTS = new CosName("StructParents");
        public static readonly CosName STRUCT_TREE_ROOT = new CosName("StructTreeRoot");
        public static readonly CosName STYLE = new CosName("Style");
        public static readonly CosName SUB_FILTER = new CosName("SubFilter");
        public static readonly CosName SUBJ = new CosName("Subj");
        public static readonly CosName SUBJECT = new CosName("Subject");
        public static readonly CosName SUBTYPE = new CosName("Subtype");
        public static readonly CosName SUPPLEMENT = new CosName("Supplement");
        public static readonly CosName SV = new CosName("SV");
        public static readonly CosName SW = new CosName("SW");
        public static readonly CosName SY = new CosName("Sy");
        public static readonly CosName SYNCHRONOUS = new CosName("Synchronous");
        // T
        public static readonly CosName T = new CosName("T");
        public static readonly CosName TARGET = new CosName("Target");
        public static readonly CosName TEMPLATES = new CosName("Templates");
        public static readonly CosName THREADS = new CosName("Threads");
        public static readonly CosName THUMB = new CosName("Thumb");
        public static readonly CosName TI = new CosName("TI");
        public static readonly CosName TILING_TYPE = new CosName("TilingType");
        public static readonly CosName TIME_STAMP = new CosName("TimeStamp");
        public static readonly CosName TITLE = new CosName("Title");
        public static readonly CosName TK = new CosName("TK");
        public static readonly CosName TM = new CosName("TM");
        public static readonly CosName TO_UNICODE = new CosName("ToUnicode");
        public static readonly CosName TR = new CosName("TR");
        public static readonly CosName TR2 = new CosName("TR2");
        public static readonly CosName TRAPPED = new CosName("Trapped");
        public static readonly CosName TRANS = new CosName("Trans");
        public static readonly CosName TRANSPARENCY = new CosName("Transparency");
        public static readonly CosName TREF = new CosName("TRef");
        public static readonly CosName TRIM_BOX = new CosName("TrimBox");
        public static readonly CosName TRUE_TYPE = new CosName("TrueType");
        public static readonly CosName TRUSTED_MODE = new CosName("TrustedMode");
        public static readonly CosName TU = new CosName("TU");
        /** Acro form field type for text field. */
        public static readonly CosName TX = new CosName("Tx");
        public static readonly CosName TYPE = new CosName("Type");
        public static readonly CosName TYPE0 = new CosName("Type0");
        public static readonly CosName TYPE1 = new CosName("Type1");
        public static readonly CosName TYPE3 = new CosName("Type3");
        // U
        public static readonly CosName U = new CosName("U");
        public static readonly CosName UE = new CosName("UE");
        public static readonly CosName UF = new CosName("UF");
        public static readonly CosName UNCHANGED = new CosName("Unchanged");
        public static readonly CosName UNIX = new CosName("Unix");
        public static readonly CosName URI = new CosName("URI");
        public static readonly CosName URL = new CosName("URL");
        // V
        public static readonly CosName V = new CosName("V");
        public static readonly CosName VERISIGN_PPKVS = new CosName("VeriSign.PPKVS");
        public static readonly CosName VERSION = new CosName("Version");
        public static readonly CosName VERTICES = new CosName("Vertices");
        public static readonly CosName VERTICES_PER_ROW = new CosName("VerticesPerRow");
        public static readonly CosName VIEW_AREA = new CosName("ViewArea");
        public static readonly CosName VIEW_CLIP = new CosName("ViewClip");
        public static readonly CosName VIEWER_PREFERENCES = new CosName("ViewerPreferences");
        public static readonly CosName VOLUME = new CosName("Volume");
        // W
        public static readonly CosName W = new CosName("W");
        public static readonly CosName W2 = new CosName("W2");
        public static readonly CosName WHITE_POINT = new CosName("WhitePoint");
        public static readonly CosName WIDGET = new CosName("Widget");
        public static readonly CosName WIDTH = new CosName("Width");
        public static readonly CosName WIDTHS = new CosName("Widths");
        public static readonly CosName WIN_ANSI_ENCODING = new CosName("WinAnsiEncoding");
        // X
        public static readonly CosName XFA = new CosName("XFA");
        public static readonly CosName X_STEP = new CosName("XStep");
        public static readonly CosName XHEIGHT = new CosName("XHeight");
        public static readonly CosName XOBJECT = new CosName("XObject");
        public static readonly CosName XREF = new CosName("XRef");
        public static readonly CosName XREF_STM = new CosName("XRefStm");
        // Y
        public static readonly CosName Y_STEP = new CosName("YStep");
        public static readonly CosName YES = new CosName("Yes");

        public string Name { get; }

        /// <summary>
        /// Create or retrieve a <see cref="CosName"/> object with a given name.
        /// </summary>
        /// <returns><see langword="null"/> if the string is invalid, an instance of the given <see cref="CosName"/> otherwise.</returns>
        [CanBeNull]
        [DebuggerStepThrough]
        public static CosName Create(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            
            if (!CommonNameMap.TryGetValue(name, out var cosName))
            {
                if (!NameMap.TryGetValue(name, out cosName))
                {
                    cosName = new CosName(name, false);
                }
            }

            return cosName;
        }

        public static bool Equals(CosName f, CosName s)
        {
            return string.Equals(f?.Name, s?.Name);
        }

        /**
         * Private constructor. This will limit the number of CosName objects. that are created.
         * 
         * @param name The name of the CosName object.
         * @param staticValue Indicates if the CosName object is static so that it can be stored in the HashMap without
         * synchronizing.
         */
        private CosName(string aName, bool staticValue = true)
        {
            Name = aName;
            if (staticValue)
            {
                CommonNameMap.Add(aName, this);
            }
            else
            {
                NameMap.TryAdd(aName, this);
            }
        }

        public override string ToString()
        {
            return $"/{Name}";
        }

        public void WriteToPdfStream(StreamWriter output)
        {
            output.Write('/');
            byte[] bytes = Encoding.ASCII.GetBytes(Name);

            foreach (var b in bytes)
            {
                int current = b & 0xFF;

                // be more restrictive than the PDF spec, "Name Objects", see PDFBOX-2073
                if (current >= 'A' && current <= 'Z' ||
                    current >= 'a' && current <= 'z' ||
                    current >= '0' && current <= '9' ||
                    current == '+' ||
                    current == '-' ||
                    current == '_' ||
                    current == '@' ||
                    current == '*' ||
                    current == '$' ||
                    current == ';' ||
                    current == '.')
                {
                    output.Write(current);
                }
                else
                {
                    output.Write('#');
                    Hex.WriteHexByte(b, output);
                }
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CosName);
        }

        public bool Equals(CosName other)
        {
            return string.Equals(Name, other?.Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        /// <summary>
        /// Is the name <see cref="string.Empty"/>?
        /// </summary>
        public bool IsEmpty()
        {
            return Name == string.Empty;
        }

        public override object Accept(ICosVisitor visitor)
        {
            return visitor.VisitFromName(this);
        }

        /**
         * Not usually needed except if resources need to be reclaimed in a long running process.
         */
        public static void ClearResources()
        {
            // Clear them all
            NameMap.Clear();
        }

        public int CompareTo(CosName other)
        {
            return Name?.CompareTo(other?.Name) ?? 0;
        }
    }

}
