$toolsRoot = (Split-Path -parent $PSCommandPath)

$mainVersion = & "$toolsRoot\get-main-version.ps1"
$commitHash = (git rev-parse HEAD).Substring(0, 5)
$date = [System.DateTime]::UtcNow.ToString('yyyyMMdd')
$nightlyVersion = "$mainVersion-alpha.$date.$commitHash"
Write-Output $nightlyVersion