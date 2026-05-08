using Microsoft.Playwright;
using NUnit.Framework;
using WorldBank.Automation.Tests.Infrastructure;
using WorldBank.Automation.Tests.Data;

namespace WorldBank.Automation.Tests.Tests;

[Parallelizable(ParallelScope.All)]
[TestFixture]
public class LoginScenariosTests : AiTriage
{
    private const string LoginUrl = "https://sandbox.worldbank.internal/auth/login";

    [SetUp]
    public async Task NavigateToLogin()
    {
        await Page.GotoAsync(LoginUrl);
    }

    // 1. Standard Happy Path
    [Test]
    public async Task Login_ValidCredentials_ShouldRouteToDashboard()
    {
        await Page.GetByLabel("Corporate ID / Email").FillAsync("admin.user@worldbank.internal");
        await Page.GetByLabel("Password").FillAsync("SecurePassword123!"); // In reality, pulled from UserSecrets
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*dashboard"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Welcome" })).ToBeVisibleAsync();
    }

    // 2. Generic Error Handling (Security Standard: Never reveal exactly which credential was wrong)
    [Test]
    public async Task Login_InvalidPassword_ShouldShowGenericError()
    {
        await Page.GetByLabel("Corporate ID / Email").FillAsync("admin.user@worldbank.internal");
        await Page.GetByLabel("Password").FillAsync("WrongPassword999");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();

        var errorAlert = Page.GetByRole(AriaRole.Alert);
        await Expect(errorAlert).ToHaveTextAsync("Invalid credentials provided.");
        await Expect(errorAlert).Not.ToContainTextAsync("password"); // Prove no system internals are leaked
    }

    // 3. Brute Force / Rate Limiting Prevention
    [Test]
    public async Task Login_FiveFailedAttempts_ShouldLockAccount()
    {
        var testUser = $"locked_test_{Guid.NewGuid():N}@worldbank.internal";

        // Simulate a rapid brute-force attack
        for (int i = 0; i < 5; i++)
        {
            await Page.GetByLabel("Corporate ID / Email").FillAsync(testUser);
            await Page.GetByLabel("Password").FillAsync("RandomAttempt!");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();
        }

        var lockoutMessage = Page.GetByRole(AriaRole.Alert);
        await Expect(lockoutMessage).ToHaveTextAsync("Account locked due to excessive failed attempts. Contact IT Support.");
    }

    // 4. Multi-Factor Authentication (MFA) Routing
    [Test]
    public async Task Login_FromNewDevice_ShouldRequireMfaChallenge()
    {
        // Intercept API to simulate a "New Device" flag from the backend
        await Page.RouteAsync("**/api/auth/verify", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                Body = "{\"requiresMfa\": true, \"mfaType\": \"Authenticator\"}"
            });
        });

        await Page.GetByLabel("Corporate ID / Email").FillAsync("teller@worldbank.internal");
        await Page.GetByLabel("Password").FillAsync("ValidPassword123!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();

        // Verify routing shifted to the MFA step, not the dashboard
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Two-Factor Authentication" })).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("6-Digit Authenticator Code")).ToBeVisibleAsync();
    }

    // 5. Zero-Trust SQL Injection Attempt
    [Test]
    public async Task Login_SqlInjectionAttempt_ShouldBeRejectedCleanly()
    {
        await Page.GetByLabel("Corporate ID / Email").FillAsync("admin@worldbank.internal' OR '1'='1");
        await Page.GetByLabel("Password").FillAsync("DoesnMatter");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();

        // Ensure the app doesn't crash with a YSOD (Yellow Screen of Death) or expose DB syntax
        await Expect(Page.GetByRole(AriaRole.Alert)).ToHaveTextAsync("Invalid credentials provided.");
        var pageText = await Page.TextContentAsync("body");
        Assert.That(pageText, Does.Not.Contain("SQL syntax"), "System leaked DB context!");
    }

    // 6. Cross-Site Scripting (XSS) Sanitization
    [Test]
    public async Task Login_XssPayloadInUsername_ShouldSanitizeInput()
    {
        var xssPayload = "<script>alert('Hacked')</script>admin@worldbank.internal";
        await Page.GetByLabel("Corporate ID / Email").FillAsync(xssPayload);
        await Page.GetByLabel("Password").FillAsync("ValidPassword123!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();

        // If the payload executed, a dialog would appear. We assert no dialog appears.
        Page.Dialog += (_, _) => Assert.Fail("XSS Payload Executed via Alert Box!");

        await Expect(Page.GetByRole(AriaRole.Alert)).ToHaveTextAsync("Invalid credentials provided.");
    }

    // 7. Secure Cookie Validation (DevSecOps Check)
    [Test]
    public async Task Auth_SuccessfulLogin_ShouldSetHttpOnlyAndSecureCookies()
    {
        await Page.GetByLabel("Corporate ID / Email").FillAsync("admin.user@worldbank.internal");
        await Page.GetByLabel("Password").FillAsync("SecurePassword123!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*dashboard"));

        var cookies = await Page.Context.CookiesAsync();
        var authCookie = cookies.FirstOrDefault(c => c.Name == "WB_Auth_Session");

        Assert.That(authCookie, Is.Not.Null, "Authentication cookie was not found.");
        Assert.That(authCookie.HttpOnly, Is.True, "Cookie is vulnerable to XSS (Not HttpOnly).");
        Assert.That(authCookie.Secure, Is.True, "Cookie is transmitting over plain HTTP (Not Secure).");
    }

    // 8. Session Expiration (JWT Handling)
    [Test]
    public async Task Auth_ExpiredSession_ShouldRedirectToLogin()
    {
        // 1. Login normally
        await Page.GetByLabel("Corporate ID / Email").FillAsync("admin.user@worldbank.internal");
        await Page.GetByLabel("Password").FillAsync("SecurePassword123!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*dashboard"));

        // 2. Clear local storage/cookies to simulate session timeout
        await Page.EvaluateAsync("window.localStorage.clear();");
        await Page.Context.ClearCookiesAsync();

        // 3. Attempt to navigate to a secure route
        await Page.GotoAsync("https://sandbox.worldbank.internal/transfers");

        // 4. Assert forced redirection
        await Expect(Page).ToHaveURLAsync(LoginUrl);
        await Expect(Page.GetByText("Your session has expired.")).ToBeVisibleAsync();
    }

    // 9. Role-Based Access Control (RBAC) Validation
    [Test]
    public async Task Auth_LoginAsStandardUser_ShouldNotSeeAdminControls()
    {
        await Page.GetByLabel("Corporate ID / Email").FillAsync("standard.teller@worldbank.internal");
        await Page.GetByLabel("Password").FillAsync("SecurePassword123!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Welcome" })).ToBeVisibleAsync();

        // Ensure the Admin portal is strictly hidden from the DOM
        var adminPanelLink = Page.GetByRole(AriaRole.Link, new() { Name = "System Administration" });
        await Expect(adminPanelLink).ToHaveCountAsync(0);
    }

    // 10. Cache Deception / Back-Button Attack Prevention
    [Test]
    public async Task Auth_LogoutAndBrowserBack_ShouldNotLoadSecurePage()
    {
        // Login
        await Page.GetByLabel("Corporate ID / Email").FillAsync("admin.user@worldbank.internal");
        await Page.GetByLabel("Password").FillAsync("SecurePassword123!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Welcome" })).ToBeVisibleAsync();

        // Logout
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log Out" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(LoginUrl);

        // Simulate user clicking the browser's "Back" button
        await Page.GoBackAsync();

        // Assert the page issues a hard reload and forces them back to login, ignoring cached HTML
        await Expect(Page).ToHaveURLAsync(LoginUrl);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Welcome" })).Not.ToBeVisibleAsync();
    }
}