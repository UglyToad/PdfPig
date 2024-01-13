namespace UglyToad.PdfPig.Images
{
    using System;
    using System.IO;

    internal static class JpegHandler
    {
        private const byte MarkerStart = 255;
        private const byte StartOfImage = 216;

        public static JpegInformation GetInformation(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!HasRecognizedHeader(stream))
            {
                throw new InvalidOperationException("The input stream did not start with the expected JPEG header [ 255 216 ]");
            }

            var marker = JpegMarker.StartOfImage;

            var shortBuffer = new byte[2];

            while (marker != JpegMarker.EndOfImage)
            {
                switch (marker)
                {
                    case JpegMarker.StartOfImage:
                    case JpegMarker.Restart0:
                    case JpegMarker.Restart1:
                    case JpegMarker.Restart2:
                    case JpegMarker.Restart3:
                    case JpegMarker.Restart4:
                    case JpegMarker.Restart5:
                    case JpegMarker.Restart6:
                    case JpegMarker.Restart7:

                        // No length markers
                        break;
                    case JpegMarker.StartOfBaselineDctFrame:
                    case JpegMarker.StartOfProgressiveDctFrame:
                        {
                            // ReSharper disable once UnusedVariable
                            var length = ReadShort(stream, shortBuffer);
                            var bpp = stream.ReadByte();
                            var height = ReadShort(stream, shortBuffer);
                            var width = ReadShort(stream, shortBuffer);
                            var numberOfComponents = stream.ReadByte();

                            return new JpegInformation(width, height, bpp, numberOfComponents);
                        }
                    case JpegMarker.ApplicationSpecific0:
                    case JpegMarker.ApplicationSpecific1:
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
                    case JpegMarker.ApplicationSpecific14:
                    case JpegMarker.ApplicationSpecific15:
                    default:
                        {
                            var length = ReadShort(stream, shortBuffer);
                            stream.Seek(length - 2, SeekOrigin.Current);
                            break;
                        }
                }

                marker = (JpegMarker)ReadSegmentMarker(stream, true);
            }

            throw new InvalidOperationException("File was a valid JPEG but the width and height could not be determined.");
        }

        private static bool HasRecognizedHeader(Stream stream)
        {
            var bytes = new byte[2];

            var read = stream.Read(bytes, 0, 2);

            if (read != 2)
            {
                return false;
            }

            return bytes[0] == MarkerStart
                   && bytes[1] == StartOfImage;
        }

        private static byte ReadSegmentMarker(Stream stream, bool skipData = false)
        {
            byte? previous = null;
            int currentValue;
            while ((currentValue = stream.ReadByte()) != -1)
            {
                var b = (byte)currentValue;

                if (!skipData)
                {
                    if (!previous.HasValue && b != MarkerStart)
                    {
                        throw new InvalidOperationException();
                    }

                    if (b != MarkerStart)
                    {
                        return b;
                    }
                }

                if (previous.HasValue && previous.Value == MarkerStart && b != MarkerStart)
                {
                    return b;
                }

                previous = b;
            }

            throw new InvalidOperationException();
        }

        private static ushort ReadShort(Stream stream, byte[] buffer)
        {
            var read = stream.Read(buffer, 0, 2);

            if (read != 2)
            {
                throw new InvalidOperationException("Failed to read a short where expected in the JPEG stream.");
            }

            return (ushort)((buffer[0] << 8) + buffer[1]);
        }
    }
}
