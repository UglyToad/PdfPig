param (
  [Parameter(Position = 0, mandatory = $true)]
  [string]$version,

  [switch]$UpdateAssemblyAndFileVersion
)

$root = (Split-Path -parent $PSCommandPath)

$projs = Get-ChildItem "$root/../src" -Recurse | Where-Object { $_.extension -eq ".csproj" -and $_.name.IndexOf("Tests") -lt 0 }
$projs | ForEach-Object {
  $xml = New-Object XML
  $xml.Load($_.FullName)
  $xml.Project.PropertyGroup.Version = $version
  $xml.Save($_.FullName)
}

$packageProjectPath = "$root/UglyToad.PdfPig.Package/UglyToad.PdfPig.Package.csproj"
$xml = New-Object XML
$xml.Load($packageProjectPath)
$xml.Project.PropertyGroup[0].Version = $version

if ($UpdateAssemblyAndFileVersion) {
    # Update AssemblyVersion and FileVersion if the nodes exist, otherwise create them
    if (-not $xml.Project.PropertyGroup[0].AssemblyVersion) {
        $node = $xml.CreateElement("AssemblyVersion")
        $node.InnerText = "$version.0"  # add the 4th segment
        $xml.Project.PropertyGroup[0].AppendChild($node) | Out-Null
    } else {
        $xml.Project.PropertyGroup[0].AssemblyVersion = "$version.0"
    }

    if (-not $xml.Project.PropertyGroup[0].FileVersion) {
        $node = $xml.CreateElement("FileVersion")
        $node.InnerText = "$version.0"
        $xml.Project.PropertyGroup[0].AppendChild($node) | Out-Null
    } else {
        $xml.Project.PropertyGroup[0].FileVersion = "$version.0"
    }
}

$xml.Save($packageProjectPath)

Write-Host $projs.Length