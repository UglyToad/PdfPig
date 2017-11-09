namespace UglyToad.Pdf.Parser.Parts
{
    public class HeaderVersion
    {
        public decimal Version { get; }

        public string VersionString { get; }

        public HeaderVersion(decimal version, string versionString)
        {
            Version = version;
            VersionString = versionString;
        }
    }
}