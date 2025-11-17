#!/usr/bin/env pwsh

# Script to remove XML documentation comments (///) from C# files
# Preserves XML comments on partial classes, structs, records, and interfaces

$rootPath = "c:\Users\andre\Code\spamma"
$csFiles = Get-ChildItem -Path $rootPath -Recurse -Include "*.cs" | Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\" }

$processedCount = 0
$filesChanged = 0

foreach ($file in $csFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    $originalContent = $content
    
    # Check if this file contains a partial class/struct/record/interface declaration
    $isPartialFile = $content -match 'public\s+partial\s+(class|struct|record|interface)'
    
    if ($isPartialFile) {
        # For partial files, only remove XML comments NOT on the partial class/struct/record/interface line
        $lines = $content -split "`n"
        $newLines = @()
        $i = 0
        
        while ($i -lt $lines.Count) {
            $line = $lines[$i]
            
            # Check if this line has the partial declaration
            if ($line -match 'public\s+partial\s+(class|struct|record|interface)') {
                # Keep the line
                $newLines += $line
            } elseif ($line -match '^\s*///') {
                # This is an XML comment line - skip it
                # Do nothing (skip this line)
            } else {
                # Regular code line
                $newLines += $line
            }
            
            $i++
        }
        
        $content = $newLines -join "`n"
    } else {
        # For non-partial files, remove all XML comments
        $lines = $content -split "`n"
        $newLines = @()
        
        foreach ($line in $lines) {
            if ($line -notmatch '^\s*///') {
                $newLines += $line
            }
        }
        
        $content = $newLines -join "`n"
    }
    
    # Remove excess blank lines (more than 2 consecutive)
    $content = $content -replace "(\r?\n\r?\n)\r?\n+", "`$1`n"
    
    # Write back if changed
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $filesChanged++
        Write-Host "Modified: $($file.Name)"
    }
    
    $processedCount++
    if ($processedCount % 50 -eq 0) {
        Write-Host "Processed $processedCount files..."
    }
}

Write-Host ""
Write-Host "✓ Complete!"
Write-Host "  Processed: $processedCount files"
Write-Host "  Modified: $filesChanged files"
