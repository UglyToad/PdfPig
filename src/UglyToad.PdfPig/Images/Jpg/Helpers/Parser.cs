namespace UglyToad.PdfPig.Images.Jpg.Helpers
{
    using System;
    using System.Diagnostics;
    using System.IO;
     
    using Images.Jpg.Parts;
    using Dht = Parts.DefineHuffmanTable;
    using Dqt = Parts.DefineQuantizationTable;
    using Sof0 = Parts.StartOfFrame0BaselineDCT;
    using Sof2 = Parts.StartOfFrame2ProgressDctFrame;
    using SOS = Parts.StartOfScan;
    using DictionaryToken = Tokens.DictionaryToken;
    using UglyToad.PdfPig.Filters;

    /// <summary>
    /// Decode a byte stream contain a simple JPEG 1 (T.871) sequential DCT-base image.    
    /// </summary>
    internal class Parser
    {
        // Jpeg 1 Parser
        // the most common jpeg. JPEG Interchange Format (JFIF)
        // See Adobe Technical Note TN.5116 for additional handling inside a PDF.

        // ITU-T81 4.5 Modes of operation
        // There are four distinct modes of operation under which the various coding processes are defined:
        //      1. sequential DCT-based,
        //      2. progressive DCT-based,
        //      3. lossless, and
        //      4. hierarchical.
        // Note only mode 1 is implemented. To be done Baseline vs Extended sequential DCT.

        // ITU-T81 4.5 Table 1 Essential characteristics
        // Baseline (all DCT-based decoders)
        //    Sequential
        //    Huffman coding: 2 AC and 2 DC tables
        //    Decoders shall process scans with 1, 2, 3, and 4 components
        //    Interleaved and non-interleaved scans

        // 8 bit only (16bit or others is not support)
        // **NOT** supported: JPEG2000, JFIF, Exif

        // Based on NanoJPEG  (https://keyj.emphy.de/nanojpeg/)
        // decodes baseline JPEG only, no progressive or lossless JPEG
        // supports 8-bit grayscale and YCbCr images, no 16 bit, CMYK or other color spaces
        // supports any power-of-two chroma subsampling ratio
        // supports restart markers

        // Addition assistance from C# port at https://github.com/JBildstein/NanoJpeg.Net

        // adds support for reading App14 "Adobe" Application Segment
        // there is no shared data and so can be called multiple times (say of different threads)

        public static Jpg Parse(byte[] ab, DictionaryToken dictionary)
        {
            using (var ms = new MemoryStream(ab))
            using (var reader = new JpgBinaryStreamReader(ms))
            {
                return new Parser().Parse(reader, dictionary);                
            }            
        }
         
        private Jpg Parse(JpgBinaryStreamReader reader, DictionaryToken dictionary)
        {
            var context = new Context();
            var NumberOfStartOfImage = 0;
            var NumberOfScans = 0;
            var NumberOfIntervalResets = 0;
            var NumberOfEndOfImage = 0;
#if DEBUG
            var isOnDebug = UglyToad.PdfPig.Images.Jpg.Jpg.isOnDebug;
#endif
            PdfDictionary.Parse(dictionary,context);

            while (reader.isAtEnd == false)
            {            
                (var isMarkerValid, var marker, int length) = ParseSegmentMarker(reader, true);
                if (reader.isAtEnd)
                {
                    if (NumberOfEndOfImage==0)
                    {
                        Debug.WriteLine($"Jpg End of stream without EndOfImage marker (0xD9).");
                    }
                    break;
                }
                if (isMarkerValid == false)
                {
                    Debug.WriteLine($"Jpg Failed to find marker.");
                    break;
                }
                 

                /*  TN.5116 - PDF guidance for Jpg in PDF
                 *  Acceptable markers:
                 *     The SOF0, SOF1, DHT, RSTm, EOI, SOS, DQT, DRI, and COM markers are properly decoded.
                 *     APPn (application-specific) markers are skipped over harmlessly except for the Adobe reserved marker described later
                 *  
                 *  These markers (if present) generate a rejection:
                 *     These markers are not decoded: SOF2-SOF15, DAC, DNL, DHP, EXP, JPGn, TEM, and RESn. 
                 *     If any occurs in a compressed image, it will be rejected. 
                 *     With the exception of DNL, none of these markers is useful in a Baseline DCT or Extended sequential DCT image.      
                 */

                switch (marker)
                {
                    case JpegMarker.Unknown: break;
                    case JpegMarker.StartOfImage: NumberOfStartOfImage++;  break;
                    case JpegMarker.EndOfImage:
                        {
                            ValidateStartOfImageMarker(marker, NumberOfStartOfImage);
                            NumberOfEndOfImage++;                             
                        }
                        break;
                    case JpegMarker.StartOfFrame0BaselineDctFrame:
                        {
                            ValidateStartOfImageMarker(marker, NumberOfStartOfImage);
                            Sof0.ParseSof0(reader, context.ForStartOfFrame); break;
                        }
                    case JpegMarker.StartOfFrame2ProgressiveDctFrame:
                        {
                            ValidateStartOfImageMarker(marker, NumberOfStartOfImage);
                            Sof2.ParseSof2(reader, context.ForStartOfFrame); break;
                        }
                    case JpegMarker.DefineHuffmanTable:
                        {
                            ValidateStartOfImageMarker(marker, NumberOfStartOfImage);
                            Dht.ParseDht(reader, context.ForDefineHuffmanTables);
                        }
                        break;                    
                    case JpegMarker.DefineQuantizationTable:
                        {
                            ValidateStartOfImageMarker(marker, NumberOfStartOfImage);
                            Dqt.ParseDqt(reader, context.ForDefineQuantizationTable);
                            break;
                        }
                    case JpegMarker.StartOfScan:
                        {
                            ValidateStartOfImageMarker(marker, NumberOfStartOfImage);
                            NumberOfScans++;
                            SOS.ParseSos(reader, context.ForStartOfScan);
                        }
                        break;
                    case JpegMarker.DefineRestartInterval:
                        {
                            NumberOfIntervalResets++;
                            ValidateStartOfImageMarker(marker, NumberOfStartOfImage);
                            DefineRestartInterval.ParseDefineRestartInterval(reader,context.ForDefineRestartInterval); 
                        }
                        break;

                    case JpegMarker.ApplicationSpecific1:
                        ValidateStartOfImageMarker(marker, NumberOfStartOfImage);
                        App1.Parse(reader, context.ForApp1Segement);
                        break;
                    case JpegMarker.ApplicationSpecific2:
                    case JpegMarker.ApplicationSpecific3:
                    case JpegMarker.ApplicationSpecific4:
                    case JpegMarker.ApplicationSpecific5:
                    case JpegMarker.ApplicationSpecific6:
                    case JpegMarker.ApplicationSpecific7:
                    case JpegMarker.ApplicationSpecific8:
                    case JpegMarker.ApplicationSpecific9:
                    case JpegMarker.ApplicationSpecific10:
                    case JpegMarker.ApplicationSpecific11:
                    case JpegMarker.ApplicationSpecific12:
                    case JpegMarker.ApplicationSpecific13:                    
                    case JpegMarker.ApplicationSpecific15:
                        {
#if DEBUG
                            if (isOnDebug)
                            {
                                var appBuffer = new byte[length-2];
                                reader.Read(appBuffer, 0, length-2);
                                var appData = new System.Text.ASCIIEncoding().GetString(appBuffer);
                            }
                            else
#endif
                            {
                                reader.Skip(length - 2);
                            }
                        }
                        break;
                    case JpegMarker.ApplicationSpecific0:   // JFIF or JFXX
                        {
                            ValidateStartOfImageMarker(marker, NumberOfStartOfImage);
                            App0JFIF.Parse(reader,context.ForApp0JFIFSegement);
                        }
                        break;
                    case JpegMarker.ApplicationSpecific14:  // Adobe
                        {
                            ValidateStartOfImageMarker(marker, NumberOfStartOfImage);
                            if (NumberOfScans>0)
                            {
                                Debug.WriteLine($"Jpg Adobe Application segment marker (0xEE) after StartOfScan (0xDA).");
                            }
                            AppAdobe.ParseAppAdobe(reader,context.ForAdobeAppSegement);
                        }
                        break;
                    case JpegMarker.Comment:
#if DEBUG
                        if (isOnDebug)
                        {
                            var appBuffer = new byte[length - 2];
                            reader.Read(appBuffer, 0, length - 2);
                            var comment = new System.Text.ASCIIEncoding().GetString(appBuffer);
                            context.Comments.Add(comment);
                        }
                        else
#endif
                        {
                            reader.Skip(length - 2);
                        }
                        break;
                    default:
                        ValidateStartOfImageMarker(marker, NumberOfStartOfImage);
                        throw new Exception($"Unsupported marker: {marker}");
                }
                 
            }
            if (NumberOfStartOfImage > 1)
            {
                Debug.WriteLine($"Jpg Too many StartOfImage marker (0xD8). Expected: 1. Got: {NumberOfStartOfImage}.");
            }
            if (NumberOfEndOfImage > 1)
            {
                Debug.WriteLine($"Jpg Too many EndOfImage markers (0xD9). Expected: 1. Got: {NumberOfEndOfImage}.");
            }
            if (NumberOfScans==0)
            {
                Debug.WriteLine($"Jpg No scan data.");
                throw new Exception("Jpg No scan data.");
            } else if (NumberOfScans>1)
            {

                // Images ... having more than one scan ... will not be decoded. - TN.5116 Page 8
 
            }

            if (context.width <= 0 || context.height <= 0 )
            {
                // Zero-size images ) are invalid. - N.5116 Page 8
                throw new InvalidDataException($"Jpg width {context.width} height: {context.height}. Expected: width > 0 and height >0");
            }
            ConvertScan.Convert(context);
            
            var Width = context.width;
            var Height = context.height;
            var NumberOfComponents = context.ncomp;
            var Data = NumberOfComponents == 1 ? context.comp[0].pixels : context.rgb;
            var jpg = new Jpg(Width,Height, NumberOfComponents, Data, context.Comments);
            return jpg;
        }

        void ValidateStartOfImageMarker(JpegMarker marker, int NumberOfStartOfImage)
        {
            if (NumberOfStartOfImage == 0)
            {
                int i = (int)marker;
                string s = Enum.GetName(typeof(JpegMarker), marker);
                Debug.WriteLine($"Jpg {s} marker (0x{i:X}) without StartOfImage marker (0xD8).");
            }
        }

        private const byte MarkerStart = 0xFF;  //  called "Fill Marker" (0xFF) in TN5116 Page 7
        private static (bool isMakerValid, JpegMarker marker, int length) ParseSegmentMarker(JpgBinaryStreamReader reader, bool skipData = false)
        {
#if DEBUG
            var isOnDebug = UglyToad.PdfPig.Images.Jpg.Jpg.isOnDebug;
#endif
            // A marker segment consists of a marker followed by a sequence of related parameters.
            // For most segments the first parameter in a marker segment is the two - byte length parameter.
            // This length parameter encodes the number of bytes in the marker segment,
            // including the length parameter and excluding the two-byte marker.
            // The marker segments identified by the SOF and SOS
            // marker codes are referred to as headers: the frame header and the scan header respectively
            byte? marker = null;
             
            { 
                byte? previous = null;              
                while (reader.Remaining > 0)
                {
                    var b = reader.ReadByteOrThrow();
                    
                    if (!skipData)
                    {
                        if (!previous.HasValue && b != MarkerStart)
                        {
                            throw new InvalidOperationException();
                        }

                        if (b != MarkerStart)
                        {
                            marker = b;
                            break;
                        }
                    }

                    if (previous.HasValue && previous.Value == MarkerStart && b != MarkerStart)
                    {
                        marker = b;                         
                        break;
                    }

                    previous = b;
                }
            }
            if (marker.HasValue) 
            {                 
                if (Enum.IsDefined(typeof(JpegMarker), marker.Value) == false)
                {
                    Debug.WriteLine($"Unknown marker: 0X{marker.Value:X}");
                    return (true, JpegMarker.Unknown, 0);
                }

                JpegMarker jpegMarker =(JpegMarker)marker.Value;
                int length = 0;
                switch (jpegMarker)
                {
                    case JpegMarker.StartOfImage:
                    case JpegMarker.EndOfImage:
                    case JpegMarker.Restart0:
                    case JpegMarker.Restart1:
                    case JpegMarker.Restart2:
                    case JpegMarker.Restart3:
                    case JpegMarker.Restart4:
                    case JpegMarker.Restart5:
                    case JpegMarker.Restart6:
                    case JpegMarker.Restart7:
                    case JpegMarker.TemporaryPrivateUseInArithmeticCoding:
#if DEBUG
                        if (isOnDebug) { Debug.WriteLine($"Jpg Marker: {jpegMarker} (0x{marker:X})"); }  // No length markers
#endif
                        break;
                    default:
                        {
                            length = reader.ReadShort();

                            if (length < 0)
                            {
                                throw new Exception($"Jpg invalid segment length. Expected >0. Got {length}.");
                            }

                            if (length > reader.Remaining)
                            {
                                throw new Exception($"Jpg invalid segment length. Expected < remaining bytes in file ({reader.Remaining}). Got {length}.");
                            }
                            reader.JumpBack(2);
#if DEBUG
                            if (isOnDebug) { Debug.WriteLine($"Jpg Marker: {jpegMarker} (0x{marker:X}) length: {length}"); } // All others have length
#endif
                        }
                        break;
                }
                 
                return (true, jpegMarker, length);               
            }

            return (false, JpegMarker.Unknown, 0);
        }
    }
}
