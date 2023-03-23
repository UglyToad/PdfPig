namespace UglyToad.PdfPig.Images.Jpg.Parts
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using ContextForSOF0 = Helpers.Context.ContextForSOF0;
 
    internal static class StartOfFrame0BaselineDCT
    {
        internal static void ParseSof0(JpgBinaryStreamReader reader, ContextForSOF0 context)
        {
            int Width;
            int Height;
            int NumberOfComponents;
            Component[] Components;

            // Lf: Frame header length (16 bits)   Possible Values: 8 + 3 × Nf
            // Specifies the length of the frame header
            var frameHeader = reader.DecodeFrameHeader(); // Get Length

            if (frameHeader.remaining < 9) { throw new Exception(); }


            // P: Sample precision  (8 bits) Possible Values: 8 (DCT Baseline) 8 or 12 (DCT Ext) 8 or 12 (Progressive DCT) 2-16 (Lossless)
            // Specifies the precision in bits for the samples of the components in the frame.
            var p = frameHeader.ReadByte();
#if DEBUG
            UglyToad.PdfPig.Images.Jpg.Jpg.precision = p;
#endif
            if (p != 8) { throw new NotImplementedException("Only 8 bit precision is supported."); }


            // Y : Number of lines (16 bits)  Possible Values: 0-65 535
            // Specifies the maximum number of lines in the source image. This shall be equal to the
            // number of lines in the component with the maximum number of vertical samples. Value 0 indicates
            // that the number of lines shall be defined by the DNL marker and parameters at the end of the first scan
            Height = frameHeader.ReadInt16BE();
            if (Height > 5000) { Debug.WriteLine($"Warning: Jpg Height > 5000."); }
            if (Height <= 0 ) { throw new InvalidDataException($"Jpg Height: {Height}. Expected: >0"); }

            // X: Number of samples per line (16 bits) Possible Values:  1-65 535
            // Specifies the maximum number of samples per line in the source image. This
            // shall be equal to the number of samples per line in the component with the maximum number of horizontal
            // samples
            Width = frameHeader.ReadInt16BE();
            if (Width > 5000) { Debug.WriteLine($"Warning: Jpg Width > 5000."); }
            if (Width <= 0) { throw new InvalidDataException($"Jpg Width: {Width}. Expected: >0"); }

            // Nf : Number of image components in frame (8 bits) Possible values (as per spec): 1-255 (Baseline DCT)
            // Specifies the number of source image components in the frame.
            // The value of Nf shall be equal to the number of sets of frame component specification parameters(Ci, Hi, Vi,
            // and Tqi) present in the frame header.
            NumberOfComponents = frameHeader.ReadByte();
            switch (NumberOfComponents)
            {
                case 1:
                case 3: break;
                case 4: throw new NotImplementedException("Warning: Support for 4 component Jpg decode not yet implemented.");
                default:
                    throw new NotImplementedException($"Error: Jpg Number of Components is {NumberOfComponents}. Expected: 1, 3 or 4.");
            }

            if (frameHeader.remaining < 3 * NumberOfComponents) { throw new NotImplementedException(); }

            Components = new Component[3] { new Component(), new Component(), new Component() };

            int MaxHSF = 0;
            int MaxVSF = 0;
            int qtused = 0;
            for (int i = 0; i < NumberOfComponents; i++)
            {
                var component = Components[i];

                // Ci: Component identifier (8 bit) Possible Values: 0-255
                // Assigns a unique label to the ith component in the sequence of frame component
                // specification parameters. These values shall be used in the scan headers to identify the components in the scan.
                // The value of Ci shall be different from the values of C1 through Ci − 1.
                component.cid = frameHeader.ReadByte();

                // Hi: Horizontal sampling factor (HSF)  (4 bits) Possible Values: 1-4
                // Specifies the relationship between the component horizontal dimension
                // and maximum image dimension X; also specifies the number of horizontal data units of component
                // Ci in each MCU, when more than one component is encoded in a scan.

                // Vi: Vertical sampling factor (VSF)    (4 bits) Possible Values: 1-4
                // Specifies the relationship between the component vertical dimension and
                // maximum image dimension Y; also specifies the number of vertical data units of component Ci in
                // each MCU, when more than one component is encoded in a scan.
                {
                    var value = frameHeader.ReadByte();
                    component.HSF = value >> 4;
                    if (component.HSF == 0) { throw new Exception(); }
                    if ((component.HSF & (component.HSF - 1)) != 0) { throw new Exception(); } // not a power of two
                    component.VSF = value & 15;
                    if ((component.VSF & (component.VSF - 1)) != 0) { throw new Exception(); }  // not a power of two
                }

                // Tqi: Quantization table destination selector (8 bits) Possible Values: 0-3
                // Specifies one of four possible quantization table destinations
                // from which the quantization table to use for dequantization of DCT coefficients of component Ci is retrieved.If
                // the decoding process uses the dequantization procedure, this table shall have been installed in this destination
                // by the time the decoder is ready to decode the scan(s) containing component Ci.The destination shall not be respecified, or its contents changed, until all scans containing Ci have been completed.
                {
                    var value = frameHeader.ReadByte();
                    if ((value & 0xFC) != 0) { throw new Exception(); } // Value is not 0, 1, 2 or 3
                    component.qtsel = value;
                }

                qtused |= 1 << component.qtsel;
                if (component.HSF > MaxHSF) { MaxHSF = component.HSF; }
                if (component.VSF > MaxVSF) { MaxVSF = component.VSF; }
            }
            if (NumberOfComponents == 1)
            {
                var component = Components[0];
                component.HSF = 1;
                component.VSF = 1;
                MaxHSF = 1;
                MaxVSF = 1;
            }


            int mbwidth, mbheight;
            int mbsizex, mbsizey;

            mbsizex = MaxHSF << 3;
            mbsizey = MaxVSF << 3;
            mbwidth = (Width + mbsizex - 1) / mbsizex;
            mbheight = (Height + mbsizey - 1) / mbsizey;

            for (int i = 0; i < NumberOfComponents; i++)
            {
                var component = Components[i];
                component.width = (Width * component.HSF + MaxHSF - 1) / MaxHSF;
                var StrideCalc1 = (component.width + 7) & 0x7FFFFFF8;
                component.height = (Height * component.VSF + MaxVSF - 1) / MaxVSF;
                var StrideCalc2 = mbwidth * mbsizex * component.HSF / MaxHSF;
                var StrideCalc3 = mbwidth * component.HSF << 3;
                 
                if (StrideCalc1 != StrideCalc2) { Debug.WriteLine($"Alt stride calc: {StrideCalc1} vs {StrideCalc2} vs {StrideCalc3}. Going with #3"); }
                component.stride = StrideCalc3;
                if (((component.width < 3) && (component.HSF != MaxHSF)) ||
                    ((component.height < 3) && (component.VSF != MaxVSF)))
                {
                    Debug.WriteLine($"component.width: {component.width} component.height: {component.height} component.HSF: {component.HSF} component.VSF: {component.VSF} MaxHSF: {MaxHSF} MaxVSF: {MaxVSF}");

                    var output = string.Empty;
                    if (component.width < 3)
                    {
                        if (component.HSF != MaxHSF) { output += $"component.HSF ({component.HSF}) != MaxHSF ({MaxHSF})"; }
                    }
                    if (component.height < 3)
                    {
                        if (component.VSF != MaxVSF) { output += $"component.VSF ({component.VSF}) != MaxVSF ({MaxVSF})"; }
                    }
                    Debug.WriteLine(output);
                    throw new Exception(output);
                }

                component.pixels = new byte[component.stride * (mbheight * mbsizey * component.VSF / MaxVSF)];
                if (component.pixels is null)
                {
                    throw new OutOfMemoryException();
                }
            }
            byte[] rgb = null;
            if (NumberOfComponents == 3)
            {
                rgb = new byte[Width * Height * NumberOfComponents];
                if (rgb is null)
                {
                    throw new OutOfMemoryException();
                }
            }

            context.width = Width;
            context.height = Height;
            context.comp = Components;
            context.ncomp = NumberOfComponents;

            context.rgb = rgb;
            
            context.mbwidth = mbwidth;
            context.mbheight = mbheight;

            context.mbsizex = mbsizex;
            context.mbsizey = mbsizey;

            context.qtused = qtused;
        }

    }
}
