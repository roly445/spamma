# PowerShell script to convert EmailLookup object initializers to CreateForTesting calls
# This handles the most common pattern found in the tests

$ErrorActionPreference = "Stop"

$files = @(
    "tests\Spamma.Modules.EmailInbox.Tests\Integration\QueryProcessors\SearchEmailsQueryProcessorTests.cs",
    "tests\Spamma.Modules.EmailInbox.Tests\Integration\QueryProcessors\GetEmailByIdQueryProcessorTests.cs",
    "tests\Spamma.Modules.EmailInbox.Tests\Integration\QueryProcessors\GetCampaignDetailQueryProcessorTests.cs",
    "tests\Spamma.Modules.EmailInbox.Tests\Integration\SearchEmailsQueryProcessorTests.cs",
    "tests\Spamma.Modules.EmailInbox.Tests\Integration\GetEmailByIdQueryProcessorTests.cs",
    "tests\Spamma.Modules.EmailInbox.Tests\Integration\GetCampaignDetailQueryProcessorTests.cs"
)

function Convert-EmailLookupInitializer {
    param (
        [string]$FilePath
    )
    
    if (-not (Test-Path $FilePath)) {
        Write-Warning "File not found: $FilePath"
        return
    }
    
    $content = Get-Content $FilePath -Raw -Encoding UTF8
    $originalContent = $content
    
    # Pattern to match: var email = new EmailLookup { ... }
    # This is a simplified pattern - we'll handle the most common cases
    
    # Replace pattern for the most common case (no DeletedAt, CampaignId, CampaignValue)
    $pattern1 = '(?ms)var (\w+) = new EmailLookup\s*\{\s*' +
                'Id = ([^,]+),\s*' +
                'SubdomainId = ([^,]+),\s*' +
                'DomainId = ([^,]+),\s*' +
                'Subject = ([^,]+),\s*' +
                'SentAt = ([^,]+),\s*' +
                'IsFavorite = ([^,]+),\s*' +
                'EmailAddresses = (new List<EmailAddress>\s*\{[^\}]*\}),?\s*' +
                '\};'
    
    $replacement1 = 'var $1 = EmailLookup.CreateForTesting(' + [Environment]::NewLine +
                    '            id: $2,' + [Environment]::NewLine +
                    '            subdomainId: $3,' + [Environment]::NewLine +
                    '            domainId: $4,' + [Environment]::NewLine +
                    '            subject: $5,' + [Environment]::NewLine +
                    '            sentAt: $6,' + [Environment]::NewLine +
                    '            isFavorite: $7,' + [Environment]::NewLine +
                    '            emailAddresses: $8);'
    
    $content = [regex]::Replace($content, $pattern1, $replacement1)
    
    if ($content -ne $originalContent) {
        Write-Host "Converted: $FilePath" -ForegroundColor Green
        Set-Content -Path $FilePath -Value $content -Encoding UTF8 -NoNewline
        return $true
    }
    else {
        Write-Host "No changes: $FilePath" -ForegroundColor Yellow
        return $false
    }
}

$convertedCount = 0
foreach ($file in $files) {
    if (Convert-EmailLookupInitializer -FilePath $file) {
        $convertedCount++
    }
}

Write-Host "`nTotal files converted: $convertedCount" -ForegroundColor Cyan
