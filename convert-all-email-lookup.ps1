# Comprehensive Email Lookup converter - handles all patterns

function Convert-ToFactoryMethod {
    param($Content)
    
    # Pattern 1: Without DomainId (substitue Guid.Empty)
    $pattern1 = '(?ms)new EmailLookup\s*\{\s*Id\s*=\s*([^,]+),\s*SubdomainId\s*=\s*([^,]+),\s*Subject\s*=\s*([^,]+),\s*SentAt\s*=\s*([^,]+),\s*(?:IsFavorite\s*=\s*([^,]+),\s*)?(?:CampaignId\s*=\s*([^,]+),\s*)?(?:DeletedAt\s*=\s*([^,]+),\s*)?EmailAddresses\s*=\s*(new List<EmailAddress>\s*\{[^\}]+\}),?\s*\}'
    $replacement1 = {
        $id = $matches[1]
        $subdomainId = $matches[2]
        $subject = $matches[3]
        $sentAt = $matches[4]
        $isFavorite = if ($matches[5]) { $matches[5] } else { "false" }
        $campaignId = if ($matches[6]) { $matches[6] } else { "null" }
        $deletedAt = if ($matches[7]) { $matches[7] } else { "null" }
        $emailAddresses = $matches[8]
        
        "EmailLookup.CreateForTesting(id: $id, subdomainId: $subdomainId, domainId: Guid.Empty, subject: $subject, sentAt: $sentAt, isFavorite: $isFavorite, emailAddresses: $emailAddresses, deletedAt: $deletedAt, campaignId: $campaignId)"
    }
    $Content = [regex]::Replace($Content, $pattern1, $replacement1)
    
    # Pattern 2: With DomainId
    $pattern2 = '(?ms)new EmailLookup\s*\{\s*Id\s*=\s*([^,]+),\s*(?:Subject\s*=\s*([^,]+),\s*)?DomainId\s*=\s*([^,]+),\s*SubdomainId\s*=\s*([^,]+),\s*(?:Subject\s*=\s*([^,]+),\s*)?SentAt\s*=\s*([^,]+),\s*(?:IsFavorite\s*=\s*([^,]+),\s*)?(?:CampaignId\s*=\s*([^,]+),\s*)?(?:DeletedAt\s*=\s*([^,]+),\s*)?EmailAddresses\s*=\s*(new List<EmailAddress>\s*\{[^\}]+\}),?\s*\}'
    $replacement2 = {
        $id = $matches[1]
        $subject = if ($matches[2]) { $matches[2] } else { $matches[5] }
        $domainId = $matches[3]
        $subdomainId = $matches[4]
        $sentAt = $matches[6]
        $isFavorite = if ($matches[7]) { $matches[7] } else { "false" }
        $campaignId = if ($matches[8]) { $matches[8] } else { "null" }
        $deletedAt = if ($matches[9]) { $matches[9] } else { "null" }
        $emailAddresses = $matches[10]
        
        "EmailLookup.CreateForTesting(id: $id, subdomainId: $subdomainId, domainId: $domainId, subject: $subject, sentAt: $sentAt, isFavorite: $isFavorite, emailAddresses: $emailAddresses, deletedAt: $deletedAt, campaignId: $campaignId)"
    }
    $Content = [regex]::Replace($Content, $pattern2, $replacement2)
    
    return $Content
}

$files = @(
    "tests\Spamma.Modules.EmailInbox.Tests\Integration\SearchEmailsQueryProcessorTests.cs",
    "tests\Spamma.Modules.EmailInbox.Tests\Integration\GetEmailByIdQueryProcessorTests.cs",
    "tests\Spamma.Modules.EmailInbox.Tests\Integration\GetCampaignDetailQueryProcessorTests.cs",
    "tests\Spamma.Modules.EmailInbox.Tests\Integration\QueryProcessors\SearchEmailsQueryProcessorTests.cs",
    "tests\Spamma.Modules.EmailInbox.Tests\Integration\QueryProcessors\GetEmailByIdQueryProcessorTests.cs",
    "tests\Spamma.Modules.EmailInbox.Tests\Integration\QueryProcessors\GetCampaignDetailQueryProcessorTests.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw -Encoding UTF8
        $newContent = Convert-ToFactoryMethod -Content $content
        
        if ($content -ne $newContent) {
            Set-Content -Path $file -Value $newContent -Encoding UTF8 -NoNewline
            Write-Host "Converted: $file" -ForegroundColor Green
        } else {
            Write-Host "No changes: $file" -ForegroundColor Yellow
        }
    } else {
        Write-Warning "File not found: $file"
    }
}
