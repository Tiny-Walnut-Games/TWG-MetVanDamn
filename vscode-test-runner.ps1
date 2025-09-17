#!/usr/bin/env pwsh

# Standalone Unity Test Runner for VS Code
# Provides test execution without requiring Unity to be running
# Can be used for unit tests that don't require Unity-specific components

param(
    [string]$TestFilter = "",
    [string]$TestCategory = "",
    [string]$TestMode = "All",
    [switch]$DryRun = $false,
    [switch]$Verbose = $false
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host "🧙‍♂️ MetVanDAMN VS Code Test Runner" -ForegroundColor Magenta
Write-Host "=================================="

$projectPath = Get-Location
$debugDir = Join-Path $projectPath "debug"
New-Item -ItemType Directory -Force -Path $debugDir | Out-Null

# Check if we have Unity available
$unityAvailable = $false
try {
    if ($env:UNITY_EXE -and (Test-Path $env:UNITY_EXE)) {
        $unityAvailable = $true
        $unityPath = $env:UNITY_EXE
    } else {
        # Try to find Unity via standard paths
        $candidates = @(
            "C:\Program Files\Unity\Hub\Editor\*\Editor\Unity.exe",
            "C:\Program Files (x86)\Unity\Hub\Editor\*\Editor\Unity.exe"
        )
        foreach ($pattern in $candidates) {
            $found = Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -First 1
            if ($found) {
                $unityPath = $found.FullName
                $unityAvailable = $true
                break
            }
        }
    }
} catch { }

if ($unityAvailable) {
    Write-Host "✅ Unity found: $unityPath" -ForegroundColor Green
    if ($DryRun) {
        Write-Host "🔍 DRY RUN: Would execute Unity tests with parameters:" -ForegroundColor Yellow
        Write-Host "   Test Mode: $TestMode" -ForegroundColor Cyan
        Write-Host "   Test Filter: $TestFilter" -ForegroundColor Cyan
        Write-Host "   Test Category: $TestCategory" -ForegroundColor Cyan
        exit 0
    }
    
    # Use the enhanced PowerShell runner
    $env:UNITY_TEST_PLATFORM = $TestMode
    if ($TestFilter) { $env:UNITY_TEST_FILTER = $TestFilter }
    if ($TestCategory) { $env:UNITY_TEST_CATEGORY = $TestCategory }
    
    Write-Host "🚀 Launching Unity test execution..." -ForegroundColor Green
    & "$projectPath/run_tests.ps1"
    exit $LASTEXITCODE
} else {
    Write-Host "⚠️ Unity not available - attempting standalone C# test execution" -ForegroundColor Yellow
    
    # Look for test assemblies that we can run with dotnet test
    $testProjects = @()
    Get-ChildItem -Path $projectPath -Recurse -Include "*.Tests.csproj" | ForEach-Object {
        $testProjects += $_.FullName
    }
    
    if ($testProjects.Count -eq 0) {
        Write-Host "❌ No test projects found and Unity not available" -ForegroundColor Red
        Write-Host "💡 To use this runner:" -ForegroundColor Yellow
        Write-Host "   1. Install Unity and set UNITY_EXE environment variable" -ForegroundColor Cyan
        Write-Host "   2. Or ensure test projects are configured for standalone execution" -ForegroundColor Cyan
        exit 1
    }
    
    Write-Host "🔍 Found test projects:" -ForegroundColor Cyan
    $testProjects | ForEach-Object { Write-Host "   - $_" -ForegroundColor DarkCyan }
    
    if ($DryRun) {
        Write-Host "🔍 DRY RUN: Would execute dotnet test on these projects" -ForegroundColor Yellow
        exit 0
    }
    
    $totalPassed = 0
    $totalFailed = 0
    $totalSkipped = 0
    
    foreach ($project in $testProjects) {
        Write-Host "`n🧪 Running tests for: $(Split-Path $project -Leaf)" -ForegroundColor Green
        
        $args = @("test", $project, "--logger", "trx", "--results-directory", $debugDir)
        if ($TestFilter) { $args += @("--filter", $TestFilter) }
        if ($Verbose) { $args += @("--verbosity", "detailed") }
        
        try {
            $result = & dotnet @args 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ Tests passed for $(Split-Path $project -Leaf)" -ForegroundColor Green
            } else {
                Write-Host "❌ Tests failed for $(Split-Path $project -Leaf)" -ForegroundColor Red
            }
            
            # Parse results from TRX files if available
            $trxFiles = Get-ChildItem -Path $debugDir -Filter "*.trx" -File | Sort-Object LastWriteTime -Descending
            if ($trxFiles.Count -gt 0) {
                try {
                    [xml]$trx = Get-Content $trxFiles[0].FullName
                    $summary = $trx.TestRun.ResultSummary
                    if ($summary) {
                        $passed = [int]$summary.Counters.passed
                        $failed = [int]$summary.Counters.failed
                        $skipped = [int]$summary.Counters.inconclusive + [int]$summary.Counters.notExecuted
                        
                        $totalPassed += $passed
                        $totalFailed += $failed
                        $totalSkipped += $skipped
                        
                        Write-Host "   Passed: $passed, Failed: $failed, Skipped: $skipped" -ForegroundColor DarkGray
                    }
                } catch {
                    Write-Host "   Could not parse test results" -ForegroundColor DarkYellow
                }
            }
        } catch {
            Write-Host "❌ Error running tests: $($_.Exception.Message)" -ForegroundColor Red
            $totalFailed++
        }
    }
    
    Write-Host "`n📊 Overall Results:" -ForegroundColor Magenta
    Write-Host "   Total Passed: $totalPassed" -ForegroundColor Green
    Write-Host "   Total Failed: $totalFailed" -ForegroundColor Red
    Write-Host "   Total Skipped: $totalSkipped" -ForegroundColor Yellow
    
    if ($totalFailed -gt 0) {
        exit 1
    } else {
        exit 0
    }
}