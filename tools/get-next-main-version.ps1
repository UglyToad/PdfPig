$xml = New-Object XML
$projectPath = Join-Path $PSScriptRoot "UglyToad.PdfPig.Package\UglyToad.PdfPig.Package.csproj"
$xml.Load($projectPath)
$current = $xml.Project.PropertyGroup[0].Version
$hyphenIndex = $current.IndexOf('-')
$len = If ($hyphenIndex -lt 0) { $current.Length } Else { $hyphenIndex }
$version = $current.Substring(0, $len)

# Split into parts
$parts = $version.Split('.')

# Increment last part (patch)
$patch = [int]$parts[-1]
$patch++

# Build new version string
$parts[-1] = $patch.ToString()
$newVersion = $parts -join '.'

Write-Output $newVersion