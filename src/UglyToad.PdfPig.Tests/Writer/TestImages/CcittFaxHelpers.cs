namespace UglyToad.PdfPig.Tests.Writer.TestImages
{
    using System;
    using System.IO;
    using System.Reflection;
    using BitMiracle.LibTiff.Classic;

    internal sealed record CcittG4Payload(int Width, int Height, byte[] Data, bool BlackIs1);

    internal static class CcittExtractor
    {
        public static CcittG4Payload FromTiff(string tiffPath)
        {
            using var tif = Tiff.Open(tiffPath, "r");
            if (tif == null)
            {
                throw new InvalidOperationException($"Cannot open TIFF: {tiffPath}");
            }

            tif.SetDirectory(0);

            int compression = TiffFaxInspector.GetIntTag(tif, TiffTag.COMPRESSION);
            if (compression != (int)Compression.CCITTFAX4)
            {
                throw new InvalidOperationException($"Not CCITT G4 (Compression={compression}).");
            }

            int width = TiffFaxInspector.GetIntTag(tif, TiffTag.IMAGEWIDTH);
            int height = TiffFaxInspector.GetIntTag(tif, TiffTag.IMAGELENGTH);

            int spp = TiffFaxInspector.GetIntTagOrDefault(tif, TiffTag.SAMPLESPERPIXEL, 1);
            int bps = TiffFaxInspector.GetIntTagOrDefault(tif, TiffTag.BITSPERSAMPLE, 1);
            if (!(spp == 1 && bps == 1))
            {
                throw new InvalidOperationException($"Not bilevel (spp={spp}, bps={bps}).");
            }

            int photo = TiffFaxInspector.GetIntTagOrDefault(tif, TiffTag.PHOTOMETRIC, (int)Photometric.MINISWHITE);
            bool blackIs1 = photo == (int)Photometric.MINISWHITE;

            var stripOffsets = TryGetLongArrayTag(tif, TiffTag.STRIPOFFSETS);
            var stripByteCounts = TryGetLongArrayTag(tif, TiffTag.STRIPBYTECOUNTS);

            if (stripOffsets != null && stripByteCounts != null && stripOffsets.Length == stripByteCounts.Length && stripOffsets.Length > 0)
            {
                var data = ReadSegments(tiffPath, stripOffsets, stripByteCounts);
                return new CcittG4Payload(width, height, data, blackIs1);
            }

            var tileOffsets = TryGetLongArrayTag(tif, TiffTag.TILEOFFSETS);
            var tileByteCounts = TryGetLongArrayTag(tif, TiffTag.TILEBYTECOUNTS);

            if (tileOffsets != null && tileByteCounts != null && tileOffsets.Length == tileByteCounts.Length && tileOffsets.Length > 0)
            {
                var data = ReadSegments(tiffPath, tileOffsets, tileByteCounts);
                return new CcittG4Payload(width, height, data, blackIs1);
            }

            throw new InvalidOperationException("Cannot locate strips/tiles offsets/bytecounts.");
        }

        private static byte[] ReadSegments(string filePath, long[] offsets, long[] byteCounts)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var ms = new MemoryStream();

            for (int i = 0; i < offsets.Length; i++)
            {
                fs.Seek(offsets[i], SeekOrigin.Begin);
                if (byteCounts[i] > int.MaxValue)
                {
                    throw new InvalidOperationException("Segment too large.");
                }

                int length = (int)byteCounts[i];
                var buffer = new byte[length];
                int read = 0;
                while (read < length)
                {
                    int r = fs.Read(buffer, read, length - read);
                    if (r <= 0)
                    {
                        throw new EndOfStreamException("Unexpected EOF while reading segment.");
                    }

                    read += r;
                }

                ms.Write(buffer, 0, length);
            }

            return ms.ToArray();
        }

        private static long[]? TryGetLongArrayTag(Tiff tif, TiffTag tag)
        {
            var fieldValue = tif.GetField(tag);
            if (fieldValue == null || fieldValue.Length == 0)
            {
                return null;
            }

            object value = fieldValue[0].Value;

            return value switch
            {
                long[] la => la,
                int[] ia => Array.ConvertAll(ia, x => (long)x),
                uint[] uia => Array.ConvertAll(uia, x => (long)x),
                short[] sa => Array.ConvertAll(sa, x => (long)x),
                ushort[] usa => Array.ConvertAll(usa, x => (long)x),
                long l => new[] { l },
                int i => new[] { (long)i },
                uint ui => new[] { (long)ui },
                _ => null
            };
        }
    }

    internal sealed record TiffFaxInfo(int Width, int Height, bool IsCcittG4, bool IsBilevel, bool BlackIs1, string? Diagnostic);

    internal static class TiffFaxInspector
    {
        public static TiffFaxInfo Inspect(string tiffPath)
        {
            try
            {
                using var tif = Tiff.Open(tiffPath, "r");
                if (tif == null)
                {
                    return new TiffFaxInfo(0, 0, false, false, false, "Cannot open TIFF");
                }

                tif.SetDirectory(0);

                int width = GetIntTag(tif, TiffTag.IMAGEWIDTH);
                int height = GetIntTag(tif, TiffTag.IMAGELENGTH);

                int compression = GetIntTag(tif, TiffTag.COMPRESSION);
                bool isG4 = compression == (int)Compression.CCITTFAX4;

                int spp = GetIntTagOrDefault(tif, TiffTag.SAMPLESPERPIXEL, 1);
                int bps = GetIntTagOrDefault(tif, TiffTag.BITSPERSAMPLE, 1);
                bool bilevel = spp == 1 && bps == 1;

                int photo = GetIntTagOrDefault(tif, TiffTag.PHOTOMETRIC, (int)Photometric.MINISWHITE);
                bool blackIs1 = photo == (int)Photometric.MINISWHITE;

                return new TiffFaxInfo(width, height, isG4, bilevel, blackIs1, null);
            }
            catch (Exception ex)
            {
                return new TiffFaxInfo(0, 0, false, false, false, ex.Message);
            }
        }

        internal static int GetIntTag(Tiff tif, TiffTag tag)
        {
            var fieldValue = tif.GetField(tag);
            if (fieldValue == null || fieldValue.Length == 0)
            {
                throw new InvalidOperationException($"Missing TIFF tag {tag}.");
            }

            return Convert.ToInt32(fieldValue[0].Value);
        }

        internal static int GetIntTagOrDefault(Tiff tif, TiffTag tag, int defaultValue)
        {
            var fieldValue = tif.GetField(tag);
            if (fieldValue == null || fieldValue.Length == 0)
            {
                return defaultValue;
            }

            return Convert.ToInt32(fieldValue[0].Value);
        }
    }

    internal static class LibTiffSilencer
    {
        public static void SuppressWarnings() => TrySetHandler("SetWarningHandler");

        public static void SuppressWarningsAndErrors()
        {
            TrySetHandler("SetWarningHandler");
            TrySetHandler("SetErrorHandler");
        }

        private static void TrySetHandler(string methodName)
        {
            var tiffType = typeof(Tiff);

            foreach (var method in tiffType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
                {
                    continue;
                }

                var parameters = method.GetParameters();
                if (parameters.Length != 1)
                {
                    continue;
                }

                var delegateType = parameters[0].ParameterType;
                var handlerInfo = typeof(LibTiffSilencer).GetMethod(
                    nameof(EmptyHandler),
                    BindingFlags.NonPublic | BindingFlags.Static);

                if (handlerInfo is null)
                {
                    return;
                }

                try
                {
                    var handler = Delegate.CreateDelegate(delegateType, handlerInfo);
                    method.Invoke(null, new object[] { handler });
                    return;
                }
                catch
                {
                    return;
                }
            }
        }

        private static void EmptyHandler(string module, string format, params object[] args)
        {
        }
    }
}