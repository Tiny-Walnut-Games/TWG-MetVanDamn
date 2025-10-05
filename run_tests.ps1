#!/usr/bin/env pwsh

# Simple, reliable headless PlayMode test runner
# - Auto-detects Unity version from ProjectSettings/ProjectVersion.txt
# - Finds Unity.exe via UNITY_EXE or Unity Hub standard locations
# - Writes results/logs to debug
# Usage:
#   pwsh ./run_tests.ps1
# Optional env vars:
#   $env:UNITY_EXE, $env:UNITY_TEST_FILTER, $env:UNITY_TEST_PLATFORM, $env:UNITY_EXTRA_ARGS

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host "MetVanDAMN Test Runner" -ForegroundColor Green
Write-Host "======================="

function Get-UnityVersion([string]$projectPath) {
    $pv = Join-Path $projectPath 'ProjectSettings/ProjectVersion.txt'
    if (-not (Test-Path $pv)) { return $null }
    $lines = Get-Content -LiteralPath $pv
    foreach ($line in $lines) {
        if ($line -match 'm_EditorVersion(?:WithRevision)?:\s*(.+)$') {
            $v = $Matches[1].Trim()
            # Trim possible revision in parentheses
            if ($v -match '^(\S+)') { return $Matches[1] }
            return $v
        }
    }
    return $null
}

function Resolve-UnityEditor([string]$version) {
    if ($env:UNITY_EXE -and (Test-Path $env:UNITY_EXE)) { return $env:UNITY_EXE }
    if (-not $version) { return $null }
    $candidates = @(
        "C:\\Program Files\\Unity\\Hub\\Editor\\$version\\Editor\\Unity.exe",
        "C:\\Program Files (x86)\\Unity\\Hub\\Editor\\$version\\Editor\\Unity.exe"
    )
    foreach ($c in $candidates) { if (Test-Path $c) { return $c } }
    # Fallback: first Unity under Hub
    $hubRoots = @("C:\\Program Files\\Unity\\Hub\\Editor", "C:\\Program Files (x86)\\Unity\\Hub\\Editor")
    foreach ($root in $hubRoots) {
        if (Test-Path $root) {
            $sub = Get-ChildItem -LiteralPath $root -Directory -ErrorAction SilentlyContinue | Sort-Object Name -Descending
            foreach ($d in $sub) {
                $exe = Join-Path $d.FullName 'Editor/Unity.exe'
                if (Test-Path $exe) { return $exe }
            }
        }
    }
    return $null
}

$projectPath = (Get-Location).Path
$unityVersion = Get-UnityVersion -projectPath $projectPath
$unityPath = Resolve-UnityEditor -version $unityVersion

if (-not $unityPath) {
    Write-Host "ERROR: Unable to resolve Unity editor for version '$unityVersion'." -ForegroundColor Red
    Write-Host "   Set UNITY_EXE or install via Unity Hub." -ForegroundColor Yellow
    exit 1
}

Write-Host "Unity Version: $unityVersion" -ForegroundColor Cyan
Write-Host "Unity Path:    $unityPath" -ForegroundColor Cyan
Write-Host "Project Path:  $projectPath" -ForegroundColor Cyan

$resultsDir = Join-Path $projectPath 'debug'
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
$ts = Get-Date -Format 'yyyyMMdd_HHmmss'
$logPath = Join-Path $resultsDir "unity_powershell_test_$ts.log"

$testPlatform = if ($env:UNITY_TEST_PLATFORM) { $env:UNITY_TEST_PLATFORM } else { 'EditMode' }
$testFilter = $env:UNITY_TEST_FILTER
$extraArgs = $env:UNITY_EXTRA_ARGS

Write-Host "Running Unity tests..." -ForegroundColor Yellow
Write-Host "Log:     $logPath" -ForegroundColor Cyan
Write-Host "Platform: $testPlatform" -ForegroundColor Cyan
if ($testFilter) { Write-Host "Filter:   $testFilter" -ForegroundColor Cyan }

$argsList = @(
    '-batchmode', '-nographics',
    '-projectPath', $projectPath,
    '-executeMethod', 'TinyWalnutGames.Tools.Editor.HeadlessTestRunner.RunEditMode',
    '-logFile', $logPath,
    '-quit'
)
if ($testFilter) { $argsList += @('-testFilter', $testFilter) }
if ($extraArgs) { $argsList += $extraArgs.Split(' ') }

# Ensure arguments are strings (Start-Process is picky about types)
$argsList = $argsList | ForEach-Object { [string]$_ }

# Remember start time for fallback scans
$runStart = Get-Date

# Launch Unity with timeout to prevent hanging
$timeoutMinutes = 10  # 10 minute timeout for test runs
Write-Host "Launching Unity (timeout: ${timeoutMinutes} minutes)..." -ForegroundColor Cyan

$proc = Start-Process -FilePath $unityPath -ArgumentList $argsList -NoNewWindow -PassThru
$timedOut = $false

# Wait for process to exit with timeout
try {
    $proc | Wait-Process -Timeout ($timeoutMinutes * 60) -ErrorAction Stop
}
catch {
    $timedOut = $true
    Write-Host "Unity process timed out after ${timeoutMinutes} minutes - terminating..." -ForegroundColor Red
    try { $proc.Kill() } catch { }
}

$exit = $proc.ExitCode
if ($null -eq $exit) {
    if ($timedOut) {
        $exit = 999  # Custom exit code for timeout
    }
    else {
        $exit = 1
    }
}

if ($exit -eq 0) {
    Write-Host "Unity exited with code 0 (success)" -ForegroundColor Green
}
elseif ($exit -eq 999) {
    Write-Host "Unity timed out and was terminated" -ForegroundColor Red
    Write-Host "This usually indicates a test hang or compilation issue" -ForegroundColor Yellow
}
else {
    Write-Host "Unity exited with code $exit" -ForegroundColor Yellow
}

if (Test-Path $logPath) {
    Write-Host "\nLog tail:" -ForegroundColor DarkCyan
    Get-Content -LiteralPath $logPath -Tail 60 | ForEach-Object { Write-Host $_ }

    # Check for compilation errors that would prevent tests from running
    $compilationErrors = Get-Content -LiteralPath $logPath | Select-String -Pattern "error CS\d+:" | Measure-Object
    if ($compilationErrors.Count -gt 0) {
        Write-Host "`n❌ Compilation errors detected ($($compilationErrors.Count) errors)" -ForegroundColor Red
        Write-Host "Tests cannot run due to compilation failures" -ForegroundColor Yellow
        exit 1
    }
}

# Patiently wait for test execution to complete (look for completion messages in log)
$maxWaitSec = 120  # 2 minutes for test execution
$testCompleted = $false

Write-Host "Waiting for test execution to complete..." -ForegroundColor Cyan
for ($i = 0; $i -lt $maxWaitSec -and -not $testCompleted; $i += 5) {
    Start-Sleep -Seconds 5

    if (Test-Path $logPath) {
        $logContent = Get-Content -LiteralPath $logPath -Tail 20
        if ($logContent -match "Test execution completed" -or $logContent -match "All tests finished") {
            $testCompleted = $true
            Write-Host "✅ Test execution completed" -ForegroundColor Green
            break
        }
    }
}

if (-not $testCompleted) {
    Write-Host "⚠️ Test execution may still be running or completed without clear completion message" -ForegroundColor Yellow
}

# Fallback 2: scan common directories for any TestResults_*.xml created around run start
if (-not (Test-Path $testResults)) {
    try {
        $probeDirs = @(
            $resultsDir,
            (Join-Path $projectPath 'Assets/debug'),
            $projectPath
        ) | Where-Object { Test-Path $_ }
        $found = @()
        foreach ($d in $probeDirs) {
            $found += Get-ChildItem -LiteralPath $d -Filter 'TestResults_*.xml' -File -ErrorAction SilentlyContinue
        }
        $cand = $found | Where-Object { $_.LastWriteTime -ge $runStart.AddMinutes(-10) } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($cand) {
            try { Copy-Item -LiteralPath $cand.FullName -Destination $testResults -Force } catch {}
        }
    }
    catch {}
}

if (Test-Path $testResults) {
    try {
        [xml]$xml = Get-Content -LiteralPath $testResults
        $tr = $xml.'test-run'
        if ($tr) {
            Write-Host "\nTest Summary" -ForegroundColor Cyan
            Write-Host ("   Total: {0}" -f $tr.total)
            Write-Host ("   Passed: {0}" -f $tr.passed) -ForegroundColor Green
            Write-Host ("   Failed: {0}" -f $tr.failed) -ForegroundColor Red
            Write-Host ("   Duration: {0}s" -f $tr.duration)
            if ([int]$tr.failed -gt 0) { exit 2 } else { exit 0 }
        }
    }
    catch {
        Write-Host "ERROR: Could not parse test results XML: $($_.Exception.Message)" -ForegroundColor Red
        exit 3
    }
}
else {
    Write-Host "No XML produced by -runTests. Trying Editor API fallback (-executeMethod)..." -ForegroundColor Yellow
    $ts2 = Get-Date -Format 'yyyyMMdd_HHmmss'
    $testResults2 = Join-Path $resultsDir "TestResults_$ts2.xml"
    $logPath2 = Join-Path $resultsDir "unity_powershell_test_${ts2}_fallback.log"
    $argsList2 = @(
        '-batchmode', '-nographics',
        '-projectPath', $projectPath,
        '-executeMethod', 'TinyWalnutGames.Tools.Editor.HeadlessTestRunner.RunPlayMode',
        '-testResults', $testResults2,
        '-logFile', $logPath2,
        '-quit'
    )
    # Propagate env filter/category if present
    if ($env:UNITY_TEST_FILTER) { $argsList2 += @('-testFilter', $env:UNITY_TEST_FILTER) }
    if ($env:UNITY_TEST_CATEGORY) { $argsList2 += @('-testCategory', $env:UNITY_TEST_CATEGORY) }
    $argsList2 = $argsList2 | ForEach-Object { [string]$_ }
    $proc2 = Start-Process -FilePath $unityPath -ArgumentList $argsList2 -NoNewWindow -PassThru -Wait
    $exit2 = $proc2.ExitCode; if ($null -eq $exit2) { $exit2 = 1 }
    if ($exit2 -ne 0) { Write-Host "Unity fallback exited with code $exit2" -ForegroundColor Yellow }
    # Prefer the explicitly requested path, then fall back to scanning
    $testResults = $testResults2
    if (-not (Test-Path $testResults)) {
        $found2 = Get-ChildItem -LiteralPath $resultsDir -Filter 'TestResults_*.xml' -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($found2) { $testResults = $found2.FullName }
    }
    if (Test-Path $testResults) {
        try {
            [xml]$xml = Get-Content -LiteralPath $testResults
            $tr = $xml.'test-run'
            if ($tr) {
                Write-Host "\nTest Summary" -ForegroundColor Cyan
                Write-Host ("   Total: {0}" -f $tr.total)
                Write-Host ("   Passed: {0}" -f $tr.passed) -ForegroundColor Green
                Write-Host ("   Failed: {0}" -f $tr.failed) -ForegroundColor Red
                Write-Host ("   Duration: {0}s" -f $tr.duration)
                if ([int]$tr.failed -gt 0) { exit 2 } else { exit 0 }
            }
        }
        catch {
            Write-Host "ERROR: Could not parse fallback test results XML: $($_.Exception.Message)" -ForegroundColor Red
            exit 3
        }
    }
    Write-Host "ERROR: Test results file not found after fallback. Trying enhanced methods..." -ForegroundColor Red
    $ts3 = Get-Date -Format 'yyyyMMdd_HHmmss'
    $testResults3 = Join-Path $resultsDir "TestResults_$ts3.xml"
    $logPath3 = Join-Path $resultsDir "unity_powershell_test_${ts3}_enhanced.log"

    # Try the enhanced methods based on test platform
    $enhancedMethod = switch ($testPlatform) {
        "EditMode" { "TinyWalnutGames.Tools.Editor.HeadlessTestRunner.RunEditMode" }
        "PlayMode" { "TinyWalnutGames.Tools.Editor.HeadlessTestRunner.RunPlayMode" }
        default { "TinyWalnutGames.Tools.Editor.HeadlessTestRunner.RunAll" }
    }

    Write-Host "Trying enhanced method: $enhancedMethod" -ForegroundColor Cyan
    $argsList3 = @(
        '-batchmode', '-nographics',
        '-projectPath', $projectPath,
        '-executeMethod', $enhancedMethod,
        '-testResults', $testResults3,
        '-logFile', $logPath3,
        '-quit'
    )
    # Propagate env filter/category if present
    if ($env:UNITY_TEST_FILTER) { $argsList3 += @('-testFilter', $env:UNITY_TEST_FILTER) }
    if ($env:UNITY_TEST_CATEGORY) { $argsList3 += @('-testCategory', $env:UNITY_TEST_CATEGORY) }
    $argsList3 = $argsList3 | ForEach-Object { [string]$_ }
    $proc3 = Start-Process -FilePath $unityPath -ArgumentList $argsList3 -NoNewWindow -PassThru -Wait
    $exit3 = $proc3.ExitCode; if ($null -eq $exit3) { $exit3 = 1 }
    if ($exit3 -ne 0) { Write-Host "Unity enhanced method exited with code $exit3" -ForegroundColor Yellow }

    # Use the enhanced results
    $testResults = $testResults3
    if (-not (Test-Path $testResults)) {
        $found3 = Get-ChildItem -LiteralPath $resultsDir -Filter 'TestResults_*.xml' -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($found3) { $testResults = $found3.FullName }
    }

    if (Test-Path $testResults) {
        try {
            [xml]$xml = Get-Content -LiteralPath $testResults
            $tr = $xml.'test-run'
            if ($tr) {
                Write-Host "\nTest Summary (Enhanced)" -ForegroundColor Cyan
                Write-Host ("   Total: {0}" -f $tr.total)
                Write-Host ("   Passed: {0}" -f $tr.passed) -ForegroundColor Green
                Write-Host ("   Failed: {0}" -f $tr.failed) -ForegroundColor Red
                Write-Host ("   Duration: {0}s" -f $tr.duration)
                if ([int]$tr.failed -gt 0) { exit 2 } else { exit 0 }
            }
        }
        catch {
            Write-Host "ERROR: Could not parse enhanced test results XML: $($_.Exception.Message)" -ForegroundColor Red
            exit 3
        }
    }
    else {
        Write-Host "ERROR: No test results found after all attempts" -ForegroundColor Red
        if (Test-Path $logPath3) { Write-Host "See enhanced log: $logPath3" -ForegroundColor Yellow }
        if (Test-Path $logPath2) { Write-Host "See fallback log: $logPath2" -ForegroundColor Yellow }
        if (Test-Path $logPath) { Write-Host "See original log: $logPath" -ForegroundColor Yellow }
        exit 4
    }
}

Write-Host "\nTest run complete!" -ForegroundColor Green
