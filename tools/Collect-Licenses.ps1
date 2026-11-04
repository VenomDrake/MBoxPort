param([Parameter(Mandatory=$true)][string]$PublishPath)
$ErrorActionPreference = 'Stop'
$taskRepo = Split-Path $PSScriptRoot -Parent
$taskAssets = Get-Content -LiteralPath (Join-Path $taskRepo 'src/PostaBridge/obj/project.assets.json') -Raw | ConvertFrom-Json
$taskLicenseDir = Join-Path $PublishPath 'licenses'
New-Item -ItemType Directory -Path $taskLicenseDir -Force | Out-Null
$taskPackageRoots = @($taskAssets.packageFolders.PSObject.Properties.Name)
$taskInventory = @()

function Find-TaskPackage([string]$RelativePath) {
    foreach ($taskCache in $taskPackageRoots) {
        $taskCandidate = Join-Path $taskCache $RelativePath
        if (Test-Path -LiteralPath $taskCandidate -PathType Container) { return $taskCandidate }
    }
    throw "Pacchetto mancante: $RelativePath"
}

function Copy-TaskLicenseFiles([string]$PackagePath, [string]$Destination) {
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    Get-ChildItem -LiteralPath $PackagePath -Recurse -File | Where-Object {
        $_.Name -match '(?i)(licen[cs]e|notice|copying)' -and $_.Extension -notin '.dll', '.pdb'
    } | ForEach-Object {
        $taskRelative = $_.FullName.Substring($PackagePath.Length).TrimStart([char[]]'\/')
        $taskTarget = Join-Path $Destination $taskRelative
        New-Item -ItemType Directory -Path (Split-Path $taskTarget -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $_.FullName -Destination $taskTarget -Force
    }
}

$taskLibraries = @($taskAssets.libraries.PSObject.Properties | Where-Object { $_.Value.type -eq 'package' } | Sort-Object Name)
foreach ($taskLibrary in $taskLibraries) {
    $taskPackage = Find-TaskPackage $taskLibrary.Value.path
    $taskPackageTarget = Join-Path $taskLicenseDir ($taskLibrary.Name.Replace('/', '-'))
    Copy-TaskLicenseFiles $taskPackage $taskPackageTarget
    $taskNuspec = Get-ChildItem -LiteralPath $taskPackage -Filter '*.nuspec' | Select-Object -First 1
    if (!$taskNuspec) { throw "Metadati NuGet mancanti: $($taskLibrary.Name)" }
    [xml]$taskXml = Get-Content -LiteralPath $taskNuspec.FullName -Raw
    $taskMeta = $taskXml.package.metadata
    Copy-Item -LiteralPath $taskNuspec.FullName -Destination $taskPackageTarget -Force
    $taskLicense = if ($taskMeta.license) { [string]$taskMeta.license.InnerText } else { [string]$taskMeta.licenseUrl }
    $taskInventory += [pscustomobject]@{ package=$taskLibrary.Name; authors=[string]$taskMeta.authors; copyright=[string]$taskMeta.copyright; license=$taskLicense; url=[string]$taskMeta.projectUrl }
}

# Read exact framework versions from the published app, never from cache ordering.
[xml]$taskProductProps = Get-Content -LiteralPath (Join-Path $taskRepo 'src/Directory.Build.props') -Raw
$taskProduct = [string]$taskProductProps.Project.PropertyGroup.Product
$taskRuntimeConfig = Get-Content -LiteralPath (Join-Path $PublishPath "$taskProduct.runtimeconfig.json") -Raw | ConvertFrom-Json
$taskRuntimes = @($taskRuntimeConfig.runtimeOptions.includedFrameworks)
foreach ($taskRuntimeName in @('Microsoft.NETCore.App', 'Microsoft.WindowsDesktop.App')) {
    $taskRuntime = $taskRuntimes | Where-Object { $_.name -eq $taskRuntimeName } | Select-Object -First 1
    if (!$taskRuntime -or !$taskRuntime.version) { throw "Runtime autonomo non trovato: $taskRuntimeName" }
    $taskPackName = $taskRuntimeName.ToLowerInvariant() + '.runtime.win-x64'
    $taskRuntimePath = Find-TaskPackage ($taskPackName + '/' + $taskRuntime.version)
    Copy-TaskLicenseFiles $taskRuntimePath (Join-Path $taskLicenseDir ($taskPackName + '-' + $taskRuntime.version))
    $taskInventory += [pscustomobject]@{ package=($taskPackName + '/' + $taskRuntime.version); authors='Microsoft'; copyright=''; license='MIT'; url='https://github.com/dotnet/' }
}
Copy-Item -LiteralPath (Join-Path $taskRepo 'LICENSE') -Destination (Join-Path $taskLicenseDir 'GPL-3.0.txt') -Force
Copy-Item -LiteralPath (Join-Path $taskRepo 'third_party/ContinuMail/NOTICE') -Destination (Join-Path $taskLicenseDir 'ContinuMail-NOTICE.txt') -Force
Copy-Item -LiteralPath (Join-Path $taskRepo 'third_party/ContinuMail/vendor/LICENSE-PSTFileFormat.txt') -Destination $taskLicenseDir -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'licenses/Apache-2.0.txt'), (Join-Path $PSScriptRoot 'licenses/LGPL-3.0.txt'), (Join-Path $PSScriptRoot 'licenses/MIT.txt') -Destination $taskLicenseDir -Force
$taskInventory | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $taskLicenseDir 'packages.json') -Encoding UTF8
