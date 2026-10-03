param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string]$Runtime = 'win-x64',
    [string]$Filter = ''
)

$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw 'FlaUI E2E requires Windows and an unlocked interactive desktop.'
}
if (-not [Environment]::UserInteractive) {
    throw 'FlaUI E2E requires an interactive desktop session.'
}

$repoRoot = Split-Path $PSScriptRoot -Parent
$publishDir = Join-Path $repoRoot "artifacts/e2e/app/$Runtime"
$runId = "$(Get-Date -Format 'yyyyMMdd-HHmmss')-$([Guid]::NewGuid().ToString('N'))"
$resultsDir = Join-Path $repoRoot "artifacts/e2e/results/$runId"
$previousAppPath = $env:E2E_APP_PATH
$previousArtifactsDir = $env:E2E_ARTIFACTS_DIR

Push-Location $repoRoot
try {
    New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null
    Write-Host "E2E results: $resultsDir"
    dotnet publish "$repoRoot/src/BackgroundServiceSample/BackgroundServiceSample.csproj" -c Release -r $Runtime --self-contained false -o $publishDir
    if ($LASTEXITCODE -ne 0) { throw 'App publish failed.' }

    $env:E2E_APP_PATH = Join-Path $publishDir 'BackgroundServiceSample.exe'
    $env:E2E_ARTIFACTS_DIR = $resultsDir
    $testArguments = @(
        'test', "$repoRoot/tests/BackgroundServiceSample.E2E/BackgroundServiceSample.E2E.csproj",
        '-c', 'Release', '--logger', 'trx;LogFileName=e2e.trx',
        '--results-directory', $resultsDir, '--blame-hang-timeout', '2m'
    )
    if (-not [string]::IsNullOrWhiteSpace($Filter)) {
        $testArguments += @('--filter', $Filter)
    }
    dotnet @testArguments
    if ($LASTEXITCODE -ne 0) { throw 'E2E tests failed. See artifacts/e2e/results.' }
}
finally {
    $env:E2E_APP_PATH = $previousAppPath
    $env:E2E_ARTIFACTS_DIR = $previousArtifactsDir
    Pop-Location
}
