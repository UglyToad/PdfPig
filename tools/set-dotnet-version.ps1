param (
  [Parameter(Position = 0, mandatory = $true)]
  [string]$version
)

$root = (Split-Path -parent $PSCommandPath)

$projs = Get-ChildItem "$root/../src" -Recurse | Where-Object { $_.extension -eq ".csproj" }
$projs | ForEach-Object {
  $xml = New-Object XML
  $xml.Load($_.FullName)
  if($xml.Project.PropertyGroup[0].TargetFrameworks) {
     $xml.Project.PropertyGroup[0].TargetFrameworks = $version
  } else {
      if ($xml.Project.PropertyGroup.TargetFrameworks) {
        $xml.Project.PropertyGroup.TargetFrameworks = $version
      }
  }

  $xml.Save($_.FullName)
}

Write-Host "Updated dotnet version to $version for all projects."