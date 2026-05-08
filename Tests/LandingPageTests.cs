using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using WorldBank.Automation.Tests.Data;
using WorldBank.Automation.Tests.Infrastructure;

namespace WorldBank.Automation.Tests.Tests;

[Parallelizable(ParallelScope.Self)]
public class LandingPageTests : AiTriage
{
    [Test]
    public async Task Search_WithSyntheticUser_ShouldLoadResults()
    {
        // 1. Arrange: Use Synthetic Data
        var user = DataFactory.CreateTestUser();

        // 2. Act: Web-First Navigation
        await Page.GotoAsync("https://www.worldbank.org/en/home");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();
        await Page.GetByPlaceholder("Search worldbank.org").FillAsync(user.Id);
        await Page.GetByPlaceholder("Search worldbank.org").PressAsync("Enter");

        // 3. Assert: Web-First Assertions (Auto-waiting)
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Search Results" })).ToBeVisibleAsync();
    }
}