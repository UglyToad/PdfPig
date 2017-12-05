@echo off

cd src\CodeCoverage

nuget restore packages.config -PackagesDirectory .

cd ..

dotnet restore  UglyToad.Pdf.sln
dotnet build  UglyToad.Pdf.sln --no-incremental -c debug /p:codecov=true
 
rem The -threshold options prevents this taking ages...
CodeCoverage\OpenCover.4.6.519\tools\OpenCover.Console.exe -target:"dotnet.exe" -targetargs:"test UglyToad.Pdf.Tests\UglyToad.Pdf.Tests.csproj --no-build -c debug" -register:user -output:.\test-results.xml -hideskipped:All -returntargetcode -oldStyle -filter:"+[UglyToad.Pdf*]* -[UglyToad.Pdf.Tests*]*" 

if %errorlevel% neq 0 exit /b %errorlevel%
