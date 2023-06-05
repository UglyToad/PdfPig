param (
)

$root = (Split-Path -parent $PSCommandPath)

$packageProjectPath = "$root/UglyToad.PdfPig.Package/UglyToad.PdfPig.Package.csproj"
$xml = New-Object XML
$xml.Load($packageProjectPath)
$xml.Project.PropertyGroup[0].PackageId = "PdfPig.Nightly"
$xml.Project.PropertyGroup[0].Title = "PdfPig.Nightly"
$xml.Project.PropertyGroup[0].Product = "PdfPig.Nightly"


$xml.Save($packageProjectPath)