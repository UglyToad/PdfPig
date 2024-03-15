namespace UglyToad.PdfPig.Tests.Functions
{
    using UglyToad.PdfPig.Functions;
    using UglyToad.PdfPig.Tests.Tokens;
    using UglyToad.PdfPig.Tokens;
    using UglyToad.PdfPig.Util;

    public class PdfFunctionType0Tests
    {
        private readonly TestPdfTokenScanner testPdfTokenScanner = new TestPdfTokenScanner();
        private readonly TestFilterProvider testFilterProvider = new TestFilterProvider();

        private static ArrayToken GetArrayToken(params double[] data)
        {
            return new ArrayToken(data.Select(v => new NumericToken(v)).ToArray());
        }

        [Fact]
        public void TIKA_1228_0()
        {
            DictionaryToken dictionaryToken = new DictionaryToken(new Dictionary<NameToken, IToken>()
            {
                { NameToken.FunctionType, new NumericToken(0) },
                { NameToken.Domain, GetArrayToken(0, 1) },
                { NameToken.Range, GetArrayToken(0, 1, 0, 1, 0, 1, 0, 1) },

                { NameToken.BitsPerSample, new NumericToken(8) },
                { NameToken.Decode, GetArrayToken(0, 1, 0, 1, 0, 1, 0, 1) },
                { NameToken.Encode, GetArrayToken(0, 254) },
                { NameToken.Size, GetArrayToken(255) }
            });

            byte[] data = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 4, 0, 0, 0, 5, 0, 0, 0, 6, 0, 0, 0, 7, 0, 0, 0, 8, 0, 0, 0, 9, 0, 0, 0, 10, 0, 0, 0, 11, 0, 0, 0, 12, 0, 0, 0, 13, 0, 0, 0, 14, 0, 0, 0, 15, 0, 0, 0, 16, 0, 0, 0, 17, 0, 0, 0, 18, 0, 0, 0, 19, 0, 0, 0, 20, 0, 0, 0, 21, 0, 0, 0, 22, 0, 0, 0, 23, 0, 0, 0, 24, 0, 0, 0, 25, 0, 0, 0, 26, 0, 0, 0, 27, 0, 0, 0, 28, 0, 0, 0, 29, 0, 0, 0, 30, 0, 0, 0, 31, 0, 0, 0, 32, 0, 0, 0, 33, 0, 0, 0, 34, 0, 0, 0, 35, 0, 0, 0, 36, 0, 0, 0, 37, 0, 0, 0, 38, 0, 0, 0, 39, 0, 0, 0, 40, 0, 0, 0, 41, 0, 0, 0, 42, 0, 0, 0, 43, 0, 0, 0, 44, 0, 0, 0, 45, 0, 0, 0, 46, 0, 0, 0, 47, 0, 0, 0, 48, 0, 0, 0, 49, 0, 0, 0, 50, 0, 0, 0, 51, 0, 0, 0, 52, 0, 0, 0, 53, 0, 0, 0, 54, 0, 0, 0, 55, 0, 0, 0, 56, 0, 0, 0, 57, 0, 0, 0, 58, 0, 0, 0, 59, 0, 0, 0, 60, 0, 0, 0, 61, 0, 0, 0, 62, 0, 0, 0, 63, 0, 0, 0, 64, 0, 0, 0, 65, 0, 0, 0, 66, 0, 0, 0, 67, 0, 0, 0, 68, 0, 0, 0, 69, 0, 0, 0, 70, 0, 0, 0, 71, 0, 0, 0, 72, 0, 0, 0, 73, 0, 0, 0, 74, 0, 0, 0, 75, 0, 0, 0, 76, 0, 0, 0, 77, 0, 0, 0, 78, 0, 0, 0, 79, 0, 0, 0, 80, 0, 0, 0, 81, 0, 0, 0, 82, 0, 0, 0, 83, 0, 0, 0, 84, 0, 0, 0, 85, 0, 0, 0, 86, 0, 0, 0, 87, 0, 0, 0, 88, 0, 0, 0, 89, 0, 0, 0, 90, 0, 0, 0, 91, 0, 0, 0, 92, 0, 0, 0, 93, 0, 0, 0, 94, 0, 0, 0, 95, 0, 0, 0, 96, 0, 0, 0, 97, 0, 0, 0, 98, 0, 0, 0, 99, 0, 0, 0, 100, 0, 0, 0, 101, 0, 0, 0, 102, 0, 0, 0, 103, 0, 0, 0, 104, 0, 0, 0, 105, 0, 0, 0, 106, 0, 0, 0, 107, 0, 0, 0, 108, 0, 0, 0, 109, 0, 0, 0, 110, 0, 0, 0, 111, 0, 0, 0, 112, 0, 0, 0, 113, 0, 0, 0, 114, 0, 0, 0, 115, 0, 0, 0, 116, 0, 0, 0, 117, 0, 0, 0, 118, 0, 0, 0, 119, 0, 0, 0, 120, 0, 0, 0, 121, 0, 0, 0, 122, 0, 0, 0, 123, 0, 0, 0, 124, 0, 0, 0, 125, 0, 0, 0, 126, 0, 0, 0, 128, 0, 0, 0, 129, 0, 0, 0, 130, 0, 0, 0, 131, 0, 0, 0, 132, 0, 0, 0, 133, 0, 0, 0, 134, 0, 0, 0, 135, 0, 0, 0, 136, 0, 0, 0, 137, 0, 0, 0, 138, 0, 0, 0, 139, 0, 0, 0, 140, 0, 0, 0, 141, 0, 0, 0, 142, 0, 0, 0, 143, 0, 0, 0, 144, 0, 0, 0, 145, 0, 0, 0, 146, 0, 0, 0, 147, 0, 0, 0, 148, 0, 0, 0, 149, 0, 0, 0, 150, 0, 0, 0, 151, 0, 0, 0, 152, 0, 0, 0, 153, 0, 0, 0, 154, 0, 0, 0, 155, 0, 0, 0, 156, 0, 0, 0, 157, 0, 0, 0, 158, 0, 0, 0, 159, 0, 0, 0, 160, 0, 0, 0, 161, 0, 0, 0, 162, 0, 0, 0, 163, 0, 0, 0, 164, 0, 0, 0, 165, 0, 0, 0, 166, 0, 0, 0, 167, 0, 0, 0, 168, 0, 0, 0, 169, 0, 0, 0, 170, 0, 0, 0, 171, 0, 0, 0, 172, 0, 0, 0, 173, 0, 0, 0, 174, 0, 0, 0, 175, 0, 0, 0, 176, 0, 0, 0, 177, 0, 0, 0, 178, 0, 0, 0, 179, 0, 0, 0, 180, 0, 0, 0, 181, 0, 0, 0, 182, 0, 0, 0, 183, 0, 0, 0, 184, 0, 0, 0, 185, 0, 0, 0, 186, 0, 0, 0, 187, 0, 0, 0, 188, 0, 0, 0, 189, 0, 0, 0, 190, 0, 0, 0, 191, 0, 0, 0, 192, 0, 0, 0, 193, 0, 0, 0, 194, 0, 0, 0, 195, 0, 0, 0, 196, 0, 0, 0, 197, 0, 0, 0, 198, 0, 0, 0, 199, 0, 0, 0, 200, 0, 0, 0, 201, 0, 0, 0, 202, 0, 0, 0, 203, 0, 0, 0, 204, 0, 0, 0, 205, 0, 0, 0, 206, 0, 0, 0, 207, 0, 0, 0, 208, 0, 0, 0, 209, 0, 0, 0, 210, 0, 0, 0, 211, 0, 0, 0, 212, 0, 0, 0, 213, 0, 0, 0, 214, 0, 0, 0, 215, 0, 0, 0, 216, 0, 0, 0, 217, 0, 0, 0, 218, 0, 0, 0, 219, 0, 0, 0, 220, 0, 0, 0, 221, 0, 0, 0, 222, 0, 0, 0, 223, 0, 0, 0, 224, 0, 0, 0, 225, 0, 0, 0, 226, 0, 0, 0, 227, 0, 0, 0, 228, 0, 0, 0, 229, 0, 0, 0, 230, 0, 0, 0, 231, 0, 0, 0, 232, 0, 0, 0, 233, 0, 0, 0, 234, 0, 0, 0, 235, 0, 0, 0, 236, 0, 0, 0, 237, 0, 0, 0, 238, 0, 0, 0, 239, 0, 0, 0, 240, 0, 0, 0, 241, 0, 0, 0, 242, 0, 0, 0, 243, 0, 0, 0, 244, 0, 0, 0, 245, 0, 0, 0, 246, 0, 0, 0, 247, 0, 0, 0, 248, 0, 0, 0, 249, 0, 0, 0, 250, 0, 0, 0, 251, 0, 0, 0, 252, 0, 0, 0, 253, 0, 0, 0, 254, 0, 0, 0, 255 };

            StreamToken function = new StreamToken(dictionaryToken, data);

            var func = PdfFunctionParser.Create(function, testPdfTokenScanner, testFilterProvider);
            Assert.Equal(FunctionTypes.Sampled, func.FunctionType);
            var function0 = func as PdfFunctionType0;

            var result = function0.Eval(new double[] { 0 });
            Assert.Equal(4, result.Length);
            result = function0.Eval(new double[] { 0.5 });
            Assert.Equal(4, result.Length);
            result = function0.Eval(new double[] { 1 });
            Assert.Equal(4, result.Length);
            result = function0.Eval(new double[] { 0.2 });
            Assert.Equal(4, result.Length);
        }

        [Fact]
        public void Simple16()
        {
            DictionaryToken dictionaryToken = new DictionaryToken(new Dictionary<NameToken, IToken>()
            {
                { NameToken.FunctionType, new NumericToken(0) },
                { NameToken.Domain, GetArrayToken(0, 1) },
                { NameToken.Range, GetArrayToken(0, 1) },

                { NameToken.BitsPerSample, new NumericToken(16) },
                { NameToken.Size, GetArrayToken(5) }
            });

            byte[] data = new ushort[] { 0, 8192, 16384, 32768, 65535 }.SelectMany(v => BitConverter.GetBytes(v)).ToArray();

            StreamToken function = new StreamToken(dictionaryToken, data);

            var func = PdfFunctionParser.Create(function, testPdfTokenScanner, testFilterProvider);
            Assert.Equal(FunctionTypes.Sampled, func.FunctionType);
            var function0 = func as PdfFunctionType0;

            var result = function0.Eval(new double[] { 0.00 });
            Assert.Single(result);
            Assert.Equal(0.0, result[0], 3);

            result = function0.Eval(new double[] { 0.25 });
            Assert.Single(result);
            Assert.Equal(0.125, result[0], 3);

            result = function0.Eval(new double[] { 0.50 });
            Assert.Single(result);
            Assert.Equal(0.25, result[0], 2);

            result = function0.Eval(new double[] { 0.75 });
            Assert.Single(result);
            Assert.Equal(0.50, result[0], 2);

            result = function0.Eval(new double[] { 1.0 });
            Assert.Single(result);
            Assert.Equal(1.00, result[0], 2);
        }

        [Fact]
        public void Simple8()
        {
            DictionaryToken dictionaryToken = new DictionaryToken(new Dictionary<NameToken, IToken>()
            {
                { NameToken.FunctionType, new NumericToken(0) },
                { NameToken.Domain, GetArrayToken(0, 1) },
                { NameToken.Range, GetArrayToken(0, 1) },

                { NameToken.BitsPerSample, new NumericToken(8) },
                { NameToken.Size, GetArrayToken(5) }
            });

            byte[] data = new byte[] { 0, 32, 64, 128, 255 };

            StreamToken function = new StreamToken(dictionaryToken, data);

            var func = PdfFunctionParser.Create(function, testPdfTokenScanner, testFilterProvider);
            Assert.Equal(FunctionTypes.Sampled, func.FunctionType);
            var function0 = func as PdfFunctionType0;

            var result = function0.Eval(new double[] { 0.00 });
            Assert.Single(result);
            Assert.Equal(0.0, result[0], 3);

            result = function0.Eval(new double[] { 0.25 });
            Assert.Single(result);
            Assert.Equal(0.125, result[0], 3);

            result = function0.Eval(new double[] { 0.50 });
            Assert.Single(result);
            Assert.Equal(0.25, result[0], 2);

            result = function0.Eval(new double[] { 0.75 });
            Assert.Single(result);
            Assert.Equal(0.50, result[0], 2);

            result = function0.Eval(new double[] { 1.0 });
            Assert.Single(result);
            Assert.Equal(1.00, result[0], 2);
        }

        [Fact]
        public void RgbColorSpace()
        {
            DictionaryToken dictionaryToken = new DictionaryToken(new Dictionary<NameToken, IToken>()
            {
                { NameToken.FunctionType, new NumericToken(0) },
                { NameToken.Domain, GetArrayToken(0, 1, 0, 1) },
                { NameToken.Range, GetArrayToken(0, 1, 0, 1, 0, 1) },

                { NameToken.BitsPerSample, new NumericToken(8) },
                { NameToken.Size, GetArrayToken(2, 2) }
            });

            byte[] data = new byte[] { 255, 255, 0, 0, 0, 0, 255, 0, 0, 0, 0, 255 };

            StreamToken function = new StreamToken(dictionaryToken, data);

            var func = PdfFunctionParser.Create(function, testPdfTokenScanner, testFilterProvider);
            Assert.Equal(FunctionTypes.Sampled, func.FunctionType);
            var function0 = func as PdfFunctionType0;

            var result = function0.Eval(new double[] { 0, 0 });
            Assert.Equal(3, result.Length);
            Assert.Equal(new double[] { 1, 1, 0 }, result); // yellow

            result = function0.Eval(new double[] { 1, 0 });
            Assert.Equal(3, result.Length);
            Assert.Equal(new double[] { 0, 0, 0 }, result); // black

            result = function0.Eval(new double[] { 0, 1 });
            Assert.Equal(3, result.Length);
            Assert.Equal(new double[] { 1, 0, 0 }, result); // red

            result = function0.Eval(new double[] { 1, 1 });
            Assert.Equal(3, result.Length);
            Assert.Equal(new double[] { 0, 0, 1 }, result); // blue

            result = function0.Eval(new double[] { 0.5, 0.5 });
            Assert.Equal(3, result.Length);
            Assert.Equal(new double[] { 0.5, 0.25, 0.25 }, result); // Mid point
        }

        [Fact]
        public void RedBlueGradient()
        {
            DictionaryToken dictionaryToken = new DictionaryToken(new Dictionary<NameToken, IToken>()
            {
                { NameToken.FunctionType, new NumericToken(0) },
                { NameToken.Domain, GetArrayToken(0, 1) },
                { NameToken.Range, GetArrayToken(0, 1, 0, 1, 0, 1) },

                { NameToken.BitsPerSample, new NumericToken(8) },
                { NameToken.Size, GetArrayToken(2) }
            });

            byte[] data = new byte[] { 255, 0, 0, 0, 0, 255 };

            StreamToken function = new StreamToken(dictionaryToken, data);

            var func = PdfFunctionParser.Create(function, testPdfTokenScanner, testFilterProvider);
            Assert.Equal(FunctionTypes.Sampled, func.FunctionType);
            var function0 = func as PdfFunctionType0;

            var result = function0.Eval(new double[] { 0 });
            Assert.Equal(3, result.Length);
            Assert.Equal(new double[] { 1, 0, 0 }, result); // red

            result = function0.Eval(new double[] { 1 });
            Assert.Equal(3, result.Length);
            Assert.Equal(new double[] { 0, 0, 1 }, result); // blue

            result = function0.Eval(new double[] { 0.5 });
            Assert.Equal(3, result.Length);
            Assert.Equal(new double[] { 0.5, 0.0, 0.5 }, result); // Mid point

            result = function0.Eval(new double[] { 0.3333 });
            Assert.Equal(3, result.Length);
            Assert.Equal(new double[] { 0.6667, 0.0, 0.3333 }, result); // 1/3 point
        }
    }
}
