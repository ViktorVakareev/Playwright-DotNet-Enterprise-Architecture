using Bogus;

namespace WorldBank.Automation.Tests.Data;

public static class DataFactory
{
    public static UserProfile CreateTestUser() =>
        new Faker<UserProfile>()
            .RuleFor(u => u.Id, f => $"WB-{f.Random.Number(1000, 9999)}")
            .RuleFor(u => u.Email, f => f.Internet.Email(provider: "worldbank.test"))
            .Generate();
}

public record UserProfile(string Id, string Email);