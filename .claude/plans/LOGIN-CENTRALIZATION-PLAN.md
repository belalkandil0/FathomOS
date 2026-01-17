# LOGIN CENTRALIZATION PLAN

**Status:** APPROVED
**Created:** 2026-01-16
**Author:** R&D-AGENT
**Approved By:** USER

---

## Overview

Centralize user authentication from EquipmentInventory module to FathomOS Shell. Currently, only EquipmentInventory has login - this should be a Shell-level feature.

---

## Current State

**EquipmentInventory Module Has:**
- `Models\User.cs` - User, Role, Permission entities (~155 lines)
- `Services\AuthenticationService.cs` - HTTP + offline auth (~230 lines)
- `Views\LoginWindow.xaml` - Login UI
- `Views\PinLoginDialog.xaml` - PIN login
- `Data\LocalDatabaseService.cs` - User authentication methods

**Other Modules:** No authentication

---

## Proposed Architecture

### Phase 1: Core Interfaces (FathomOS.Core)

**New Files:**
- `Interfaces\IAuthenticationService.cs`
- `Interfaces\IUser.cs`
- `Models\AuthenticatedUser.cs`

```csharp
public interface IAuthenticationService
{
    IUser? CurrentUser { get; }
    bool IsAuthenticated { get; }
    string AccessToken { get; }
    event EventHandler<IUser?>? AuthenticationChanged;

    Task<AuthenticationResult> LoginAsync(string username, string password);
    Task<AuthenticationResult> LoginWithPinAsync(string username, string pin);
    void Logout();
    Task<bool> RefreshTokenAsync();
    bool HasPermission(string permission);
    bool HasRole(params string[] roles);
    Task<bool> ShowLoginDialogAsync(object? owner = null);
}
```

### Phase 2: Shell Implementation

**New Files:**
- `Shell\Services\AuthenticationService.cs`
- `Shell\Views\LoginWindow.xaml`
- `Shell\Views\PinLoginDialog.xaml`
- `Shell\Data\UserDatabaseService.cs`

### Phase 3: DI Registration

```csharp
services.AddSingleton<IAuthenticationService>(sp =>
    new AuthenticationService(
        sp.GetRequiredService<IEventAggregator>(),
        sp.GetRequiredService<ISettingsService>()));
```

### Phase 4: Shell Startup Flow

Show login AFTER license validation, BEFORE dashboard.

### Phase 5: Migrate EquipmentInventory

- Remove local AuthenticationService
- Accept IAuthenticationService via DI
- Use Shell's CurrentUser

### Phase 6: PersonnelManagement Integration

- Personnel entity stays separate (survey crew)
- Use IAuthenticationService.CurrentUser for logged-in user
- Link Personnel.UserId to authenticated user if needed

---

## Benefits

1. **Single Sign-On** - Login once, all modules share session
2. **Consistent Security** - Global password policies
3. **Reduced Code** - ~800 lines consolidated
4. **Easier Maintenance** - Security fixes apply everywhere
5. **Module Simplicity** - Modules don't implement auth

---

## Estimated Effort

| Phase | Effort | Files |
|-------|--------|-------|
| Phase 1: Core Interfaces | Low | 3-4 new |
| Phase 2: Shell Service | Medium | 5-6 new |
| Phase 3: DI Config | Low | 1 modify |
| Phase 4: Startup Flow | Low | 1 modify |
| Phase 5: EquipmentInventory | Medium | 10+ modify |
| Phase 6: PersonnelManagement | Low | 2-3 modify |

**Total: ~2-3 days implementation**

---

## Critical Files

- `FathomOS.Core\Interfaces\IAuthenticationService.cs` (create)
- `FathomOS.Shell\App.xaml.cs` (modify DI + startup)
- `FathomOS.Modules.EquipmentInventory\EquipmentInventoryModule.cs` (modify)
- `FathomOS.Modules.EquipmentInventory\Services\AuthenticationService.cs` (reference then remove)
- `FathomOS.Modules.EquipmentInventory\Models\User.cs` (move to Core)
