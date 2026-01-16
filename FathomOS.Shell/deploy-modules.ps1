# deploy-modules.ps1
# ============================================================================
# AUTOMATIC MODULE & GROUP DEPLOYMENT SCRIPT
# ============================================================================
# Deploys:
# 1. Root modules: FathomOS.Modules.* at solution root
# 2. Grouped modules: FathomOS.ModuleGroups.*\FathomOS.Modules.*
# 3. Group metadata: GroupInfo.json and icons for each group
# ============================================================================

param(
    [Parameter(Mandatory=$true)]
    [string]$OutputPath,
    
    [Parameter(Mandatory=$true)]
    [string]$SolutionDir
)

$OutputPath = $OutputPath.TrimEnd('\', '/')
$SolutionDir = $SolutionDir.TrimEnd('\', '/')

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  Fathom OS - Module Deployment" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "Output:   $OutputPath"
Write-Host "Solution: $SolutionDir"
Write-Host ""

# Create base Modules folder
$modulesBaseDir = Join-Path $OutputPath "Modules"
if (-not (Test-Path $modulesBaseDir)) {
    New-Item -ItemType Directory -Force -Path $modulesBaseDir | Out-Null
}

$deployedModules = 0
$deployedGroups = 0

# ============================================================================
# STEP 1: Deploy Root Modules (FathomOS.Modules.* at solution root)
# ============================================================================
Write-Host "--- Root Modules ---" -ForegroundColor Yellow

$moduleDlls = Get-ChildItem -Path $OutputPath -Filter 'FathomOS.Modules.*.dll' -File -ErrorAction SilentlyContinue

foreach ($dll in $moduleDlls) {
    $moduleName = $dll.BaseName -replace '^FathomOS\.Modules\.', ''
    
    # Check if this is a root module (source folder exists at solution root)
    $moduleSourceDir = Join-Path $SolutionDir "FathomOS.Modules.$moduleName"
    if (-not (Test-Path $moduleSourceDir)) {
        # Not a root module, will be handled as grouped module
        continue
    }
    
    Write-Host "  [$moduleName]" -ForegroundColor White
    
    $moduleDir = Join-Path $modulesBaseDir $moduleName
    $assetsDir = Join-Path $moduleDir "Assets"
    
    if (-not (Test-Path $moduleDir)) { New-Item -ItemType Directory -Force -Path $moduleDir | Out-Null }
    if (-not (Test-Path $assetsDir)) { New-Item -ItemType Directory -Force -Path $assetsDir | Out-Null }
    
    # Copy DLL
    Copy-Item $dll.FullName -Destination $moduleDir -Force
    Write-Host "    [OK] DLL" -ForegroundColor DarkGray
    
    # Copy ModuleInfo.json
    $moduleInfoPath = Join-Path $moduleSourceDir "ModuleInfo.json"
    if (Test-Path $moduleInfoPath) {
        Copy-Item $moduleInfoPath -Destination $moduleDir -Force
        Write-Host "    [OK] ModuleInfo.json" -ForegroundColor DarkGray
    }
    
    # Copy icon.png
    $assetsSourceDir = Join-Path $moduleSourceDir "Assets"
    $iconPath = Join-Path $assetsSourceDir "icon.png"
    if (Test-Path $iconPath) {
        Copy-Item $iconPath -Destination $assetsDir -Force
        Write-Host "    [OK] icon.png" -ForegroundColor DarkGray
    }
    
    $deployedModules++
    Write-Host "    [DEPLOYED]" -ForegroundColor Green
}

# ============================================================================
# STEP 2: Deploy Module Groups (FathomOS.ModuleGroups.*)
# ============================================================================
Write-Host ""
Write-Host "--- Module Groups ---" -ForegroundColor Yellow

$groupFolders = Get-ChildItem -Path $SolutionDir -Directory -Filter 'FathomOS.ModuleGroups.*' -ErrorAction SilentlyContinue

foreach ($groupFolder in $groupFolders) {
    $groupName = $groupFolder.Name -replace '^FathomOS\.ModuleGroups\.', ''
    
    Write-Host "  [GROUP: $groupName]" -ForegroundColor Magenta
    
    # Create group folder in output: Modules/_Groups/Calibrations/
    $groupsBaseDir = Join-Path $modulesBaseDir "_Groups"
    $groupOutputDir = Join-Path $groupsBaseDir $groupName
    $groupAssetsDir = Join-Path $groupOutputDir "Assets"
    
    if (-not (Test-Path $groupOutputDir)) { New-Item -ItemType Directory -Force -Path $groupOutputDir | Out-Null }
    if (-not (Test-Path $groupAssetsDir)) { New-Item -ItemType Directory -Force -Path $groupAssetsDir | Out-Null }
    
    # Copy GroupInfo.json
    $groupInfoPath = Join-Path $groupFolder.FullName "GroupInfo.json"
    if (Test-Path $groupInfoPath) {
        Copy-Item $groupInfoPath -Destination $groupOutputDir -Force
        Write-Host "    [OK] GroupInfo.json" -ForegroundColor DarkGray
    } else {
        Write-Host "    [WARN] GroupInfo.json not found!" -ForegroundColor Yellow
    }
    
    # Copy group icon
    $groupIconPath = Join-Path $groupFolder.FullName "icon.png"
    if (Test-Path $groupIconPath) {
        Copy-Item $groupIconPath -Destination $groupAssetsDir -Force
        Write-Host "    [OK] icon.png" -ForegroundColor DarkGray
    }
    
    $deployedGroups++
    
    # Deploy modules inside this group
    $groupModuleFolders = Get-ChildItem -Path $groupFolder.FullName -Directory -Filter 'FathomOS.Modules.*' -ErrorAction SilentlyContinue
    
    foreach ($moduleFolder in $groupModuleFolders) {
        $moduleName = $moduleFolder.Name -replace '^FathomOS\.Modules\.', ''
        
        Write-Host "    [$moduleName]" -ForegroundColor White
        
        # Find the DLL in output
        $dllName = "FathomOS.Modules.$moduleName.dll"
        $dllPath = Join-Path $OutputPath $dllName
        
        if (-not (Test-Path $dllPath)) {
            Write-Host "      [SKIP] DLL not found" -ForegroundColor Yellow
            continue
        }
        
        # Create module folder under the group
        $moduleDir = Join-Path $groupOutputDir $moduleName
        $assetsDir = Join-Path $moduleDir "Assets"
        
        if (-not (Test-Path $moduleDir)) { New-Item -ItemType Directory -Force -Path $moduleDir | Out-Null }
        if (-not (Test-Path $assetsDir)) { New-Item -ItemType Directory -Force -Path $assetsDir | Out-Null }
        
        # Copy DLL
        Copy-Item $dllPath -Destination $moduleDir -Force
        Write-Host "      [OK] DLL" -ForegroundColor DarkGray
        
        # Copy ModuleInfo.json
        $moduleInfoPath = Join-Path $moduleFolder.FullName "ModuleInfo.json"
        if (Test-Path $moduleInfoPath) {
            Copy-Item $moduleInfoPath -Destination $moduleDir -Force
            Write-Host "      [OK] ModuleInfo.json" -ForegroundColor DarkGray
        }
        
        # Copy icon.png
        $moduleAssetsDir = Join-Path $moduleFolder.FullName "Assets"
        $iconPath = Join-Path $moduleAssetsDir "icon.png"
        if (Test-Path $iconPath) {
            Copy-Item $iconPath -Destination $assetsDir -Force
            Write-Host "      [OK] icon.png" -ForegroundColor DarkGray
        }
        
        $deployedModules++
        Write-Host "      [DEPLOYED]" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  Deployment Complete" -ForegroundColor Cyan
Write-Host "  Modules: $deployedModules | Groups: $deployedGroups" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

exit 0
