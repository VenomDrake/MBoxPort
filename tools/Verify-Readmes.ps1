param([string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
$taskCatalogDirectory = Join-Path $ProjectRoot 'src/PostaBridge.Engine/Localization'
$taskCatalogs = @(Get-ChildItem -LiteralPath $taskCatalogDirectory -Filter '*.json' -File | Sort-Object BaseName)
if (!$taskCatalogs.Count) { throw 'No application language catalogs found.' }
$taskExpectedReadmes = @($taskCatalogs | ForEach-Object {
    if ($_.BaseName -eq 'en') { 'README.md' } else { 'README.' + $_.BaseName + '.md' }
})
$taskEnglish = [System.IO.File]::ReadAllText((Join-Path $ProjectRoot 'README.md')).Replace("`r`n", "`n")
$taskCodePattern = '(?ms)^```[^\n]*\n(.*?)^```[ \t]*$'
$taskEnglishCode = @([regex]::Matches($taskEnglish, $taskCodePattern) | ForEach-Object { $_.Groups[1].Value })
$taskEnglishSections = [regex]::Matches($taskEnglish, '(?m)^##\s+').Count
$taskEnglishLinks = @([regex]::Matches($taskEnglish, '\]\(([^)]+)\)') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
foreach ($taskReadmeName in $taskExpectedReadmes) {
    $taskReadmePath = Join-Path $ProjectRoot $taskReadmeName
    if (!(Test-Path -LiteralPath $taskReadmePath -PathType Leaf)) { throw "Missing application README: $taskReadmeName" }
    $taskText = [System.IO.File]::ReadAllText($taskReadmePath).Replace("`r`n", "`n")
    if ([string]::IsNullOrWhiteSpace($taskText)) { throw "Empty README: $taskReadmeName" }
    $taskLinks = @([regex]::Matches($taskText, '\]\(([^)]+)\)') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
    foreach ($taskTarget in $taskExpectedReadmes) {
        if ($taskLinks -notcontains $taskTarget) { throw "Missing language navigation link in ${taskReadmeName}: $taskTarget" }
    }
    if (@(Compare-Object $taskEnglishLinks $taskLinks).Count) { throw "README link targets differ from English: $taskReadmeName" }
    if ([regex]::Matches($taskText, '(?m)^##\s+').Count -ne $taskEnglishSections) { throw "README section count differs from English: $taskReadmeName" }
    $taskCode = @([regex]::Matches($taskText, $taskCodePattern) | ForEach-Object { $_.Groups[1].Value })
    if ($taskCode.Count -ne $taskEnglishCode.Count) { throw "README command block count differs from English: $taskReadmeName" }
    for ($taskBlock = 0; $taskBlock -lt $taskCode.Count; $taskBlock++) {
        if ($taskCode[$taskBlock] -cne $taskEnglishCode[$taskBlock]) { throw "README command block differs from English: $taskReadmeName (block $($taskBlock + 1))" }
    }
}
Write-Host "README languages verified: $($taskCatalogs.BaseName -join ', ')"
