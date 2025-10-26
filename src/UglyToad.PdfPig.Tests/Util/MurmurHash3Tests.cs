namespace UglyToad.PdfPig.Tests.Util
{
    using PdfPig.Util;
    using System.Text;

    public class MurmurHash3Tests
    {
        public static object[][] MurmurHashData = new object[][]
        {
            // https://murmurhash.shorelabs.com/
            ["The quick brown fox jumps over the lazy dog", "2f1583c3ecee2c675d7bf66ce5e91d2c", "e34bbc7bbc071b6c7a433ca9c49a9347"],
            ["MurmurHash3 was written by Austin Appleby, and is placed in the public", "6d3583489d9d1e5a898493af67e2ad10", "a91793d43f82cbabda2fb0c28c24799a"],
            ["0", "0ab2409ea5eb34f8a5eb34f8a5eb34f8", "2ac9debed546a3803a8de9e53c875e09"],
        };

        [Theory]
        [MemberData(nameof(MurmurHashData))]
        public void x86x64Check(string sentence, string expectedX86, string expectedX64)
        {
            byte[] data = Encoding.UTF8.GetBytes(sentence);
            
            var hash = MurmurHash3.Compute_x86_128(data, data.Length, 0);
            var actual = string.Concat(Array.ConvertAll(hash, x => x.ToString("x2")));
            Assert.Equal(expectedX86, actual);

            hash = MurmurHash3.Compute_x64_128(data, data.Length, 0);
            actual = string.Concat(Array.ConvertAll(hash, x => x.ToString("x2")));
            Assert.Equal(expectedX64, actual);
        }
    }
}
