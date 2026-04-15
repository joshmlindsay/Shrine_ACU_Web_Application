using AcuCarShowClient.Models;

namespace Shrine_ACU_Web_Application.Tests.Helpers;

/// <summary>
/// Factory for creating mock DTOs for testing purposes
/// Uses actual DTO property names from generated Kiota client
/// </summary>
public static class MockDataFactory
{
    public static AppUserDto CreateMockUser(
        int id = 1,
        string username = "testuser",
        string email = "test@example.com",
        string password = "TestPassword123")
    {
        return new AppUserDto
        {
            UserId = id,
            Username = username,
            Email = email,
            Password = password,
            FirstName = "Test",
            LastName = "User",
            DisplayName = "Test User",
            IsActive = true
        };
    }

    public static CarShowDto CreateMockCarShow(
        int id = 1,
        string name = "Test Car Show")
    {
        return new CarShowDto
        {
            ShowId = id,
            Name = name,
            Description = "Test car show for unit testing",
            EventDate = DateTimeOffset.UtcNow.AddDays(30),
            Active = true
        };
    }

    public static CarShowEntryDto CreateMockEntry(
        int entryId = 1,
        int? showId = 1,
        string paymentStatus = "Pending")
    {
        return new CarShowEntryDto
        {
            EntryId = entryId,
            ShowId = showId,
            PaymentStatus = paymentStatus,
            Status = "Pending"
        };
    }

    public static List<CarShowDto> CreateMockCarShowList(int count = 3)
    {
        var shows = new List<CarShowDto>();
        for (int i = 1; i <= count; i++)
        {
            shows.Add(CreateMockCarShow(
                id: i,
                name: $"Car Show {i}"));
        }
        return shows;
    }

    public static List<CarShowEntryDto> CreateMockEntryList(int count = 3, int showId = 1)
    {
        var entries = new List<CarShowEntryDto>();
        for (int i = 1; i <= count; i++)
        {
            entries.Add(CreateMockEntry(
                entryId: i,
                showId: showId,
                paymentStatus: i % 2 == 0 ? "Completed" : "Pending"));
        }
        return entries;
    }
}
