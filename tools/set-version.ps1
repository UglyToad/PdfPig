param (
  [Parameter(Position = 0, mandatory = $true)]
  [string]$version
)

$root = (Split-Path -parent $PSCommandPath)

$projs = Get-ChildItem "$root/../src" -Recurse | Where-Object { $_.extension -eq ".csproj" -and $_.name.IndexOf("Tests") -lt 0 }
$projs | ForEach-Object {
  $xml = New-Object XML
  $xml.Load($_.FullName)
  $xml.Project.PropertyGroup[0].Version = $version
  $xml.Save($_.FullName)
}

$packageProjectPath = "$root/UglyToad.PdfPig.Package/UglyToad.PdfPig.Package.csproj"
$xml = New-Object XML
$xml.Load($packageProjectPath)
$xml.Project.PropertyGroup[0].Version = $version
$xml.Save($packageProjectPath)

Write-Host $projs.Length