$xml = New-Object XML
$projectPath = Join-Path $PSScriptRoot "UglyToad.PdfPig.Package\UglyToad.PdfPig.Package.csproj"
$xml.Load($projectPath)
$current = $xml.Project.PropertyGroup[0].Version
$hyphenIndex = $current.IndexOf('-')
$len = If ($hyphenIndex -lt 0) { $current.Length } Else { $hyphenIndex }
$version = $current.Substring(0, $len)
Write-Output $version