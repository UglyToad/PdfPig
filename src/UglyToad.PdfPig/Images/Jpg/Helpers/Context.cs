// ReSharper disable UnusedVariable
namespace UglyToad.PdfPig.Images.Jpg.Helpers
{
    using HuffmanTreeNode = Parts.HuffmanTreeNode;
    using Component = Parts.Component;
    using UglyToad.PdfPig.Images.Jpg.Parts.Exif;
    using System.Collections.Generic;

    internal class Context
    {
        public int BitsPerComponent { get; set; }
        public bool HasImageMask { get; set; }

        public object ColorSpace { get; set; }
        public ColorSpaceFamily ColorspaceFamily { get; set; }

        public bool m_bDefaultDecode { get; set; } = false;

        //public byte[] posb;         // C#: Because we don't have fancy pointers.
        public Result error;
        public int pos;
        public int size;
        public int length;
        public int width, height;
        public int mbwidth, mbheight;
        public int mbsizex, mbsizey;
        public int ncomp;
        public Component[] comp;
        public int qtused, qtavail;
        public byte[][] qtab;
        public HuffmanTreeNode[][] vlctab;
        public int buf, bufbits;
        public int[] block;
        public int rstinterval;
        public byte[] rgb;

        public List<string> Comments = new List<string>();

        #region Adobe App Segment Values
        public bool hasAdobeSegment = false;
        public int colorTransformCode;
        public int versionDctEncodeDecode;
        public int flag0;
        public int flag1;
        #endregion

        #region App0 Segment
        public bool hasApp0segment = false;
        public int App0versionMajor;
        public int App0versionMinor;
        public int App0density_unit;
        public int App0X_density;
        public int App0Y_density;
        #endregion

        public J_COLOR_SPACE jpeg_color_space = J_COLOR_SPACE.JCS_UNKNOWN;
        public J_COLOR_SPACE out_color_space = J_COLOR_SPACE.JCS_UNKNOWN;

        #region Decompress parameters
        /* Decompression parameters. */
        // ReSharper disable UnusedVariable
#pragma warning disable 414
        int scale_num = 1;         /* 1:1 scaling */
        int scale_denom = 1;
        double output_gamma = 1.0;
        bool buffered_image = false;
        bool raw_data_out = false;
        J_DCT_METHOD dct_method = J_DCT_METHOD.JDCT_ISLOW;
        bool do_fancy_upsampling = true;
        bool do_block_smoothing = true;
        bool quantize_colors = false;
        /* We set these in case application only sets quantize_colors. */
        J_DITHER_MODE dither_mode = J_DITHER_MODE.JDITHER_FS;
        bool two_pass_quantize = false;
        int desired_number_of_colors = 256;
        //JSAMPARRAY colormap = NULL; /* ptr to one image row of pixel samples. */
        /* Initialize for no mode change in buffered-image mode. */
        bool enable_1pass_quant = false;
        bool enable_external_quant = false;
        bool enable_2pass_quant = false;
#pragma warning restore 414
        // ReSharper restore UnusedVariable
        #endregion


        public enum Result
        {
            OK = 0,         // no error, decoding successful
            NO_JPEG,        // not a JPEG file
            UNSUPPORTED,    // unsupported format
            OUT_OF_MEM,     // out of memory
            INTERNAL_ERROR, // internal error
            SYNTAX_ERROR,   // syntax error
            FINISHED,       // used internally, will never be reported
        };

        public enum J_COLOR_SPACE
        {
            JCS_UNKNOWN,            /* error/unspecified */
            JCS_GRAYSCALE,          /* monochrome */
            JCS_RGB,                /* red/green/blue as specified by the RGB_RED,                             RGB_GREEN, RGB_BLUE, and RGB_PIXELSIZE macros */
            JCS_YCbCr,              /* Y/Cb/Cr (also known as YUV) */
            JCS_CMYK,               /* C/M/Y/K */
            JCS_YCCK,               /* Y/Cb/Cr/K */
            JCS_EXT_RGB,            /* red/green/blue */
            JCS_EXT_RGBX,           /* red/green/blue/x */
            JCS_EXT_BGR,            /* blue/green/red */
            JCS_EXT_BGRX,           /* blue/green/red/x */
            JCS_EXT_XBGR,           /* x/blue/green/red */
            JCS_EXT_XRGB,           /* x/red/green/blue */
            /* When out_color_space it set to JCS_EXT_RGBX, JCS_EXT_BGRX, JCS_EXT_XBGR,
               or JCS_EXT_XRGB during decompression, the X byte is undefined, and in
               order to ensure the best performance, libjpeg-turbo can set that byte to
               whatever value it wishes.  Use the following colorspace constants to
               ensure that the X byte is set to 0xFF, so that it can be interpreted as an
               opaque alpha channel. */
            JCS_EXT_RGBA,           /* red/green/blue/alpha */
            JCS_EXT_BGRA,           /* blue/green/red/alpha */
            JCS_EXT_ABGR,           /* alpha/blue/green/red */
            JCS_EXT_ARGB,           /* alpha/red/green/blue */
            JCS_RGB565              /* 5-bit red/6-bit green/5-bit blue */
        };

        /* Dithering options for decompression. */

        enum J_DITHER_MODE
        {
            JDITHER_NONE,           /* no dithering */
            JDITHER_ORDERED,        /* simple ordered dither */
            JDITHER_FS              /* Floyd-Steinberg error diffusion dither */
        };


        /* DCT/IDCT algorithm options. */

        enum J_DCT_METHOD
        {
            JDCT_ISLOW,             /* accurate integer method */
            JDCT_IFAST,             /* less accurate integer method [legacy feature] */
            JDCT_FLOAT              /* floating-point method [legacy feature] */
        };

        public enum ColorSpaceFamily
        {
            kUnknown = 0,
            kDeviceGray = 1,
            kDeviceRGB = 2,
            kDeviceCMYK = 3,
            kCalGray = 4,
            kCalRGB = 5,
            kLab = 6,
            kICCBased = 7,
            kSeparation = 8,
            kDeviceN = 9,
            kIndexed = 10,
            kPattern = 11,
        };

        internal class ContextForSOF0
        {
            private Context context;
            internal int width { set { context.width = value; } }
            internal int height { set { context.height = value; } }
            internal Component[] comp { set { context.comp = value; } }
            internal byte[] rgb { set { context.rgb = value; } }

            internal int ncomp { set { context.ncomp = value; } }


            internal int mbwidth { set { context.mbwidth = value; } }
            internal int mbheight { set { context.mbheight = value; } }

            internal int mbsizex { set { context.mbsizex = value; } }
            internal int mbsizey { set { context.mbsizey = value; } }

            internal int qtused { set { context.qtused = value; } }
             
            internal ContextForSOF0(Context context)
            {
                this.context = context;
            }
        }

        internal class ContextForSOF2
        {
            private Context context;
            internal int width { set { context.width = value; } }
            internal int height { set { context.height = value; } }
            internal Component[] comp { set { context.comp = value; } }
            internal byte[] rgb { set { context.rgb = value; } }

            internal int ncomp { set { context.ncomp = value; } }


            internal int mbwidth { set { context.mbwidth = value; } }
            internal int mbheight { set { context.mbheight = value; } }

            internal int mbsizex { set { context.mbsizex = value; } }
            internal int mbsizey { set { context.mbsizey = value; } }

            internal int qtused { set { context.qtused = value; } }

            internal ContextForSOF2(Context context)
            {
                this.context = context;
            }
        }
        internal class ContextForStartOfScan
        {
            private Context context;

            public Component[] comp => context.comp;
            public int ncomp => context.ncomp;

            public HuffmanTreeNode[][] vlctab => context.vlctab;

            public int rstinterval => context.rstinterval;

            public int mbwidth => context.mbwidth;

            public int mbheight => context.mbheight;

            public byte[][] qtab => context.qtab;
             

            internal ContextForStartOfScan(Context context)
            {
                this.context = context;

            }
        }

        internal class ContextForDefineHuffmanTables
        {
            private Context context;

            public Component[] comp => context.comp;
            public int ncomp => context.ncomp;

            public HuffmanTreeNode[][] vlctab { get { return context.vlctab; } set { context.vlctab = value; } }

            internal ContextForDefineHuffmanTables(Context context)
            {
                this.context = context;

            }
        }

        internal class ContextForQuantizationTableSpecification
        {
            private Context context;


            public byte[][] qtab { get { return context.qtab; } set { context.qtab = value; } }

            public int qtavail { get { return context.qtavail; } set { context.qtavail = value; } }


            internal ContextForQuantizationTableSpecification(Context context)
            {
                this.context = context;

            }
        }
        internal class ContextForDefineRestartInterval
        {
            private Context context;
              
            public int rstinterval { get { return context.rstinterval; } set { context.rstinterval = value; } }


            internal ContextForDefineRestartInterval(Context context)
            {
                this.context = context;
            }
        }

        internal class ContextForAdobeAppSegment
        {
            private Context context;

            public bool hasAdobeSegment { set { context.hasAdobeSegment = value; } }
            public int colorTransformCode { set { context.colorTransformCode = value; } }
            public int versionDctEncodeDecode { set { context.versionDctEncodeDecode = value; } }
            public int flag0 { set { context.flag0 = value; } }
            public int flag1 { set { context.flag1 = value; } }
             
            internal ContextForAdobeAppSegment(Context context)
            {
                this.context = context;
            }
        }

        internal class ContextForApp0JFIFSegment
        {
            private Context context;

            public bool hasApp0segment { set { context.hasApp0segment = value; } }
            public int App0versionMajor { set { context.App0versionMajor = value; } }
            public int App0versionMinor { set { context.App0versionMinor = value; } }
            public int App0density_unit { set { context.App0density_unit = value; } }
            public int App0X_density { set { context.App0X_density = value; } }
            public int App0Y_density { set { context.App0Y_density = value; } }

            internal ContextForApp0JFIFSegment(Context context)
            {
                this.context = context;
            }
        }

        internal class ContextForApp1Segment
        {
            private Context context;

            public ExifImageProperties ExifImageProperties { get; set; }

            public System.Xml.XmlDocument XMP { get; set; }

            internal ContextForApp1Segment(Context context)
            {
                this.context = context;
            }
        }

        internal class ContextForColorSpaceHints
        {
            private Context context;

            public bool hasAdobeSegment => context.hasAdobeSegment;
            public bool hasApp0segment => context.hasApp0segment;

            public int colorTransformCode => context.colorTransformCode;

            public Component[] comp => context.comp;

            public int ncomp => context.ncomp;

            public J_COLOR_SPACE jpeg_color_space { set { context.jpeg_color_space = value; } }
            public J_COLOR_SPACE out_color_space { set { context.out_color_space = value; } }

            internal ContextForColorSpaceHints(Context context)
            {
                this.context = context;
            }
        }

        public Context()
        {
            //this.posb = null;
            this.comp = new Component[3];
            this.block = new int[64];
            this.qtab = new byte[4][];
            this.vlctab = new HuffmanTreeNode[4][];
            for (byte i = 0; i < 4; i++)
            {
                this.qtab[i] = new byte[64];
                this.vlctab[i] = new HuffmanTreeNode[65536];
                if (i < this.comp.Length)
                {
                    this.comp[i] = new Component();
                }
            }


        }
        internal ContextForDefineHuffmanTables ForDefineHuffmanTables => new ContextForDefineHuffmanTables(this);
        internal ContextForSOF0 ForStartOfFrame => new ContextForSOF0(this);
        internal ContextForSOF2 ForStart2fFrame => new ContextForSOF2(this);
        internal ContextForStartOfScan ForStartOfScan => new ContextForStartOfScan(this);

        internal ContextForQuantizationTableSpecification ForDefineQuantizationTable => new ContextForQuantizationTableSpecification(this);

        internal ContextForDefineRestartInterval ForDefineRestartInterval => new ContextForDefineRestartInterval(this);

        internal ContextForAdobeAppSegment ForAdobeAppSegement => new ContextForAdobeAppSegment(this);

        internal ContextForApp0JFIFSegment ForApp0JFIFSegement => new ContextForApp0JFIFSegment(this);

        internal ContextForApp1Segment ForApp1Segement => new ContextForApp1Segment(this);

        internal ContextForColorSpaceHints ForColorSpaceHints => new ContextForColorSpaceHints(this);
      

    }
}
