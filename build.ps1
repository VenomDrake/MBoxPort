param(
    [string]$Dotnet = 'dotnet',
    [string]$InnoCompiler = '',
    [string]$BuildOutput = ''
)
$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$taskRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)
$taskArtifacts = Join-Path $taskRoot 'artifacts'
$taskPublish = Join-Path $taskArtifacts 'publish'
$taskSmoke = Join-Path $taskArtifacts 'smoke'
[xml]$taskVersionProps = Get-Content -LiteralPath (Join-Path $taskRoot 'src/Directory.Build.props') -Raw
$taskVersion = [string]$taskVersionProps.Project.PropertyGroup.Version
$taskProduct = [string]$taskVersionProps.Project.PropertyGroup.Product
if ($taskVersion -notmatch '^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$') { throw 'Versione di progetto non valida.' }
if ($taskProduct -notmatch '^[A-Za-z][A-Za-z0-9-]*$') { throw 'Nome di progetto non valido.' }
$taskPackagePrefix = "$taskProduct-$taskVersion"
$taskPackages = @()

function Invoke-TaskDotnet([string[]]$Arguments) {
    & $Dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Comando .NET non riuscito: $($Arguments -join ' ')" }
}

function Reset-TaskDirectory([string]$Path) {
    # Only the two fixed staging directories may be recursively removed.
    $taskFullPath = [System.IO.Path]::GetFullPath($Path)
    $taskRootPrefix = $taskRoot.TrimEnd([char[]]'\/') + [System.IO.Path]::DirectorySeparatorChar
    if (!$taskFullPath.StartsWith($taskRootPrefix, [System.StringComparison]::OrdinalIgnoreCase) -or
        ($taskFullPath -ne $taskPublish -and $taskFullPath -ne $taskSmoke)) {
        throw "Cartella temporanea fuori dal progetto: $taskFullPath"
    }
    foreach ($taskCheckedPath in @($taskArtifacts, $taskFullPath)) {
        if (Test-Path -LiteralPath $taskCheckedPath) {
            $taskItem = Get-Item -LiteralPath $taskCheckedPath -Force
            if (!$taskItem.PSIsContainer -or ($taskItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
                throw "La cartella temporanea deve essere una directory ordinaria: $taskCheckedPath"
            }
        }
    }
    if (Test-Path -LiteralPath $taskFullPath) { Remove-Item -LiteralPath $taskFullPath -Recurse -Force }
    New-Item -ItemType Directory -Path $taskFullPath -Force | Out-Null
}

function New-TaskZip([string]$Source, [string]$Destination, [bool]$SourceOnly) {
    $taskSourcePath = [System.IO.Path]::GetFullPath($Source).TrimEnd([char[]]'\/')
    $taskStream = [System.IO.File]::Open($Destination, [System.IO.FileMode]::Create)
    $taskZip = [System.IO.Compression.ZipArchive]::new($taskStream, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        Get-ChildItem -LiteralPath $taskSourcePath -Recurse -File -Force | Sort-Object FullName | ForEach-Object {
            $taskRelative = $_.FullName.Substring($taskSourcePath.Length).TrimStart([char[]]'\/').Replace('\', '/')
            if ($SourceOnly -and ($taskRelative -match '(^|/)(bin|obj|artifacts|node_modules|\.git|\.vs)(/|$)' -or $taskRelative -match '\.user$')) { return }
            if ($SourceOnly -and $BuildOutput) {
                $taskOutputPrefix = [System.IO.Path]::GetFullPath($BuildOutput).TrimEnd([char[]]'\/') + [System.IO.Path]::DirectorySeparatorChar
                if ($_.FullName.StartsWith($taskOutputPrefix, [System.StringComparison]::OrdinalIgnoreCase)) { return }
            }
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($taskZip, $_.FullName, $taskRelative, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally { $taskZip.Dispose(); $taskStream.Dispose() }
}

Push-Location $taskRoot
try {
    & (Join-Path $taskRoot 'tools/Verify-Readmes.ps1') -ProjectRoot $taskRoot
    $taskSdk = & $Dotnet --version
    if ($LASTEXITCODE -ne 0) { throw 'SDK .NET richiesto da global.json non disponibile.' }
    Write-Host "$taskProduct $taskVersion — SDK .NET $taskSdk — win-x64"
    Reset-TaskDirectory $taskPublish
    Reset-TaskDirectory $taskSmoke

    Invoke-TaskDotnet @('restore', 'src/PostaBridge/PostaBridge.csproj', '-r', 'win-x64', '-p:SelfContained=true', '--locked-mode')
    $taskBuildArgs = @('publish', 'src/PostaBridge/PostaBridge.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '--no-restore', '-p:PublishSingleFile=false', '-p:PublishTrimmed=false', '-o', $taskPublish, '--nologo', '-v', 'minimal')
    if ($BuildOutput) { $taskBuildArgs += "-p:BaseOutputPath=$BuildOutput" }
    Invoke-TaskDotnet $taskBuildArgs
    Copy-Item -LiteralPath (Join-Path $taskRoot 'GUIDA.txt'), (Join-Path $taskRoot 'VALIDAZIONE.txt') -Destination $taskPublish -Force
    Get-ChildItem -LiteralPath $taskRoot -Filter 'README*.md' -File | Copy-Item -Destination $taskPublish -Force
    Copy-Item -LiteralPath (Join-Path $taskRoot 'CHANGELOG.md'), (Join-Path $taskRoot 'LICENSE'), (Join-Path $taskRoot 'THIRD_PARTY.md') -Destination $taskPublish -Force
    & (Join-Path $taskRoot 'tools/Collect-Licenses.ps1') -PublishPath $taskPublish

    Invoke-TaskDotnet @('restore', 'tests/PostaBridge.Tests/PostaBridge.Tests.csproj', '-r', 'win-x64', '--locked-mode')
    Invoke-TaskDotnet @('test', 'tests/PostaBridge.Tests/PostaBridge.Tests.csproj', '-c', 'Release', '-r', 'win-x64', '--no-restore', '--nologo', '-v', 'minimal')
    $taskCoreTests = 'third_party/ContinuMail/tests/Mail2Pst.Core.Tests/Mail2Pst.Core.Tests.csproj'
    $taskCoreFilter = 'FullyQualifiedName~MboxParserTests|FullyQualifiedName~PstWriterTests|FullyQualifiedName~MimeMessageMapperTests|FullyQualifiedName~AttachmentWriterTests|FullyQualifiedName~ConversionRunnerTests|FullyQualifiedName~ConversionRunnerSourceReadTests|FullyQualifiedName~PstWriterRollbackTests|FullyQualifiedName~ConversionReportRollbackTests'
    Invoke-TaskDotnet @('restore', $taskCoreTests, '-r', 'win-x64', '--locked-mode')
    Invoke-TaskDotnet @('test', $taskCoreTests, '-c', 'Release', '-r', 'win-x64', '--no-restore', '--filter', $taskCoreFilter, '--nologo', '-v', 'minimal')
    Invoke-TaskDotnet @('restore', 'tools/SmokeTest/SmokeTest.csproj', '-r', 'win-x64', '--locked-mode')
    Invoke-TaskDotnet @('run', '--project', 'tools/SmokeTest/SmokeTest.csproj', '-c', 'Release', '-r', 'win-x64', '--no-restore', '--', $taskSmoke)

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $taskSourceZip = Join-Path $taskArtifacts "$taskPackagePrefix-Sorgenti.zip"
    New-TaskZip $taskRoot $taskSourceZip $true
    $taskPackages += $taskSourceZip
    New-Item -ItemType Directory -Path (Join-Path $taskPublish 'sorgenti') -Force | Out-Null
    Copy-Item -LiteralPath $taskSourceZip -Destination (Join-Path $taskPublish 'sorgenti') -Force
    $taskPortableZip = Join-Path $taskArtifacts "$taskPackagePrefix-Portatile-x64.zip"
    New-TaskZip $taskPublish $taskPortableZip $false
    $taskPackages += $taskPortableZip

    if (!$InnoCompiler) {
        $taskInnoCommand = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
        if ($taskInnoCommand) { $InnoCompiler = $taskInnoCommand.Source }
        foreach ($taskInnoRoot in @(${env:ProgramFiles(x86)}, $env:ProgramFiles, (Join-Path $env:LOCALAPPDATA 'Programs'))) {
            if (!$InnoCompiler -and $taskInnoRoot) {
                $taskInnoCandidate = Join-Path $taskInnoRoot 'Inno Setup 6/ISCC.exe'
                if (Test-Path -LiteralPath $taskInnoCandidate) { $InnoCompiler = $taskInnoCandidate }
            }
        }
    }
    if ($InnoCompiler) {
        $taskInnoVersion = (Get-Item -LiteralPath $InnoCompiler).VersionInfo.FileVersion
        Write-Host "Creazione installer con Inno Setup $taskInnoVersion"
        & $InnoCompiler "/DAppVersion=$taskVersion" "/DAppName=$taskProduct" (Join-Path $taskRoot 'installer/PostaBridge.iss')
        if ($LASTEXITCODE -ne 0) { throw 'Creazione installer non riuscita.' }
        $taskInstaller = Join-Path $taskArtifacts "$taskPackagePrefix-Setup-x64.exe"
        if (!(Test-Path -LiteralPath $taskInstaller)) { throw 'Installer atteso non generato.' }
        $taskPackages += $taskInstaller
    } else {
        Write-Host 'Inno Setup non trovato: generati i pacchetti sorgenti e portatile.'
    }
    $taskHashLines = foreach ($taskPackage in $taskPackages) {
        $taskHash = Get-FileHash -LiteralPath $taskPackage -Algorithm SHA256
        '{0}  {1}' -f $taskHash.Hash.ToLowerInvariant(), [System.IO.Path]::GetFileName($taskPackage)
    }
    [System.IO.File]::WriteAllLines((Join-Path $taskArtifacts 'SHA256SUMS.txt'), [string[]]$taskHashLines, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Pacchetti pronti: $taskArtifacts"
} finally { Pop-Location }
