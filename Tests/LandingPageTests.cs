using Microsoft.Playwright;
using NUnit.Framework;
using WorldBank.Automation.Tests.Infrastructure;

namespace WorldBank.Automation.Tests.Tests;

[Parallelizable(ParallelScope.All)]
[TestFixture]
public class LoginScenariosTests : AiTriage
{
    [SetUp]
    public async Task NavigateToLogin()
    {
        // Dynamically navigates to QA, Sandbox, or Pre-Prod based on the Jenkins parameter
        var loginUrl = $"{TestConfig.BaseUrl}/auth/login";
        await Page.GotoAsync(loginUrl);
    }

    [Test]
    public async Task Login_ValidCredentials_ShouldRouteToDashboard()
    {
        await Page.GetByLabel("Corporate ID / Email").FillAsync("admin.user@worldbank.internal");
        await Page.GetByLabel("Password").FillAsync("SecurePassword123!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*dashboard"));
    }
}