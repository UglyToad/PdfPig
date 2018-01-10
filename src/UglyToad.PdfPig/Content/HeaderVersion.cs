namespace UglyToad.PdfPig.Content
{
    internal class HeaderVersion
    {
        public decimal Version { get; }

        public string VersionString { get; }

        public HeaderVersion(decimal version, string versionString)
        {
            Version = version;
            VersionString = versionString;
        }

        public override string ToString()
        {
            return $"Version: {VersionString}";
        }
    }
}