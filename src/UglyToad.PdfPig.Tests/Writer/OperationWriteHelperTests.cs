namespace UglyToad.PdfPig.Tests.Writer
{
    using System.Globalization;
    using UglyToad.PdfPig.Graphics.Operations;

    public class OperationWriteHelperTests
    {
        [Fact]
        public void WriteDouble0()
        {
            using (var memStream = new MemoryStream())
            {
                OperationWriteHelper.WriteDouble(memStream, 0);

                // Read Test
                memStream.Position = 0;
                using (var streamReader = new StreamReader(memStream))
                {
                    var line = streamReader.ReadToEnd();
                    Assert.Equal("0", line);
                }
            }
        }

        [Fact]
        public void WriteDouble5()
        {
            using (var memStream = new MemoryStream())
            {
                OperationWriteHelper.WriteDouble(memStream, 5);

                // Read Test
                memStream.Position = 0;
                using (var streamReader = new StreamReader(memStream))
                {
                    var line = streamReader.ReadToEnd();
                    Assert.Equal("5", line);
                }
            }
        }

        [Fact]
        public void WriteDoubleMinus5()
        {
            using (var memStream = new MemoryStream())
            {
                OperationWriteHelper.WriteDouble(memStream, -5);

                // Read Test
                memStream.Position = 0;
                using (var streamReader = new StreamReader(memStream))
                {
                    var line = streamReader.ReadToEnd();
                    Assert.Equal("-5", line);
                }
            }
        }

        [Fact]
        public void WriteDouble10()
        {
            using (var memStream = new MemoryStream())
            {
                OperationWriteHelper.WriteDouble(memStream, 10);

                // Read Test
                memStream.Position = 0;
                using (var streamReader = new StreamReader(memStream))
                {
                    var line = streamReader.ReadToEnd();
                    Assert.Equal("10", line);
                }
            }
        }

        [Fact]
        public void WriteDoubleMinus10()
        {
            using (var memStream = new MemoryStream())
            {
                OperationWriteHelper.WriteDouble(memStream, -10);

                // Read Test
                memStream.Position = 0;
                using (var streamReader = new StreamReader(memStream))
                {
                    var line = streamReader.ReadToEnd();
                    Assert.Equal("-10", line);
                }
            }
        }

        [Fact]
        public void WriteDouble1()
        {
            using (var memStream = new MemoryStream())
            {
                OperationWriteHelper.WriteDouble(memStream, 0.00000001);

                // Read Test
                memStream.Position = 0;
                using (var streamReader = new StreamReader(memStream))
                {
                    var line = streamReader.ReadToEnd();
                    Assert.Equal("0.00000001", line);
                }
            }
        }

        [Fact]
        public void WriteDouble1bis()
        {
            using (var memStream = new MemoryStream())
            {
                OperationWriteHelper.WriteDouble(memStream, -0.00000001);

                // Read Test
                memStream.Position = 0;
                using (var streamReader = new StreamReader(memStream))
                {
                    var line = streamReader.ReadToEnd();
                    Assert.Equal("-0.00000001", line);
                }
            }
        }

        [Fact]
        public void WriteDouble2() 
        {
            using (var memStream = new MemoryStream())
            {
                OperationWriteHelper.WriteDouble(memStream, .00000005100);

                // Read Test
                memStream.Position = 0;
                using (var streamReader = new StreamReader(memStream))
                {
                    var line = streamReader.ReadToEnd();
                    Assert.Equal("0.000000051", line);
                }
            }
        }

        [Fact]
        public void WriteDouble2bis()
        {
            using (var memStream = new MemoryStream())
            {
                OperationWriteHelper.WriteDouble(memStream, -.0000000510);

                // Read Test
                memStream.Position = 0;
                using (var streamReader = new StreamReader(memStream))
                {
                    var line = streamReader.ReadToEnd();
                    Assert.Equal("-0.000000051", line);
                }
            }
        }

        [Fact]
        public void WriteDouble3()
        {
            using (var memStream = new MemoryStream())
            {
                OperationWriteHelper.WriteDouble(memStream, 15001.98);

                // Read Test
                memStream.Position = 0;
                using (var streamReader = new StreamReader(memStream))
                {
                    var line = streamReader.ReadToEnd();
                    var v = double.Parse(line, CultureInfo.InvariantCulture);
                    Assert.Equal(15001.98, v);
                }
            }
        }

        [Fact]
        public void WriteDouble4()
        {
            using (var memStream = new MemoryStream())
            {
                OperationWriteHelper.WriteDouble(memStream, 10000.000);

                // Read Test
                memStream.Position = 0;
                using (var streamReader = new StreamReader(memStream))
                {
                    var line = streamReader.ReadToEnd();
                    Assert.Equal("10000", line);
                }
            }
        }

#if NET
        // See here why we are not running on framework - thanks @cremor
        // https://stackoverflow.com/a/1658420/631802
        [Fact]
        public void WriteMinValue()
        {
            string expected = "-340282346638528859811704183484516925440";
            using (var memStream = new MemoryStream())
            {
                OperationWriteHelper.WriteDouble(memStream, -340282346638528859811704183484516925440d);

                // Read Test
                memStream.Position = 0;
                using (var streamReader = new StreamReader(memStream))
                {
                    var line = streamReader.ReadToEnd();
                    Assert.Equal(expected, line);
                }
            }
        }

        [Fact]
        public void WriteMaxValue()
        {
            string expected = "340282346638528859811704183484516925440";
            using (var memStream = new MemoryStream())
            {
                OperationWriteHelper.WriteDouble(memStream, 340282346638528859811704183484516925440d);

                // Read Test
                memStream.Position = 0;
                using (var streamReader = new StreamReader(memStream))
                {
                    var line = streamReader.ReadToEnd();
                    Assert.Equal(expected, line);
                }
            }
        }
#endif
    }
}
