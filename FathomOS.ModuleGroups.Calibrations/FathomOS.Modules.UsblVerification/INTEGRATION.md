# USBL Verification Module - Integration Instructions

## Step 1: Extract and Place the Module

Extract the contents of this zip file and place the `FathomOS.Modules.UsblVerification` folder under the Calibrations group:

```
FathomOS/
├── DEV/
│   ├── FathomOS.Core/
│   ├── FathomOS.Shell/
│   └── FathomOS.ModuleGroups.Calibrations/
│       ├── GroupInfo.json
│       ├── FathomOS.Modules.GnssCalibration/
│       ├── FathomOS.Modules.MruCalibration/
│       ├── FathomOS.Modules.RovGyroCalibration/
│       └── FathomOS.Modules.UsblVerification/    <-- YOUR MODULE HERE
```

**Alternative: Root Module Location**
If you want to place it as a root module instead:
```
FathomOS/
├── DEV/
│   ├── FathomOS.Core/
│   ├── FathomOS.Shell/
│   ├── FathomOS.Modules.SurveyListing/
│   ├── FathomOS.Modules.UsblVerification/    <-- OR HERE
│   └── ...
```

**⚠️ IMPORTANT:** If using root module location, edit the .csproj file and change the ProjectReference path:
- Comment out: `<ProjectReference Include="..\..\FathomOS.Core\FathomOS.Core.csproj" />`
- Uncomment: `<ProjectReference Include="..\FathomOS.Core\FathomOS.Core.csproj" />`

## Step 2: Add to Solution

Open a command prompt in the solution root folder and run:

```bash
dotnet sln FathomOS.sln add DEV\FathomOS.ModuleGroups.Calibrations\FathomOS.Modules.UsblVerification\FathomOS.Modules.UsblVerification.csproj
```

Or in Visual Studio:
1. Right-click on the solution in Solution Explorer
2. Select "Add" → "Existing Project..."
3. Navigate to `DEV\FathomOS.ModuleGroups.Calibrations\FathomOS.Modules.UsblVerification\`
4. Select `FathomOS.Modules.UsblVerification.csproj`

## Step 3: Restore and Build

```bash
dotnet restore
dotnet build
```

Or in Visual Studio: Build → Rebuild Solution

## Troubleshooting

### Error: "Unable to find project FathomOS.Core.csproj"
- **Check the path in .csproj** - The default is for GROUPED module location (`..\..\FathomOS.Core\`)
- If in ROOT location, change to `..\FathomOS.Core\`
- Verify FathomOS.Core exists in the expected location

### Error: "The type or namespace name 'Core' does not exist"
- Ensure FathomOS.Core builds first
- Run `dotnet restore` in the solution folder

### Error: "MetroWindow does not exist in XML namespace"
- This is caused by Core reference not resolving
- Fix the Core project path first, then rebuild

### Error: "Metadata file could not be found"
- Clean the solution: `dotnet clean`
- Delete `bin` and `obj` folders in the module directory
- Rebuild: `dotnet build`

### Module not appearing on Dashboard
- Verify ModuleInfo.json exists in the module folder
- Check that moduleId matches the DLL name ("UsblVerification")
- Ensure the build deploys to `bin/Modules/_Groups/Calibrations/UsblVerification/`
