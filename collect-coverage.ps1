#!/bin/pwsh
# Coverage collection script for Spamma test modules

Write-Host "Installing ReportGenerator..." -ForegroundColor Cyan
dotnet tool install -g dotnet-reportgenerator-globaltool --version 6.0.0

# Array of test projects
$testProjects = @(
    "tests/Spamma.Modules.UserManagement.Tests",
    "tests/Spamma.Modules.EmailInbox.Tests",
    "tests/Spamma.Modules.DomainManagement.Tests"
)

# Create coverage directory
$coverageDir = "coverage-reports"
New-Item -ItemType Directory -Path $coverageDir -Force | Out-Null
Write-Host "Coverage directory created: $coverageDir" -ForegroundColor Green

# Run tests with coverage for each project
foreach ($project in $testProjects) {
    $projectName = Split-Path -Leaf $project
    Write-Host "`nRunning coverage for $projectName..." -ForegroundColor Yellow
    
    dotnet test $project `
        -c Release `
        --no-build `
        /p:CollectCoverage=true `
        /p:CoverageFormat=opencover `
        /p:CoverageFileName="$projectName.opencover.xml" `
        /p:ExcludeByAttribute="*.ExcludeFromCodeCoverage" `
        /p:DeterministicReport=true `
        2>&1 | Select-String "Passed|Failed|Duration|Tests"
}

Write-Host "`n✅ Coverage collection complete!" -ForegroundColor Green
Write-Host "Coverage files location: $((Get-Item -Path "./tests/*/bin/Release/net9.0/").FullName)" -ForegroundColor Cyan
