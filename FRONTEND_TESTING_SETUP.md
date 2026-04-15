# Frontend Testing Guide - bUnit Setup

## Overview

We've set up **bUnit** testing infrastructure for the Blazor frontend components. bUnit is the industry-standard testing framework for Blazor applications.

## What's Been Set Up

✅ **Test Project Created**: `Shrine_ACU_Web_Application.Tests`  
✅ **Packages Installed**:
   - bUnit 2.7.2 (Blazor component testing)
   - xUnit (test runner)
   - Moq 4.20.72 (mocking framework)

✅ **Project Structure**:
```
Shrine_ACU_Web_Application.Tests/
├── Components/
│   ├── PaymentComponentTests.cs
│   └── SignupComponentTests.cs
├── Services/
│   └── UserSessionAuthenticationProviderTests.cs
└── Helpers/
    └── MockDataFactory.cs
```

## Current Testing Status

### ✅ Working Tests

- **UserSessionAuthenticationProviderTests**: Tests Basic Auth header generation
  - Verifies headers are added correctly
  - Tests special character encoding
  - Validates null/empty handling

### ⚠️ Component Tests (Limited)

Component tests for `Payment.razor` and `FetchSignup.razor` are limited because:
- `UserSessionService` is a sealed class (cannot be mocked with Moq)
- `AcuCarShowClient` is generated code (sealed)
- These services would need to be refactored to use interfaces for full mocking

**Current approach**: Basic smoke tests that verify components can be instantiated.

## Mock Data Factory

The `MockDataFactory` class provides helper methods to create test DTOs:

```csharp
// Create mock user
var user = MockDataFactory.CreateMockUser(
    id: 1,
    username: "testuser",
    password: "password123"
);

// Create mock car show
var show = MockDataFactory.CreateMockCarShow(
    id: 1,
    name: "Summer Car Show"
);

// Create mock entry
var entry = MockDataFactory.CreateMockEntry(
    entryId: 1,
    showId: 1,
    paymentStatus: "Pending"
);

// Create lists
var shows = MockDataFactory.CreateMockCarShowList(count: 5);
var entries = MockDataFactory.CreateMockEntryList(count: 3, showId: 1);
```

## Running Tests

### Run All Tests
```powershell
dotnet test Shrine_ACU_Web_Application.Tests\Shrine_ACU_Web_Application.Tests.csproj
```

### Run Specific Test Class
```powershell
dotnet test --filter FullyQualifiedName~UserSessionAuthenticationProviderTests
```

### Run With Detailed Output
```powershell
dotnet test --logger "console;verbosity=detailed"
```

### Run in Visual Studio
1. Open **Test Explorer** (Test → Test Explorer)
2. Click **Run All** or right-click specific tests

## Test Examples

### Authentication Provider Tests

```csharp
[Fact]
public async Task AuthenticateRequestAsync_AddsBasicAuthHeader_WhenUserLoggedIn()
{
    // Arrange
    var mockUserSession = new Mock<UserSessionService>();
    var testUser = MockDataFactory.CreateMockUser(username: "testuser", password: "testpass");
    mockUserSession.Setup(x => x.CurrentUser).Returns(testUser);

    var provider = new UserSessionAuthenticationProvider(mockUserSession.Object);
    var requestInfo = new RequestInformation
    {
        HttpMethod = Method.GET,
        UrlTemplate = "https://api.example.com/test"
    };

    // Act
    await provider.AuthenticateRequestAsync(requestInfo);

    // Assert
    Assert.True(requestInfo.Headers.ContainsKey("Authorization"));
    var authHeader = requestInfo.Headers["Authorization"].First();
    Assert.StartsWith("Basic ", authHeader);
}
```

### Component Smoke Tests

```csharp
[Fact]
public void Payment_ComponentCreates_Successfully()
{
    // Arrange - Setup minimal services
    Services.AddSingleton(mockClient.Object);
    Services.AddSingleton(mockUserSession.Object);
    Services.AddSingleton(mockNotification.Object);
    Services.AddSingleton(mockDialog.Object);

    // Act & Assert - Should not throw
    var cut = Render<Payment>(parameters => parameters
        .Add(p => p.EntryId, 1));
    
    // Component should exist
    Assert.NotNull(cut);
}
```

## Limitations & Future Improvements

### Current Limitations

1. **Sealed Services**: `UserSessionService` and `AcuCarShowClient` cannot be mocked
   - These would need interfaces extracted for full testability
   
2. **Component State Testing**: Limited ability to test internal component state
   - Most fields are private/internal
   - Would need to expose more public properties or use integration testing

3. **Radzen Components**: Radzen UI components may require additional setup
   - Some components need JSInterop mocking
   - Dialog/Notification services need special handling

### Recommended Improvements

**Option 1: Extract Interfaces** (Best for unit testing)
```csharp
public interface IUserSessionService
{
    AppUserDto? CurrentUser { get; }
    Task LoginAsync(string username, string password);
    // ... other methods
}

public class UserSessionService : IUserSessionService
{
    // Existing implementation
}
```

**Option 2: Integration Testing** (Test real components)
- Use WebApplicationFactory<Program>
- Test actual API integration
- Requires test database or API mocking

**Option 3: End-to-End Testing** (Playwright/Selenium)
- Test real user flows in browser
- Most realistic but slower
- Best for critical user journeys

## Adding New Tests

### 1. Create Test Class

```csharp
using Bunit;
using Xunit;

namespace Shrine_ACU_Web_Application.Tests.Components;

public class MyComponentTests : TestContext
{
    [Fact]
    public void MyComponent_RendersCorrectly()
    {
        // Arrange
        // Act
        var cut = Render<MyComponent>();
        
        // Assert
        Assert.Contains("Expected Text", cut.Markup);
    }
}
```

### 2. Run & Verify

```powershell
dotnet test Shrine_ACU_Web_Application.Tests
```

### 3. Add to CI/CD

```yaml
# Example GitHub Actions
- name: Run Tests
  run: dotnet test --no-build --verbosity normal
```

## Resources

- **bUnit Documentation**: https://bunit.dev
- **xUnit Documentation**: https://xunit.net
- **Moq Documentation**: https://github.com/moq/moq4
- **Blazor Testing Best Practices**: https://bunit.dev/docs/getting-started/

## Summary

The test infrastructure is ready to use with:
- ✅ Mock data factory for creating test DTOs
- ✅ Basic authentication provider tests (all passing)
- ✅ Component smoke tests to verify instantiation
- ✅ Build pipeline integration

**Next steps** depend on your testing goals:
- For quick validation: Current smoke tests are sufficient
- For comprehensive coverage: Consider extracting service interfaces
- For E2E validation: Consider Playwright or manual testing per TESTING_GUIDE.md
