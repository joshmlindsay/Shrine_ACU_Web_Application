using Microsoft.Kiota.Abstractions;
using Moq;
using Shrine_ACU_Web_Application.Services;
using Shrine_ACU_Web_Application.Tests.Helpers;
using AcuCarShowClient.Models;
using System.Text;
using Xunit;

namespace Shrine_ACU_Web_Application.Tests.Services;

public class UserSessionAuthenticationProviderTests
{
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

        // Verify the encoded credentials
        var encodedCredentials = authHeader.Replace("Basic ", "");
        var decodedBytes = Convert.FromBase64String(encodedCredentials);
        var decodedString = Encoding.UTF8.GetString(decodedBytes);
        Assert.Equal("testuser:testpass", decodedString);
    }

    [Fact]
    public async Task AuthenticateRequestAsync_DoesNotAddHeader_WhenUserNotLoggedIn()
    {
        // Arrange
        var mockUserSession = new Mock<UserSessionService>();
        mockUserSession.Setup(x => x.CurrentUser).Returns((AppUserDto)null);

        var provider = new UserSessionAuthenticationProvider(mockUserSession.Object);
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.GET,
            UrlTemplate = "https://api.example.com/test"
        };

        // Act
        await provider.AuthenticateRequestAsync(requestInfo);

        // Assert
        Assert.False(requestInfo.Headers.ContainsKey("Authorization"));
    }

    [Fact]
    public async Task AuthenticateRequestAsync_DoesNotAddHeader_WhenUsernameIsEmpty()
    {
        // Arrange
        var mockUserSession = new Mock<UserSessionService>();
        var testUser = MockDataFactory.CreateMockUser(username: "", password: "testpass");
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
        Assert.False(requestInfo.Headers.ContainsKey("Authorization"));
    }

    [Fact]
    public async Task AuthenticateRequestAsync_DoesNotAddHeader_WhenPasswordIsEmpty()
    {
        // Arrange
        var mockUserSession = new Mock<UserSessionService>();
        var testUser = MockDataFactory.CreateMockUser(username: "testuser", password: "");
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
        Assert.False(requestInfo.Headers.ContainsKey("Authorization"));
    }

    [Fact]
    public async Task AuthenticateRequestAsync_EncodesSpecialCharacters_InCredentials()
    {
        // Arrange
        var mockUserSession = new Mock<UserSessionService>();
        var testUser = MockDataFactory.CreateMockUser(username: "user@domain.com", password: "p@ss:w0rd!");
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
        
        // Verify the encoded credentials with special characters
        var encodedCredentials = authHeader.Replace("Basic ", "");
        var decodedBytes = Convert.FromBase64String(encodedCredentials);
        var decodedString = Encoding.UTF8.GetString(decodedBytes);
        Assert.Equal("user@domain.com:p@ss:w0rd!", decodedString);
    }

    [Fact]
    public async Task AuthenticateRequestAsync_OverwritesExistingAuthHeader()
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
        
        // Add an existing auth header
        requestInfo.Headers.Add("Authorization", new[] { "Bearer old-token" });

        // Act
        await provider.AuthenticateRequestAsync(requestInfo);

        // Assert
        Assert.True(requestInfo.Headers.ContainsKey("Authorization"));
        var authHeaders = requestInfo.Headers["Authorization"];
        Assert.Equal(2, authHeaders.Count()); // Old and new header
        Assert.Contains(authHeaders, h => h.StartsWith("Basic "));
    }
}
