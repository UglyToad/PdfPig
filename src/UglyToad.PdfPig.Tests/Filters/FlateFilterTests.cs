namespace UglyToad.PdfPig.Tests.Filters
{
    using System.Text;
    using PdfPig.Core;
    using PdfPig.Filters;
    using PdfPig.Tokens;

    public class FlateFilterTests
    {
        private readonly FlateFilter filter = new FlateFilter();

        [Fact]
        public void EncodeAndDecodePreservesInput()
        {
            var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>());
            var input = new byte[] { 67, 69, 69, 10, 4, 20, 6, 19, 120, 64, 64, 64, 32 };

            using (var inputStream = new MemoryStream(input))
            {
                inputStream.Seek(0, SeekOrigin.Begin);
                var result = filter.Encode(inputStream, parameters);
                var decoded = filter.Decode(result, parameters, TestFilterProvider.Instance, 0);
                Assert.Equal(input, decoded.ToArray());
            }
        }

        [Fact]
        public void CanDecodeCorruptedInputIssue1235()
        {
            const string hexStr =
                "789C958D5D0AC2400C844FB077980B74BB7FD9D982F820B43E8B7B03C542C187EAFDC1F84B7D1164200999E49BD9044C6653D10E1E443DA1AF6636ED76EF315E7572968E1ECDAB7FB7506C4C59C0AEB3912EE270366AAAF4E36D364BF7911450DC274A5112B1AC9751D77A58680B51A4D8AE433D62953C037396E0F290FBE098B267A43051725AA34E77E44EF50B1B52B42C90E4ADF83FB94FDD0000000000";

            var hex = new HexToken(hexStr.AsSpan());

            var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>());

            var result = filter.Decode(hex.Bytes.ToArray(), parameters, TestFilterProvider.Instance, 0);

            var text = OtherEncodings.BytesAsLatin1String(result.ToArray());

            Assert.StartsWith("q", text);
        }

        [Fact]
        public void DataThatIsNoDeflateStreamDoesNotComeBackAsThoughItWereDecoded()
        {
            var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>());
            var input = OtherEncodings.StringAsLatin1Bytes("This is not a deflate stream.");

            var decoded = filter.Decode(input, parameters, TestFilterProvider.Instance, 0);

            // The input used to be handed straight back, which a caller cannot tell apart
            // from content that happens to look like that.
            Assert.NotEqual(input, decoded.ToArray());
        }

        [Fact]
        public void TruncatedStreamKeepsWhatInflated()
        {
            var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>());

            // Comfortably more than one deflate block: the inflater of .NET Framework
            // hands over completed blocks only, so a stream small enough to be a single
            // block yields nothing there while .NET yields a prefix of it.
            var content = new StringBuilder();
            for (var i = 0; content.Length < 200000; i++)
            {
                content.Append("Line ").Append(i).Append(" of a document spanning several blocks.\n");
            }

            var original = OtherEncodings.StringAsLatin1Bytes(content.ToString());

            byte[] compressed;
            using (var inputStream = new MemoryStream(original))
            {
                compressed = filter.Encode(inputStream, parameters);
            }

            var half = new byte[compressed.Length / 2];
            Array.Copy(compressed, half, half.Length);

            var decoded = filter.Decode(half, parameters, TestFilterProvider.Instance, 0).ToArray();

            Assert.NotEmpty(decoded);
            Assert.True(decoded.Length < original.Length, "a truncated stream cannot yield everything");
            Assert.Equal(original.AsSpan(0, decoded.Length).ToArray(), decoded);
        }

        [Fact]
        public void DamagedStreamKeepsWhatInflatedBeforeTheDamage()
        {
            var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>());

            var content = new StringBuilder();
            for (var i = 0; content.Length < 400_000; i++)
            {
                content.Append("Line ").Append(i).Append(" of a document whose compressed form is damaged part way.\n");
            }

            var original = OtherEncodings.StringAsLatin1Bytes(content.ToString());

            byte[] compressed;
            using (var inputStream = new MemoryStream(original))
            {
                compressed = filter.Encode(inputStream, parameters);
            }

            // Damage about three fifths of the way in. Uniform text inflates evenly, so the
            // first half of the output lies well before the damage and has to come out intact.
            // The inflater notices damage a little after meeting it, so the bytes between the
            // damage and the failure are noise, as they are with every inflater.
            var damage = (compressed.Length * 3) / 5;
            compressed[damage] ^= 0xFF;
            compressed[damage + 1] ^= 0xFF;

            var decoded = filter.Decode(compressed, parameters, TestFilterProvider.Instance, 0).ToArray();
            var half = original.Length / 2;

            Assert.True(decoded.Length < original.Length, "a damaged stream cannot yield everything");

#if NET
            // Read many kilobytes at a time, the read that meets the damage would take its
            // output down with it and the first half would not survive; the filter inflates a
            // damaged stream again in small reads so that it does. The inflater of .NET
            // Framework hands over whole deflate blocks only, so the bound cannot hold there.
            Assert.True(decoded.Length >= half, $"only {decoded.Length} of {original.Length} bytes survived the damage");
#endif

            var intact = Math.Min(half, decoded.Length);
            Assert.Equal(original.AsSpan(0, intact).ToArray(), decoded.AsSpan(0, intact).ToArray());
        }

        [Fact]
        public void AFailureYieldsNothingRatherThanTheInput()
        {
            // A predictor row too wide for any buffer to hold. Nothing here decodes, and the
            // input must not come back dressed as content.
            // The resolver only hands the parameters over when a filter is named.
            var parameters = FilterTestHelpers.StreamDictionary(NameToken.FlateDecode, [(NameToken.Predictor, 12), (NameToken.Colors, 32), (NameToken.BitsPerComponent, 16), (NameToken.Columns, int.MaxValue)]);

            byte[] compressed;
            using (var inputStream = new MemoryStream(OtherEncodings.StringAsLatin1Bytes("content")))
            {
                compressed = filter.Encode(inputStream, new DictionaryToken(new Dictionary<NameToken, IToken>()));
            }

            var decoded = filter.Decode(compressed, parameters, TestFilterProvider.Instance, 0);

            Assert.Empty(decoded.ToArray());
        }

        [Fact]
        public void ADamagedImageStreamKeepsTheRowsBeforeTheDamage()
        {
            // A stream with a /Height is inflated row by row, straight into the result, while one
            // without is inflated first and decoded afterwards. Both take the second way again once
            // the data turns out damaged, so they have to agree, and both have to keep the rows that
            // inflated before the damage. The stream is built of stored deflate blocks of twenty rows,
            // so that the damage, a block length whose complement no longer matches, is found at the
            // same place by every inflater.
            const int columns = 200;
            const int rows = 400;
            const int stride = columns + 1;
            const int rowsPerBlock = 20;
            const int damagedBlock = 12;
            const int blockLength = rowsPerBlock * stride;

            var original = FilterTestHelpers.RandomRows(new Random(7), rows * stride, 12, stride);
            var compressed = StoredZlibStream(original, blockLength);

            var decodeParameters = new[] { (NameToken.Predictor, 12), (NameToken.Columns, columns) };
            var image = FilterTestHelpers.StreamDictionary(NameToken.FlateDecode, decodeParameters, (NameToken.Height, rows));
            var plain = FilterTestHelpers.StreamDictionary(NameToken.FlateDecode, decodeParameters);

            var expected = FilterTestHelpers.DecodedAtOnce(original, 12, 1, 8, columns);

            Assert.Equal(expected, filter.Decode(compressed, image, TestFilterProvider.Instance, 0).ToArray());

            // The low byte of the length complement in the header of the damaged block.
            compressed[2 + (damagedBlock * (5 + blockLength)) + 3] ^= 0xFF;

            var decodedImage = filter.Decode(compressed, image, TestFilterProvider.Instance, 0).ToArray();
            var decodedPlain = filter.Decode(compressed, plain, TestFilterProvider.Instance, 0).ToArray();

            Assert.Equal(decodedPlain, decodedImage);

            // The read that meets the damage takes its output down with it: a salvage read of 4096
            // bytes, on top of the 8 KB of input the inflater may have taken in ahead of it. So a few
            // dozen rows before the damage may be lost, but nothing before those.
            var intact = damagedBlock * rowsPerBlock * columns;
            var lost = (((8192 + 4096) / stride) + 1) * columns;

            Assert.True(decodedImage.Length <= intact, $"{decodedImage.Length} bytes came out of a stream damaged after {intact}");
            Assert.True(decodedImage.Length >= intact - lost, $"only {decodedImage.Length} of the {intact} bytes before the damage survived");
            // The loss may cut the last row short, and its padding then decodes to other bytes.
            var trusted = decodedImage.Length - columns;
            Assert.Equal(expected.AsSpan(0, trusted).ToArray(), decodedImage.AsSpan(0, trusted).ToArray());
        }

        /// <summary>A zlib stream of stored deflate blocks of <paramref name="blockLength"/> bytes each.</summary>
        private static byte[] StoredZlibStream(byte[] data, int blockLength)
        {
            var blocks = (data.Length + blockLength - 1) / blockLength;
            var stream = new byte[2 + (blocks * 5) + data.Length + 4];

            stream[0] = 0x78;
            stream[1] = 0x01;

            var position = 2;

            for (var block = 0; block < blocks; block++)
            {
                var offset = block * blockLength;
                var length = Math.Min(blockLength, data.Length - offset);

                stream[position++] = (byte)(block == blocks - 1 ? 1 : 0);
                stream[position++] = (byte)length;
                stream[position++] = (byte)(length >> 8);
                stream[position++] = (byte)~length;
                stream[position++] = (byte)(~length >> 8);

                Array.Copy(data, offset, stream, position, length);
                position += length;
            }

            uint a = 1;
            uint b = 0;

            foreach (var value in data)
            {
                a = (a + value) % 65521;
                b = (b + a) % 65521;
            }

            stream[position++] = (byte)(b >> 8);
            stream[position++] = (byte)b;
            stream[position++] = (byte)(a >> 8);
            stream[position] = (byte)a;

            return stream;
        }

    }
}
