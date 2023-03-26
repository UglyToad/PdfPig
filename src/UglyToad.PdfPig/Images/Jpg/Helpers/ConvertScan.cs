namespace UglyToad.PdfPig.Images.Jpg.Helpers
{
    using System;
    using Parts;

    internal static class ConvertScan
    { 
        public static bool Convert(Context context)
        {
            int i;
            Component c;
            for (i = 0; i < context.ncomp; ++i)
            {
                c = context.comp[i];
                while ((c.width < context.width) || (c.height < context.height))
                {
                    if (c.width < context.width)
                    {
                        var isSuccess =  UpsampleH(c);
                        if (isSuccess == false) return false;
                    }
                    if (c.height < context.height)
                    {
                        var isSuccess = UpsampleV(c);
                        if (isSuccess == false) return false;
                    }
                }
                if ((c.width < context.width) || (c.height < context.height)) { throw new Exception(); } //internal ;
            }   
            if (context.ncomp == 3)
            {
                // convert to RGB
                int x, yy;
                int prgb = 0, py = 0, pcb = 0, pcr = 0;
                for (yy = context.height; yy != 0; --yy)
                {
                    for (x = 0; x < context.width; ++x)
                    {
                        int y = context.comp[0].pixels[py + x] << 8;
                        int cb = context.comp[1].pixels[pcb + x] - 128;
                        int cr = context.comp[2].pixels[pcr + x] - 128;
                        context.rgb[prgb++] = njClip((y + 359 * cr + 128) >> 8);
                        context.rgb[prgb++] = njClip((y - 88 * cb - 183 * cr + 128) >> 8);
                        context.rgb[prgb++] = njClip((y + 454 * cb + 128) >> 8);
                    }
                    py += context.comp[0].stride;
                    pcb += context.comp[1].stride;
                    pcr += context.comp[2].stride;
                }
            }
            else if (context.comp[0].width != context.comp[0].stride)
            {
                // grayscale -> only remove stride  (from NanJpeg.Net)
                var component = context.comp[0];
                byte[] Data;
                
                int d = component.stride - component.width;
                if (d == 0) { Data = component.pixels; }
                else
                {
                    int w = component.width;
                    int h = component.height;

                    Data = new byte[w * h];
                    for (int y = 0; y < component.height; y++)
                    {
                        Buffer.BlockCopy(
                            component.pixels,
                            y * component.stride,
                            Data,
                            y * component.width,
                            component.width);
                    }
                    component.pixels = Data;
                    component.stride = component.width;
                }
            }
            if (context.ncomp == 1 && context.comp[0].pixels.Length != context.comp[0].width * context.comp[0].height)
            {
                if (context.comp[0].pixels.Length > context.comp[0].width * context.comp[0].height)
                {
                    //Truncate block. Seperfulous scan lines at bottom removed.
                    var component = context.comp[0];
                    var newSize = context.comp[0].width * context.comp[0].height;
                    var newPixels = new byte[newSize];

                    Buffer.BlockCopy(
                        component.pixels,
                        0,
                        newPixels,
                        0,
                        newPixels.Length);

                    component.pixels = newPixels;
                }
            }
            return true;
        }

        public static bool UpsampleH(Component c)
        {
            int xmax = c.width - 3;
            byte[] outv;
            int lin = 0, lout = 0;
            int x, y;
            outv = new byte[(c.width * c.height) << 1];
            if (outv == null) throw new OutOfMemoryException();
            for (y = c.height; y != 0; --y)
            {
                outv[lout] = CF(CF2A * c.pixels[lin] + CF2B * c.pixels[lin + 1]);
                outv[lout + 1] = CF(CF3X * c.pixels[lin] + CF3Y * c.pixels[lin + 1] + CF3Z * c.pixels[lin + 2]);
                outv[lout + 2] = CF(CF3A * c.pixels[lin] + CF3B * c.pixels[lin + 1] + CF3C * c.pixels[lin + 2]);
                for (x = 0; x < xmax; ++x)
                {
                    outv[lout + (x << 1) + 3] = CF(CF4A * c.pixels[lin + x] + CF4B * c.pixels[lin + x + 1] + CF4C * c.pixels[lin + x + 2] + CF4D * c.pixels[lin + x + 3]);
                    outv[lout + (x << 1) + 4] = CF(CF4D * c.pixels[lin + x] + CF4C * c.pixels[lin + x + 1] + CF4B * c.pixels[lin + x + 2] + CF4A * c.pixels[lin + x + 3]);
                }
                lin += c.stride;
                lout += c.width << 1;
                outv[lout + -3] = CF(CF3A * c.pixels[lin - 1] + CF3B * c.pixels[lin - 2] + CF3C * c.pixels[lin - 3]);
                outv[lout + -2] = CF(CF3X * c.pixels[lin - 1] + CF3Y * c.pixels[lin - 2] + CF3Z * c.pixels[lin - 3]);
                outv[lout + -1] = CF(CF2A * c.pixels[lin - 1] + CF2B * c.pixels[lin - 2]);
            }
            c.width <<= 1;
            c.stride = c.width;
            c.pixels = outv;
            return true;
        }
        public static bool UpsampleV(Component c)
        {
            int w = c.width, s1 = c.stride, s2 = s1 + s1;
            byte[] outv;
            int cin, cout;
            int x, y;
            outv = new byte[(c.width * c.height) << 1];
            if (outv == null) throw new OutOfMemoryException();
            for (x = 0; x < w; ++x)
            {
                cin = x;
                cout = x;
                outv[cout] = CF(CF2A * c.pixels[cin] + CF2B * c.pixels[cin + s1]); cout += w;
                outv[cout] = CF(CF3X * c.pixels[cin] + CF3Y * c.pixels[cin + s1] + CF3Z * c.pixels[cin + s2]); cout += w;
                outv[cout] = CF(CF3A * c.pixels[cin] + CF3B * c.pixels[cin + s1] + CF3C * c.pixels[cin + s2]); cout += w;
                cin += s1;
                for (y = c.height - 3; y != 0; --y)
                {
                    outv[cout] = CF(CF4A * c.pixels[cin + -s1] + CF4B * c.pixels[cin] + CF4C * c.pixels[cin + s1] + CF4D * c.pixels[cin + s2]); cout += w;
                    outv[cout] = CF(CF4D * c.pixels[cin + -s1] + CF4C * c.pixels[cin] + CF4B * c.pixels[cin + s1] + CF4A * c.pixels[cin + s2]); cout += w;
                    cin += s1;
                }
                cin += s1;
                outv[cout] = CF(CF3A * c.pixels[cin] + CF3B * c.pixels[cin - s1] + CF3C * c.pixels[cin - s2]); cout += w;
                outv[cout] = CF(CF3X * c.pixels[cin] + CF3Y * c.pixels[cin - s1] + CF3Z * c.pixels[cin - s2]); cout += w;
                outv[cout] = CF(CF2A * c.pixels[cin] + CF2B * c.pixels[cin - s1]);
            }
            c.height <<= 1;
            c.stride = c.width;
            c.pixels = outv;
            return true;
        }
        private static readonly int CF4A = (-9);
        private static readonly int CF4B = (111);
        private static readonly int CF4C = (29);
        private static readonly int CF4D = (-3);
        private static readonly int CF3A = (28);
        private static readonly int CF3B = (109);
        private static readonly int CF3C = (-9);
        private static readonly int CF3X = (104);
        private static readonly int CF3Y = (27);
        private static readonly int CF3Z = (-3);
        private static readonly int CF2A = (139);
        private static readonly int CF2B = (-11);
        private static byte CF(int x)
        {
            return njClip(((x) + 64) >> 7);
        }
        private static byte njClip(int x)
        {
            return (byte)((x < 0) ? 0 : ((x > 0xFF) ? 0xFF : (byte)x));
        }
    }
}
