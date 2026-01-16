# Fathom OS Equipment System - Developer Onboarding Guide
## Getting Started for New Developers

---

## Welcome! 👋

This guide will help you get up and running with the Fathom OS Equipment Management System codebase. By the end, you'll have a local development environment and understand the project architecture.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Repository Structure](#repository-structure)
3. [Local Development Setup](#local-development-setup)
4. [Architecture Overview](#architecture-overview)
5. [Development Workflows](#development-workflows)
6. [Coding Standards](#coding-standards)
7. [Testing](#testing)
8. [Useful Resources](#useful-resources)

---

## Prerequisites

### Required Software

| Software | Version | Purpose |
|----------|---------|---------|
| Git | Latest | Version control |
| Docker Desktop | Latest | Container runtime |
| Node.js | 18+ | Mobile development |
| .NET SDK | 8.0 | API development (optional) |
| VS Code | Latest | Recommended IDE |
| Android Studio | Latest | Android emulator |
| Xcode | 15+ | iOS simulator (macOS only) |

### VS Code Extensions

Install these for the best experience:

```
- C# Dev Kit (Microsoft)
- ESLint
- Prettier
- TypeScript + JavaScript
- React Native Tools
- Docker
- GitLens
- REST Client
```

---

## Repository Structure

```
s7-fathom-equipment/
├── api/                          # Backend API (ASP.NET Core 8.0)
│   ├── src/
│   │   ├── FathomOS.Api/         # Web API project
│   │   │   ├── Controllers/      # API endpoints
│   │   │   ├── DTOs/             # Data transfer objects
│   │   │   ├── Middleware/       # Custom middleware
│   │   │   ├── Services/         # Business logic
│   │   │   └── Program.cs        # Application entry point
│   │   ├── FathomOS.Core/        # Domain layer
│   │   │   ├── Entities/         # Domain models
│   │   │   ├── Enums/            # Enumerations
│   │   │   └── Interfaces/       # Contracts
│   │   └── FathomOS.Infrastructure/  # Data access layer
│   │       ├── Data/             # DbContext
│   │       ├── Repositories/     # Repository implementations
│   │       └── Services/         # External services
│   ├── scripts/                  # Database scripts
│   └── Dockerfile                # API container definition
│
├── mobile/                       # Mobile App (React Native)
│   ├── src/
│   │   ├── components/           # Reusable UI components
│   │   ├── screens/              # Application screens
│   │   ├── navigation/           # Navigation configuration
│   │   ├── store/                # Redux state management
│   │   ├── services/             # API and sync services
│   │   ├── database/             # WatermelonDB models
│   │   ├── utils/                # Utility functions
│   │   └── types/                # TypeScript definitions
│   ├── ios/                      # iOS native project
│   ├── android/                  # Android native project
│   └── package.json              # Node dependencies
│
├── docs/                         # Documentation
├── scripts/                      # Deployment scripts
├── docker-compose.yml            # Development environment
└── README.md                     # Project overview
```

---

## Local Development Setup

### Step 1: Clone Repository

```bash
git clone https://github.com/your-org/s7-fathom-equipment.git
cd s7-fathom-equipment
```

### Step 2: Start Backend Services

```bash
# Start PostgreSQL and API with Docker
./scripts/deploy.sh deploy

# Wait for services to start (about 30 seconds)
# Verify at: http://localhost:5000/health
```

**Default Credentials:**
- Admin Username: `admin`
- Admin Password: `Admin@123`

### Step 3: Setup Mobile Development

```bash
# Navigate to mobile directory
cd mobile

# Install dependencies
npm install

# iOS only: Install CocoaPods
cd ios && pod install && cd ..

# Start Metro bundler
npm start
```

### Step 4: Run Mobile App

**iOS (macOS only):**
```bash
npm run ios
# Or open ios/FathomOSEquipment.xcworkspace in Xcode
```

**Android:**
```bash
npm run android
# Or open android/ folder in Android Studio
```

### Step 5: Verify Setup

1. Open mobile app on simulator/emulator
2. Login with admin credentials
3. Navigate through screens
4. Verify data loads from API

---

## Architecture Overview

### Backend Architecture (Clean Architecture)

```
┌─────────────────────────────────────────────────────────────┐
│                        API Layer                            │
│  Controllers → DTOs → Validation → Response                 │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     Application Layer                        │
│  Services → Business Logic → Use Cases                      │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                       Domain Layer                           │
│  Entities → Interfaces → Enums                              │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                       │
│  Repositories → DbContext → External Services               │
└─────────────────────────────────────────────────────────────┘
```

**Key Principles:**
- Dependencies flow inward (outer layers depend on inner)
- Domain layer has no external dependencies
- Infrastructure implements interfaces defined in Core

### Mobile Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                       UI Layer                               │
│  Screens → Components → Navigation                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     State Layer (Redux)                      │
│  Slices → Actions → Selectors                               │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     Service Layer                            │
│  API Service → Sync Service → Photo Service                 │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     Data Layer                               │
│  WatermelonDB → Models → Migrations                         │
└─────────────────────────────────────────────────────────────┘
```

**Offline-First Strategy:**
1. All reads come from local WatermelonDB
2. Writes go to local DB + sync queue
3. Sync service pushes/pulls changes when online
4. Conflict resolution handles concurrent edits

### Data Flow

```
Mobile App                    API Server                    Database
    │                             │                             │
    │──── POST /auth/login ──────>│                             │
    │<─── JWT Token ──────────────│                             │
    │                             │                             │
    │──── GET /equipment ────────>│                             │
    │                             │──── SELECT * FROM... ──────>│
    │                             │<─── Equipment rows ─────────│
    │<─── Equipment JSON ─────────│                             │
    │                             │                             │
    │ (Store in WatermelonDB)     │                             │
    │                             │                             │
    │──── POST /sync/push ───────>│                             │
    │                             │──── INSERT/UPDATE... ──────>│
    │                             │<─── Affected rows ──────────│
    │<─── Sync result ────────────│                             │
```

---

## Development Workflows

### Adding a New API Endpoint

1. **Create/Update Entity** in `FathomOS.Core/Entities/`
2. **Add to DbContext** in `FathomOS.Infrastructure/Data/FathomOSDbContext.cs`
3. **Create DTO** in `FathomOS.Api/DTOs/`
4. **Add Repository Method** in `FathomOS.Infrastructure/Repositories/`
5. **Create Controller Action** in `FathomOS.Api/Controllers/`
6. **Add Unit Tests**
7. **Update Swagger documentation**

**Example: Adding a new endpoint**

```csharp
// 1. DTO (DTOs/VesselDTOs.cs)
public class VesselDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string IMONumber { get; set; }
}

// 2. Controller (Controllers/VesselsController.cs)
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VesselsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VesselDto>>> GetAll()
    {
        var vessels = await _unitOfWork.Vessels.GetAllAsync();
        return Ok(vessels.Select(MapToDto));
    }
}
```

### Adding a New Mobile Screen

1. **Create Screen Component** in `src/screens/`
2. **Add Navigation Route** in `src/navigation/`
3. **Create Redux Slice** (if needed) in `src/store/slices/`
4. **Update Types** in `src/types/`
5. **Add to Navigation Stack**

**Example: Adding a new screen**

```typescript
// 1. Screen (src/screens/VesselListScreen.tsx)
import React, { useEffect } from 'react';
import { View, FlatList } from 'react-native';
import { useAppSelector, useAppDispatch } from '../store/hooks';
import { fetchVessels } from '../store/slices/vesselsSlice';

export const VesselListScreen: React.FC = () => {
  const dispatch = useAppDispatch();
  const vessels = useAppSelector(state => state.vessels.items);

  useEffect(() => {
    dispatch(fetchVessels());
  }, []);

  return (
    <View>
      <FlatList
        data={vessels}
        renderItem={({ item }) => <VesselCard vessel={item} />}
        keyExtractor={item => item.id}
      />
    </View>
  );
};

// 2. Add to navigation (src/navigation/MainNavigator.tsx)
<Stack.Screen name="VesselList" component={VesselListScreen} />
```

### Database Migrations

**Adding a new column:**

```sql
-- Create migration file: migrations/YYYYMMDD_add_vessel_flag.sql
ALTER TABLE Vessels ADD COLUMN Flag VARCHAR(50);

-- Update init-db.sql for fresh installs
```

**WatermelonDB Migration:**

```typescript
// src/database/migrations.ts
export const migrations = schemaMigrations({
  migrations: [
    {
      toVersion: 2,
      steps: [
        addColumns({
          table: 'vessels',
          columns: [{ name: 'flag', type: 'string', isOptional: true }],
        }),
      ],
    },
  ],
});
```

---

## Coding Standards

### C# Conventions

```csharp
// Use PascalCase for public members
public class EquipmentService
{
    // Use camelCase for private fields with underscore prefix
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EquipmentService> _logger;

    // Constructor injection
    public EquipmentService(IUnitOfWork unitOfWork, ILogger<EquipmentService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // Async methods end with Async
    public async Task<Equipment> GetEquipmentByIdAsync(Guid id)
    {
        return await _unitOfWork.Equipment.GetByIdAsync(id);
    }

    // Use expression-bodied members for simple methods
    public bool IsValid(Equipment equipment) => equipment != null && equipment.IsActive;
}
```

### TypeScript/React Native Conventions

```typescript
// Use PascalCase for components and types
interface EquipmentCardProps {
  equipment: Equipment;
  onPress: () => void;
}

// Use functional components with hooks
export const EquipmentCard: React.FC<EquipmentCardProps> = ({
  equipment,
  onPress,
}) => {
  // Use camelCase for variables and functions
  const [isLoading, setIsLoading] = useState(false);

  // Use useCallback for event handlers
  const handlePress = useCallback(() => {
    setIsLoading(true);
    onPress();
  }, [onPress]);

  return (
    <TouchableOpacity onPress={handlePress}>
      <Text>{equipment.name}</Text>
    </TouchableOpacity>
  );
};

// Export types separately
export type { EquipmentCardProps };
```

### Git Commit Messages

Follow conventional commits:

```
feat: add vessel management endpoints
fix: resolve sync conflict detection issue
docs: update API documentation
refactor: extract equipment validation logic
test: add unit tests for manifest service
chore: update dependencies
```

### Branch Naming

```
feature/add-vessel-management
bugfix/sync-conflict-resolution
hotfix/login-token-expiry
docs/api-documentation-update
```

---

## Testing

### API Unit Tests

```csharp
// Tests/EquipmentServiceTests.cs
public class EquipmentServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly EquipmentService _service;

    public EquipmentServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _service = new EquipmentService(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task GetById_ExistingEquipment_ReturnsEquipment()
    {
        // Arrange
        var equipmentId = Guid.NewGuid();
        var expected = new Equipment { Id = equipmentId, Name = "Test" };
        _mockUnitOfWork.Setup(x => x.Equipment.GetByIdAsync(equipmentId))
            .ReturnsAsync(expected);

        // Act
        var result = await _service.GetEquipmentByIdAsync(equipmentId);

        // Assert
        Assert.Equal(expected.Id, result.Id);
    }
}
```

### Mobile Component Tests

```typescript
// __tests__/EquipmentCard.test.tsx
import { render, fireEvent } from '@testing-library/react-native';
import { EquipmentCard } from '../src/components/EquipmentCard';

describe('EquipmentCard', () => {
  const mockEquipment = {
    id: '123',
    name: 'ROV Camera',
    assetNumber: 'EQ-2024-00001',
  };

  it('renders equipment name', () => {
    const { getByText } = render(
      <EquipmentCard equipment={mockEquipment} onPress={() => {}} />
    );
    expect(getByText('ROV Camera')).toBeTruthy();
  });

  it('calls onPress when tapped', () => {
    const onPress = jest.fn();
    const { getByTestId } = render(
      <EquipmentCard equipment={mockEquipment} onPress={onPress} />
    );
    fireEvent.press(getByTestId('equipment-card'));
    expect(onPress).toHaveBeenCalled();
  });
});
```

### Running Tests

```bash
# API Tests
cd api
dotnet test

# Mobile Tests
cd mobile
npm test

# With coverage
npm test -- --coverage
```

---

## Useful Resources

### Internal Documentation

- [API Documentation](./API_DOCUMENTATION.md)
- [Deployment Guide](./DEPLOYMENT_GUIDE.md)
- [System Architecture](../SYSTEM_ARCHITECTURE.md)
- [Mobile Requirements](../MOBILE_APP_REQUIREMENTS.md)

### External Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [React Native Documentation](https://reactnative.dev/docs/getting-started)
- [WatermelonDB](https://nozbe.github.io/WatermelonDB/)
- [Redux Toolkit](https://redux-toolkit.js.org/)

### Team Contacts

| Role | Contact |
|------|---------|
| Tech Lead | tech-lead@company.com |
| Backend Lead | backend@company.com |
| Mobile Lead | mobile@company.com |
| DevOps | devops@company.com |

### Slack Channels

- `#s7-fathom-dev` - General development
- `#s7-fathom-backend` - API discussions
- `#s7-fathom-mobile` - Mobile app discussions
- `#s7-fathom-support` - Production issues

---

## Quick Reference Commands

```bash
# Start all services
./scripts/deploy.sh deploy

# Stop all services
./scripts/deploy.sh stop

# View logs
./scripts/deploy.sh logs

# Run API tests
./scripts/test-api.sh

# Mobile development
cd mobile
npm start           # Start Metro
npm run ios         # Run iOS
npm run android     # Run Android
npm run typecheck   # Check TypeScript
npm test            # Run tests

# Database access
docker exec -it s7fathom-postgres psql -U s7fathom -d s7fathom_equipment
```

---

**Welcome to the team! 🎉**

If you have questions, don't hesitate to ask in Slack or reach out to your team lead.

---

**Document Version:** 1.0  
**Last Updated:** January 2025
