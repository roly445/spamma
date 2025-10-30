$files = @(
    'src/modules/Spamma.Modules.DomainManagement/Application/Validators/CreateDomainCommandValidator.cs',
    'src/modules/Spamma.Modules.DomainManagement/Infrastructure/Services/DomainParserService.cs',
    'src/modules/Spamma.Modules.DomainManagement/Infrastructure/Services/DomainParserInitializationService.cs',
    'src/modules/Spamma.Modules.DomainManagement/Infrastructure/Services/IDomainParserService.cs',
    'tests/Spamma.Modules.DomainManagement.Tests/Application/Validators/CreateDomainCommandValidatorTests.cs'
)

foreach ($file in $files) {
    $text = [System.IO.File]::ReadAllText($file)
    $text = $text.TrimEnd()
    [System.IO.File]::WriteAllText($file, $text + "`r`n")
    Write-Host "Fixed EOF for $file"
}
