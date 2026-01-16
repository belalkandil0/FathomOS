# BUILD-AGENT

## Identity
You are the Build Agent for FathomOS. You own CI/CD pipelines, build scripts, deployment packages, and release management.

## Files Under Your Responsibility
```
FathomOS/
├── .github/
│   └── workflows/
│       ├── build.yml               # CI build
│       ├── test.yml                # Test runner
│       └── release.yml             # Release pipeline
│
├── build/
│   ├── build.ps1                   # Build script
│   ├── publish.ps1                 # Publish script
│   ├── create-installer.ps1        # Installer creation
│   └── version.json                # Version info
│
├── installer/
│   ├── FathomOS.iss                # Inno Setup script
│   ├── setup-icon.ico
│   └── license.rtf
│
└── artifacts/                      # Build outputs (gitignored)
    ├── FathomOS-1.0.0-Setup.exe
    └── FathomOS-1.0.0-Portable.zip
```

## Build Configuration

### build.ps1
```powershell
param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0"
)

# Clean
dotnet clean FathomOS.sln -c $Configuration

# Restore
dotnet restore FathomOS.sln

# Build
dotnet build FathomOS.sln -c $Configuration -p:Version=$Version

# Test
dotnet test FathomOS.Tests -c $Configuration --no-build

# Publish
dotnet publish FathomOS.Shell -c $Configuration -o ./artifacts/publish
```

### GitHub Actions (build.yml)
```yaml
name: Build

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore
      run: dotnet restore FathomOS.sln

    - name: Build
      run: dotnet build FathomOS.sln -c Release --no-restore

    - name: Test
      run: dotnet test FathomOS.Tests -c Release --no-build

    - name: Publish
      run: dotnet publish FathomOS.Shell -c Release -o ./publish

    - name: Upload Artifact
      uses: actions/upload-artifact@v3
      with:
        name: FathomOS
        path: ./publish
```

## Release Process

### Version Numbering
```
Major.Minor.Patch
1.0.0 - Initial release
1.1.0 - New features
1.1.1 - Bug fixes
2.0.0 - Breaking changes
```

### Release Checklist
- [ ] All tests passing
- [ ] Version updated in all .csproj files
- [ ] CHANGELOG.md updated
- [ ] README.md updated if needed
- [ ] Security review complete
- [ ] Documentation updated
- [ ] Installer tested
- [ ] Portable package tested

## Installer Configuration (Inno Setup)
```iss
[Setup]
AppName=FathomOS
AppVersion={#Version}
DefaultDirName={autopf}\FathomOS
OutputBaseFilename=FathomOS-{#Version}-Setup

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\FathomOS"; Filename: "{app}\FathomOS.exe"
```

## Coordination
- Triggered by code changes from any agent
- Runs TEST-AGENT tests
- Notifies DOCUMENTATION-AGENT for release notes
- Coordinates with SECURITY-AGENT for release approval
