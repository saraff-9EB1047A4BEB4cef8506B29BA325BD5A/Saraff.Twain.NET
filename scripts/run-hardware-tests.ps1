[CmdletBinding()]
param(
    [ValidateSet('all', 'x86-twain1', 'x86-twain2', 'x64-twain2')]
    [string]$Mode = 'all',

    [string]$SourceX86,
    [string]$SourceX64,
    [string]$DsmPath,
    [ValidateSet('0', '1', '')]
    [string]$Feeder = '',

    [string]$Configuration = 'Release',
    [string]$OutputRoot = (Join-Path $PSScriptRoot '..\artifacts\hardware')
)

$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot '..\tests\Saraff.Twain.HardwareTests\Saraff.Twain.HardwareTests.csproj'
$outputRoot = [System.IO.Path]::GetFullPath($OutputRoot)

$modes = if ($Mode -eq 'all') {
    @('x86-twain1', 'x86-twain2', 'x64-twain2')
} else {
    @($Mode)
}

$previous = @{
    TWAIN_HARDWARE_TESTS = $env:TWAIN_HARDWARE_TESTS
    TWAIN_TEST_MODE = $env:TWAIN_TEST_MODE
    TWAIN_TEST_OUTPUT = $env:TWAIN_TEST_OUTPUT
    TWAIN_TEST_SOURCE = $env:TWAIN_TEST_SOURCE
    TWAIN_TEST_DSM_PATH = $env:TWAIN_TEST_DSM_PATH
    TWAIN_TEST_FEEDER = $env:TWAIN_TEST_FEEDER
}

$failed = @()

try {
    foreach ($currentMode in $modes) {
        $platform = if ($currentMode.StartsWith('x86-', [System.StringComparison]::Ordinal)) { 'x86' } else { 'x64' }
        $source = if ($platform -eq 'x86') { $SourceX86 } else { $SourceX64 }
        $output = Join-Path $outputRoot $currentMode
        New-Item -ItemType Directory -Force -Path $output | Out-Null

        $env:TWAIN_HARDWARE_TESTS = '1'
        $env:TWAIN_TEST_MODE = $currentMode
        $env:TWAIN_TEST_OUTPUT = $output
        $env:TWAIN_TEST_SOURCE = $source
        $env:TWAIN_TEST_DSM_PATH = $DsmPath
        $env:TWAIN_TEST_FEEDER = $Feeder

        Write-Host ""
        Write-Host "=== $currentMode ($platform) ===" -ForegroundColor Cyan
        Write-Host "Output: $output"

        dotnet build $project -c $Configuration -p:PlatformTarget=$platform -t:Rebuild
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed for $currentMode."
        }

        dotnet test $project -c $Configuration --no-build -p:PlatformTarget=$platform --logger "console;verbosity=normal" --results-directory $output
        $testExitCode = $LASTEXITCODE

        $report = Join-Path $output 'report.txt'
        if (Test-Path $report) {
            Get-Content $report
        }

        if ($testExitCode -ne 0 -or -not (Test-Path $report)) {
            $failed += $currentMode
        }
    }
}
finally {
    foreach ($name in $previous.Keys) {
        [System.Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process')
    }
}

if ($failed.Count -gt 0) {
    throw "Hardware tests failed for: $($failed -join ', ')"
}

Write-Host ""
Write-Host "All requested hardware test modes passed." -ForegroundColor Green
