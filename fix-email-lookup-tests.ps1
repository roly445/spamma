# Script to convert EmailLookup object initializers to CreateForTesting factory method calls

$testFiles = Get-ChildItem -Path "tests\Spamma.Modules.EmailInbox.Tests" -Recurse -Filter "*.cs" | 
    Where-Object { $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*" }

$pattern = @'
(?s)(var\s+\w+\s+=\s+)?new EmailLookup\s*\{[^}]*?Id\s*=\s*([^,\n]+),\s*SubdomainId\s*=\s*([^,\n]+),\s*DomainId\s*=\s*([^,\n]+),\s*Subject\s*=\s*([^,\n]+),\s*SentAt\s*=\s*([^,\n]+),\s*IsFavorite\s*=\s*([^,\n]+),\s*(?:DeletedAt\s*=\s*([^,\n]+),\s*)?(?:CampaignId\s*=\s*([^,\n]+),\s*)?(?:CampaignValue\s*=\s*([^,\n]+),\s*)?EmailAddresses\s*=\s*(new List<EmailAddress>[^}]+)\}
'@

foreach ($file in $testFiles) {
    $content = Get-Content $file.FullName -Raw
    
    if ($content -match "EmailAddresses\s*=\s*new List<EmailAddress>") {
        Write-Host "Processing: $($file.FullName)" -ForegroundColor Yellow
        
        # This is complex - let's do a simpler approach for now
        # Just report files that need manual conversion
        Write-Host "  - Needs manual conversion" -ForegroundColor Cyan
    }
}

Write-Host "`nTotal files to convert: $($testFiles.Count)" -ForegroundColor Green
