#!/usr/bin/env pwsh

# Simple test runner for MetVanDAMN tests
# Run from the project root directory

Write-Host "🚀 MetVanDAMN Test Runner" -ForegroundColor Green
Write-Host "=========================="

$unityPath = "C:\Program Files\Unity\Hub\Editor\2023.3.36f1\Editor\Unity.exe"
$projectPath = Get-Location

if (-not (Test-Path $unityPath)) {
    Write-Host "❌ Unity not found at: $unityPath" -ForegroundColor Red
    Write-Host "Please update the Unity path in this script" -ForegroundColor Yellow
    exit 1
}

Write-Host "🎮 Unity Path: $unityPath" -ForegroundColor Cyan
Write-Host "📁 Project Path: $projectPath" -ForegroundColor Cyan

# Test parameters
$testResults = Join-Path $projectPath "TestResults_$(Get-Date -Format 'yyyyMMdd_HHmmss').xml"

Write-Host "🧪 Running Unity tests..." -ForegroundColor Yellow
Write-Host "📊 Results will be saved to: $testResults" -ForegroundColor Cyan

try {
    # Run Unity tests in batch mode
    & $unityPath -runTests -batchmode -projectPath $projectPath -testResults $testResults -testPlatform playmode -logFile -

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Tests completed successfully!" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️ Tests completed with exit code: $LASTEXITCODE" -ForegroundColor Yellow
    }

    if (Test-Path $testResults) {
        Write-Host "📄 Test results saved to: $testResults" -ForegroundColor Green

        # Try to show a summary
        try {
            [xml]$xml = Get-Content $testResults
            $testRun = $xml.'test-run'
            if ($testRun) {
                Write-Host "" -ForegroundColor White
                Write-Host "📊 Test Summary:" -ForegroundColor Cyan
                Write-Host "   Total: $($testRun.total)" -ForegroundColor White
                Write-Host "   Passed: $($testRun.passed)" -ForegroundColor Green
                Write-Host "   Failed: $($testRun.failed)" -ForegroundColor Red
                Write-Host "   Duration: $($testRun.duration)s" -ForegroundColor White
            }
        }
        catch {
            Write-Host "❌ Could not parse test results XML" -ForegroundColor Red
        }
    }
    else {
        Write-Host "❌ Test results file not found" -ForegroundColor Red
    }

}
catch {
    Write-Host "❌ Error running Unity tests: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "" -ForegroundColor White
Write-Host "🎯 Test run complete!" -ForegroundColor Green
