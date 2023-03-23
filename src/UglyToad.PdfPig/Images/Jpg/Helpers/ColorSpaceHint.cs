using static UglyToad.PdfPig.Images.Jpg.Helpers.Context;

namespace UglyToad.PdfPig.Images.Jpg.Helpers
{
    using System;
    
    using System.Diagnostics;
    using System.Net.NetworkInformation;
    using ContextForColorSpaceHints = Context.ContextForColorSpaceHints;
    using J_COLOR_SPACE = Helpers.Context.J_COLOR_SPACE;
    internal class ColorSpaceHints
    {
        internal static void Get(ContextForColorSpaceHints context)
        {
            // Guess the input colorspace, and set output colorspace accordingly. 
            // DCTDecode dictionary may override guesses.

            switch (context.ncomp)
            {
                case 1:
                    {
                        context.jpeg_color_space = J_COLOR_SPACE.JCS_GRAYSCALE;
                        context.out_color_space = J_COLOR_SPACE.JCS_GRAYSCALE;
                    }
                    break;
                case 3:
                    if (context.hasApp0segment)
                    {
                        /* JFIF implies YCbCr */
                        context.jpeg_color_space = J_COLOR_SPACE.JCS_YCbCr;
                    }
                    else if (context.hasAdobeSegment)
                    {
                        FromAdobeSegment(context, J_COLOR_SPACE.JCS_YCbCr);
                    }
                    else
                    {
                        // Saw no special markers, try to guess from the component IDs
                        FromComponentIds(context);
                    }
                    // Always guess RGB is proper output colorspace. 
                    context.out_color_space = J_COLOR_SPACE.JCS_RGB;
                    break;
                case 4:
                    if (context.hasAdobeSegment)
                    {
                        FromAdobeSegment(context, J_COLOR_SPACE.JCS_YCCK);
                    }
                    else
                    {
                        // No special markers, assume straight CMYK. 
                        context.jpeg_color_space = J_COLOR_SPACE.JCS_CMYK;
                    }
                    context.out_color_space = J_COLOR_SPACE.JCS_CMYK;
                    break;
                default:
                    context.jpeg_color_space = J_COLOR_SPACE.JCS_UNKNOWN;
                    context.out_color_space = J_COLOR_SPACE.JCS_UNKNOWN;
                    break;
            }
        }
        internal static void FromAdobeSegment(ContextForColorSpaceHints context, J_COLOR_SPACE defaultInputColorSpace)
        {
            switch (context.colorTransformCode)
            {
                case 0:
                    context.jpeg_color_space = J_COLOR_SPACE.JCS_RGB;
                    break;
                case 1:
                    context.jpeg_color_space = J_COLOR_SPACE.JCS_YCbCr;
                    break;
                default:
                    Debug.WriteLine($"Warning: Jpg Adobe Segment Colorspace Code is unknown. Got: {context.colorTransformCode}. Expected: 0, 1. Assume input colorspace is {defaultInputColorSpace}.");
                    context.jpeg_color_space = defaultInputColorSpace;
                    break;
            }
        }

        internal static void FromComponentIds(ContextForColorSpaceHints context)
        {
            int cid0 = context.comp[0].cid;
            int cid1 = context.comp[1].cid;
            int cid2 = context.comp[2].cid;

            if (cid0 == 1 && cid1 == 2 && cid2 == 3)            // assume JFIF w/out marker
            {
                context.jpeg_color_space = J_COLOR_SPACE.JCS_YCbCr;
            }
            else if (cid0 == 82 && cid1 == 71 && cid2 == 66)    // ASCII 'R', 'G', 'B' */
            {
                context.jpeg_color_space = J_COLOR_SPACE.JCS_RGB;
            }
            else                                                // assume it's YCbCr 
            {
                Debug.WriteLine($"Warning: Jpg Unknown input colorpace. Assume YCbCr.");
                context.jpeg_color_space = J_COLOR_SPACE.JCS_YCbCr; // assume it's YCbCr 
            }
        }

        internal static uint GetPitch(uint bitsPerComponent,
                                     uint numberOfComponents,
                                     int width)
        {
            if (bitsPerComponent == 0) { throw new ArgumentException(nameof(bitsPerComponent)); }
            if (numberOfComponents == 0) { throw new ArgumentException(nameof(numberOfComponents)); }
            if (width == 0) { throw new ArgumentException(nameof(width)); }

            uint pitch = bitsPerComponent;
            pitch *= numberOfComponents;
            pitch *= (uint)width;
            pitch += 7;
            pitch /= 8;
            return pitch;
        }

        internal static int CalculateBitsPerPixel(uint bitsPerComponent, uint numberOfComponents)
        {
            if (bitsPerComponent == 0)
            {
                throw new Exception($"Jpg Expected bits per pixel not to be zero.");
            }
            uint bitsPerPixel = bitsPerComponent * numberOfComponents;
            if (bitsPerPixel == 1)
            {
                return 1;
            }
            if (bitsPerPixel <= 8)
            {
                return 8;
            }
            return 24;
        }


        public enum FXDIB_Format : UInt16
        {
            kInvalid = 0,
            k1bppRgb = 0x001,
            k8bppRgb = 0x008,
            kRgb = 0x018,
            kRgb32 = 0x020,
            k1bppMask = 0x101,
            k8bppMask = 0x108,
            kArgb = 0x220,
        };


        public static FXDIB_Format MakeRGBFormat(int bitsPerPixel)
        {
            switch (bitsPerPixel)
            {
                case 1:
                    return FXDIB_Format.k1bppRgb;
                case 8:
                    return FXDIB_Format.k8bppRgb;
                case 24:
                    return FXDIB_Format.kRgb;
                case 32:
                    return FXDIB_Format.kRgb32;
                default:
                    return FXDIB_Format.kInvalid;
            }
        }

        public static int GetBppFromFormat(FXDIB_Format format)
        {
            return ((UInt16)(format)) & 0xff;
        }


        uint CalculatePitch32y(int bitsPerPixel, int width)
        {
            if (bitsPerPixel > 32)
            {
                throw new Exception($"Jpg bits per pixel > 32. Got: {bitsPerPixel}.");
            }
            if (width <=0)
            {
                throw new Exception($"Jpg width <=0. Got: {width}.");
            }
            uint pitch = (uint)bitsPerPixel;
            pitch *= (uint)width;
            pitch += 31;
            pitch /= 32;  // quantized to number of 32-bit words.
            pitch *= 4;   // and then back to bytes, (not just /8 in one step).
            return pitch;
        }

       

        public static void LoadPalette(Context context)
        {
            if (context.ColorSpace is null || context.ColorspaceFamily == ColorSpaceFamily.kPattern)
            {
                return;
            }
            if (context.BitsPerComponent>32)
            {
                throw new Exception($"Jpg Bits Per Component > 32. Got {context.BitsPerComponent}");
            }

            if (context.ncomp > 3)
            {
                throw new Exception($"Jpg Number of components >3. Got {context.ncomp}");
            }
            uint bits = (uint)context.BitsPerComponent;
            bits *= (uint)context.ncomp;
            if (bits > 8)
            {
                return;
            }

            if (bits == 1)
            {
                if (context.m_bDefaultDecode && (context.ColorspaceFamily ==  ColorSpaceFamily.kDeviceGray||
                            context.ColorspaceFamily == ColorSpaceFamily.kDeviceRGB))
                {
                    return;
                }
                //if (context.ColorSpace->CountComponents() > 3)
                //{
                //    return;
                //}
            }
        }
            /*
             * 
      if (!m_pColorSpace || m_Family == CPDF_ColorSpace::Family::kPattern)
        return;

      if (m_bpc == 0)
        return;

      // Use FX_SAFE_UINT32 just to be on the safe side, in case |m_bpc| or
      // |m_nComponents| somehow gets a bad value.
      FX_SAFE_UINT32 safe_bits = m_bpc;
      safe_bits *= m_nComponents;
      uint32_t bits = safe_bits.ValueOrDefault(255);
      if (bits > 8)
        return;

      if (bits == 1) {
        if (m_bDefaultDecode && (m_Family == CPDF_ColorSpace::Family::kDeviceGray ||
                                 m_Family == CPDF_ColorSpace::Family::kDeviceRGB)) {
          return;
        }
        if (m_pColorSpace->CountComponents() > 3) {
          return;
        }
        float color_values[3];
        std::fill(std::begin(color_values), std::end(color_values),
                  m_CompData[0].m_DecodeMin);

        float R = 0.0f;
        float G = 0.0f;
        float B = 0.0f;
        m_pColorSpace->GetRGB(color_values, &R, &G, &B);

        FX_ARGB argb0 = ArgbEncode(255, FXSYS_roundf(R * 255),
                                   FXSYS_roundf(G * 255), FXSYS_roundf(B * 255));
        FX_ARGB argb1;
        const CPDF_IndexedCS* indexed_cs = m_pColorSpace->AsIndexedCS();
        if (indexed_cs && indexed_cs->GetMaxIndex() == 0) {
          // If an indexed color space's hival value is 0, only 1 color is specified
          // in the lookup table. Another color should be set to 0xFF000000 by
          // default to set the range of the color space.
          argb1 = 0xFF000000;
        } else {
          color_values[0] += m_CompData[0].m_DecodeStep;
          color_values[1] += m_CompData[0].m_DecodeStep;
          color_values[2] += m_CompData[0].m_DecodeStep;
          m_pColorSpace->GetRGB(color_values, &R, &G, &B);
          argb1 = ArgbEncode(255, FXSYS_roundf(R * 255), FXSYS_roundf(G * 255),
                             FXSYS_roundf(B * 255));
        }

        if (argb0 != 0xFF000000 || argb1 != 0xFFFFFFFF) {
          SetPaletteArgb(0, argb0);
          SetPaletteArgb(1, argb1);
        }
        return;
      }
      if (m_bpc == 8 && m_bDefaultDecode &&
          m_pColorSpace ==
              CPDF_ColorSpace::GetStockCS(CPDF_ColorSpace::Family::kDeviceGray)) {
        return;
      }

      int palette_count = 1 << bits;
      // Using at least 16 elements due to the call m_pColorSpace->GetRGB().
      std::vector<float> color_values(std::max(m_nComponents, 16u));
      for (int i = 0; i < palette_count; i++) {
        int color_data = i;
        for (uint32_t j = 0; j < m_nComponents; j++) {
          int encoded_component = color_data % (1 << m_bpc);
          color_data /= 1 << m_bpc;
          color_values[j] = m_CompData[j].m_DecodeMin +
                            m_CompData[j].m_DecodeStep * encoded_component;
        }
        float R = 0;
        float G = 0;
        float B = 0;
        if (m_nComponents == 1 && m_Family == CPDF_ColorSpace::Family::kICCBased &&
            m_pColorSpace->CountComponents() > 1) {
          int nComponents = m_pColorSpace->CountComponents();
          std::vector<float> temp_buf(nComponents);
          for (int k = 0; k < nComponents; ++k)
            temp_buf[k] = color_values[0];
          m_pColorSpace->GetRGB(temp_buf, &R, &G, &B);
        } else {
          m_pColorSpace->GetRGB(color_values, &R, &G, &B);
        }
        SetPaletteArgb(i, ArgbEncode(255, FXSYS_roundf(R * 255),
                                     FXSYS_roundf(G * 255), FXSYS_roundf(B * 255)));
      }
    }
             */

        }
}
