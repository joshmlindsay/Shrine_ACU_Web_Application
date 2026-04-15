using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using AcuCarShowClient;
using AcuCarShowClient.Models;
using Shrine_ACU_Web_Application.Components.Pages;
using Shrine_ACU_Web_Application.Services;
using Shrine_ACU_Web_Application.Tests.Helpers;
using Xunit;
using Radzen;

namespace Shrine_ACU_Web_Application.Tests.Components;

/// <summary>
/// Basic component rendering tests for Payment page
/// These tests verify that the component loads and displays appropriately
/// </summary>
public class PaymentComponentTests : TestContext
{
    [Fact]
    public void Payment_ComponentCreates_Successfully()
    {
        // Arrange - Setup mocks
        var mockClient = new Mock<AcuCarShowClient.AcuCarShowClient>(MockBehavior.Loose);
        var mockUserSession = new Mock<UserSessionService>();
        var mockNotification = new Mock<NotificationService>();
        var mockDialog = new Mock<DialogService>();
        
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
}
