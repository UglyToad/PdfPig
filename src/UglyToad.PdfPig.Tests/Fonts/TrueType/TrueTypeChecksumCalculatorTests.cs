namespace UglyToad.PdfPig.Tests.Fonts.TrueType
{
    using System;
    using System.IO;
    using System.Linq;
    using PdfPig.Core;
    using PdfPig.Fonts.TrueType;
    using PdfPig.Fonts.TrueType.Parser;
    using Xunit;

    public class TrueTypeChecksumCalculatorTests
    {
        [Fact]
        public void CalculatedChecksumsMatchRoboto()
        {
            // Both checksums are wrong in the file.
            Run(GetFileBytes("Roboto-Regular.ttf"), false, false);
        }

        [Fact]
        public void CalculatedChecksumsMatchAndada()
        {
            Run(GetFileBytes("Andada-Regular.ttf"), true, true);
        }

        [Fact]
        public void CalculatedChecksumsMatchGoogleDoc()
        {
            // Checksum adjustment is wrong.
            Run(GetFileBytes("google-simple-doc.ttf"), true, false);
        }

        [Fact]
        public void CalculatedChecksumsMatchPMing()
        {
            // Checksum adjustment is wrong.
            Run(GetFileBytes("PMingLiU.ttf"), true, false);
        }

        [Fact]
        public void CalculatedChecksumsMatchCalibriWindows()
        {
            const string path = @"C:\Windows\Fonts\Calibri.ttf";
            if (!File.Exists(path))
            {
                return;
            }

            Run(File.ReadAllBytes(path), true, true);
        }

        [Fact]
        public void CalculatedChecksumsMatchCourierNewWindows()
        {
            const string path = @"C:\Windows\Fonts\cour.ttf";
            if (!File.Exists(path))
            {
                return;
            }

            Run(File.ReadAllBytes(path), true, true);
        }

        private void Run(byte[] bytes, bool checkHeaderChecksum, bool checkWholeFileChecksum)
        {
            var inputBytes = new ByteArrayInputBytes(bytes);

            var font = TrueTypeFontParser.Parse(new TrueTypeDataBytes(inputBytes));

            inputBytes = new ByteArrayInputBytes(bytes);

            foreach (var header in font.TableHeaders)
            {
                // Acts as the whole table checksum
                if (header.Key == "head")
                {
                    if (checkHeaderChecksum)
                    {
                        var headerChecksum = TrueTypeChecksumCalculator.Calculate(inputBytes, header.Value);

                        Assert.Equal(header.Value.CheckSum, headerChecksum);
                    }

                    continue;
                }

                var input = bytes.Skip((int)header.Value.Offset).Take((int)header.Value.Length);

                var checksum = TrueTypeChecksumCalculator.Calculate(input);

                Assert.Equal(header.Value.CheckSum, checksum);

                var checksumByTable = TrueTypeChecksumCalculator.Calculate(inputBytes, header.Value);

                Assert.Equal(header.Value.CheckSum, checksumByTable);
            }

            if (checkWholeFileChecksum)
            {
                var headerActual = font.TableHeaders["head"];
                var wholeFontChecksum = TrueTypeChecksumCalculator.CalculateWholeFontChecksum(inputBytes, headerActual);
                var adjustment = 0xB1B0AFBA - wholeFontChecksum;
                var adjustmentRecorded = font.TableRegister.HeaderTable.CheckSumAdjustment;

                Assert.Equal(adjustmentRecorded, adjustment);

                var expectedWholeFontChecksum = 0xB1B0AFBA - adjustmentRecorded;

                Assert.Equal(expectedWholeFontChecksum, wholeFontChecksum);
            }
        }

        private static byte[] GetFileBytes(string name)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "TrueType");

            name = name.EndsWith(".ttf") || name.EndsWith(".txt") ? name : name + ".ttf";

            var file = Path.Combine(path, name);

            return File.ReadAllBytes(file);
        }
    }
}
