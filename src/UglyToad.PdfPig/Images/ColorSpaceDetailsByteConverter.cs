namespace UglyToad.PdfPig.Images
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Graphics.Colors;

    internal static class ColorSpaceDetailsByteConverter
    {
        public static byte[] Convert(ColorSpaceDetails details, IReadOnlyList<byte> decoded)
        {
            switch (details)
            {
                case IndexedColorSpaceDetails indexed:
                    return UnwrapIndexedColorSpaceBytes(indexed, decoded);
            }

            return decoded.ToArray();
        }

        private static byte[] UnwrapIndexedColorSpaceBytes(IndexedColorSpaceDetails indexed, IReadOnlyList<byte> input)
        {
            var multiplier = 1;
            Func<byte, IEnumerable<byte>> transformer = null;
            switch (indexed.BaseColorSpaceDetails.Type)
            {
                case ColorSpace.DeviceRGB:
                    transformer = x =>
                    {
                        var r = new byte[3];
                        for (var i = 0; i < 3; i++)
                        {
                            r[i] = indexed.ColorTable[x + i];
                        }

                        return r;
                    };
                    multiplier = 3;
                    break;
                case ColorSpace.DeviceCMYK:
                    transformer = x =>
                    {
                        var r = new byte[4];
                        for (int i = 0; i < 4; i++)
                        {
                            r[i] = indexed.ColorTable[x + i];
                        }

                        return r;
                    };

                    multiplier = 4;
                    break;
                case ColorSpace.DeviceGray:
                    transformer = x => new[] {indexed.ColorTable[x]};
                    multiplier = 1;
                    break;
            }

            if (transformer != null)
            {
                var result = new byte[input.Count * multiplier];
                var i = 0;
                foreach (var b in input)
                {
                    foreach (var newByte in transformer(b))
                    {
                        result[i++] = newByte;
                    }
                }

                return result;
            }

            return input.ToArray();
        }
    }
}
