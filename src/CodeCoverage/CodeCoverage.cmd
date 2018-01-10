@echo off

cd src\CodeCoverage

nuget restore packages.config -PackagesDirectory .

cd ..

CodeCoverage\OpenCover.4.6.519\tools\OpenCover.Console.exe -target:"dotnet.exe" -targetargs:"test UglyToad.PdfPig.Tests\UglyToad.PdfPig.Tests.csproj  --framework netcoreapp2.0 -c debug" -register:user -output:.\test-results.xml -hideskipped:All -returntargetcode -oldStyle -filter:"+[UglyToad.PdfPig*]* -[UglyToad.PdfPig.Tests*]*" 
