#!/usr/bin/env pwsh
# Script to remove XML documentation comments from non-partial classes
# Keeps XML comments only on partial classes as requested

$ErrorActionPreference = "Stop"

# Get all C# files in src directory (excluding bin/obj)
$files = Get-ChildItem -Recurse -Path src -Include *.cs | 
    Where-Object { $_.FullName -notlike '*\obj\*' -and $_.FullName -notlike '*\bin\*' }

Write-Host "Processing $($files.Count) C# files..." -ForegroundColor Cyan

$totalFilesModified = 0
$totalCommentsRemoved = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    
    # Check if file contains 'partial class' or 'partial record'
    $isPartialClass = $content -match '\bpartial\s+(class|record|struct|interface)\b'
    
    if ($isPartialClass) {
        Write-Host "  Skipping $($file.Name) (contains partial class/record)" -ForegroundColor Yellow
        continue
    }
    
    # Remove XML doc comments (/// lines including summary tags)
    # Pattern: Match one or more lines starting with /// (with optional whitespace before)
    $pattern = '(?m)^\s*///.*?$\r?\n'
    
    $matchCount = ([regex]::Matches($content, $pattern)).Count
    
    if ($matchCount -gt 0) {
        # Simply remove XML comment lines entirely
        $content = $content -replace $pattern, ''
        
        # Fix SA1516: Ensure proper spacing between namespace and type declarations/attributes
        # Add blank line between namespace; and public/internal/attribute declarations
        $content = $content -replace '(?m)(namespace\s+[^;]+;\r?\n)(public|internal|private|\[)', "`$1`n`$2"
        
        # Fix SA1505: Remove blank line after opening brace
        # Pattern: Opening brace followed by 2+ newlines
        $content = $content -replace '(?m)\{\r?\n\r?\n+', "{`n"
        
        # Fix SA1513 + SA1516: Ensure blank line after closing brace (before next element)
        $content = $content -replace '(?m)(\}\r?\n)(public|internal|private|protected|\[)', "`$1`n`$2"
        
        # Fix SA1516: Ensure blank line between properties
        $content = $content -replace '(?m)(\}\r?\n)(    public)', "`$1`n`$2"
        
        # Clean up 3+ consecutive newlines down to 2
        $content = $content -replace '(?m)(\r?\n){3,}', "`n`n"
        
        Set-Content -Path $file.FullName -Value $content -NoNewline
        
        $totalFilesModified++
        $totalCommentsRemoved += $matchCount
        Write-Host "  ✓ $($file.Name): Removed $matchCount XML comment blocks" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Files modified: $totalFilesModified" -ForegroundColor Green
Write-Host "  XML comment blocks removed: $totalCommentsRemoved" -ForegroundColor Green
Write-Host ""
Write-Host "Done! Run 'dotnet build' to verify changes." -ForegroundColor Cyan
