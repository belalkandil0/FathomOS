<# 
.SYNOPSIS
    Fathom OS Release Build Script with Obfuscation
    
.DESCRIPTION
    This script builds Fathom OS in Release mode and applies obfuscation
    using either ConfuserEx, Dotfuscator Community, or both.
    
.PARAMETER ObfuscationTool
    Which obfuscation tool to use: "ConfuserEx", "Dotfuscator", "Both", or "None"
    
.EXAMPLE
    .\Build-Release.ps1 -ObfuscationTool ConfuserEx
    .\Build-Release.ps1 -ObfuscationTool Dotfuscator
    .\Build-Release.ps1 -ObfuscationTool Both
#>

param(
    [ValidateSet("ConfuserEx", "Dotfuscator", "Both", "None")]
    [string]$ObfuscationTool = "ConfuserEx"
)

$ErrorActionPreference = "Stop"

# Configuration
$SolutionDir = $PSScriptRoot
$Configuration = "Release"
$OutputDir = Join-Path $SolutionDir "FathomOS.Shell\bin\Release\net8.0-windows"
$ConfuserExPath = "C:\Tools\ConfuserEx\Confuser.CLI.exe"  # UPDATE THIS PATH
$DotfuscatorPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\PreEmptiveSolutions\DotfuscatorCE\dotfuscator.exe"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Fathom OS Release Build Script" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean previous builds
Write-Host "[1/5] Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean "$SolutionDir\FathomOS.sln" -c $Configuration --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Error "Clean failed!"
    exit 1
}
Write-Host "      Done!" -ForegroundColor Green

# Step 2: Restore packages
Write-Host "[2/5] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore "$SolutionDir\FathomOS.sln" --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Error "Restore failed!"
    exit 1
}
Write-Host "      Done!" -ForegroundColor Green

# Step 3: Build in Release mode
Write-Host "[3/5] Building in Release mode..." -ForegroundColor Yellow
dotnet build "$SolutionDir\FathomOS.sln" -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}
Write-Host "      Done!" -ForegroundColor Green

# Step 4: Run module deployment script
Write-Host "[4/5] Deploying modules..." -ForegroundColor Yellow
$DeployScript = Join-Path $SolutionDir "FathomOS.Shell\deploy-modules.ps1"
if (Test-Path $DeployScript) {
    & $DeployScript
}
Write-Host "      Done!" -ForegroundColor Green

# Step 5: Apply obfuscation
Write-Host "[5/5] Applying obfuscation ($ObfuscationTool)..." -ForegroundColor Yellow

switch ($ObfuscationTool) {
    "ConfuserEx" {
        if (Test-Path $ConfuserExPath) {
            Write-Host "      Running ConfuserEx..." -ForegroundColor Cyan
            & $ConfuserExPath "$SolutionDir\FathomOS.crproj"
            if ($LASTEXITCODE -eq 0) {
                Write-Host "      ConfuserEx completed!" -ForegroundColor Green
                Write-Host "      Output: $SolutionDir\Confused\" -ForegroundColor Gray
            } else {
                Write-Warning "ConfuserEx failed. Continuing with unobfuscated build."
            }
        } else {
            Write-Warning "ConfuserEx not found at: $ConfuserExPath"
            Write-Host "      Download from: https://github.com/mkaring/ConfuserEx/releases" -ForegroundColor Gray
            Write-Host "      Skipping obfuscation..." -ForegroundColor Yellow
        }
    }
    
    "Dotfuscator" {
        if (Test-Path $DotfuscatorPath) {
            Write-Host "      Running Dotfuscator Community..." -ForegroundColor Cyan
            & $DotfuscatorPath "$SolutionDir\FathomOS.Dotfuscator.xml"
            if ($LASTEXITCODE -eq 0) {
                Write-Host "      Dotfuscator completed!" -ForegroundColor Green
                Write-Host "      Output: $OutputDir\Dotfuscated\" -ForegroundColor Gray
            } else {
                Write-Warning "Dotfuscator failed. Continuing with unobfuscated build."
            }
        } else {
            Write-Warning "Dotfuscator not found at: $DotfuscatorPath"
            Write-Host "      Dotfuscator Community comes with Visual Studio." -ForegroundColor Gray
            Write-Host "      Tools > PreEmptive Protection - Dotfuscator Community" -ForegroundColor Gray
            Write-Host "      Skipping obfuscation..." -ForegroundColor Yellow
        }
    }
    
    "Both" {
        # Run ConfuserEx first
        if (Test-Path $ConfuserExPath) {
            Write-Host "      Running ConfuserEx..." -ForegroundColor Cyan
            & $ConfuserExPath "$SolutionDir\FathomOS.crproj"
            if ($LASTEXITCODE -eq 0) {
                Write-Host "      ConfuserEx completed!" -ForegroundColor Green
            }
        } else {
            Write-Warning "ConfuserEx not found. Skipping..."
        }
        
        # Then run Dotfuscator
        if (Test-Path $DotfuscatorPath) {
            Write-Host "      Running Dotfuscator Community..." -ForegroundColor Cyan
            & $DotfuscatorPath "$SolutionDir\FathomOS.Dotfuscator.xml"
            if ($LASTEXITCODE -eq 0) {
                Write-Host "      Dotfuscator completed!" -ForegroundColor Green
            }
        } else {
            Write-Warning "Dotfuscator not found. Skipping..."
        }
    }
    
    "None" {
        Write-Host "      Skipping obfuscation (as requested)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output locations:" -ForegroundColor White
Write-Host "  Unobfuscated: $OutputDir" -ForegroundColor Gray
if ($ObfuscationTool -eq "ConfuserEx" -or $ObfuscationTool -eq "Both") {
    Write-Host "  ConfuserEx:   $SolutionDir\Confused\" -ForegroundColor Gray
}
if ($ObfuscationTool -eq "Dotfuscator" -or $ObfuscationTool -eq "Both") {
    Write-Host "  Dotfuscator:  $OutputDir\Dotfuscated\" -ForegroundColor Gray
}
Write-Host ""
Write-Host "IMPORTANT: For distribution, use the OBFUSCATED build!" -ForegroundColor Yellow
