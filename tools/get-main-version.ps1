$xml = New-Object XML
$xml.Load(".\tools\UglyToad.PdfPig.Package\UglyToad.PdfPig.Package.csproj")
$current = $xml.Project.PropertyGroup[0].Version
$hyphenIndex = $current.IndexOf('-')
$len = If ($hyphenIndex -lt 0) { $current.Length } Else { $hyphenIndex }
$version = $current.Substring(0, $len)
Write-Output $version