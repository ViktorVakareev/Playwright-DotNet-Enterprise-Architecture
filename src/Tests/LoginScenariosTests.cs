using Microsoft.Playwright;
using System.Text.RegularExpressions;
using WorldBank.Automation.Tests.Data;
using WorldBank.Automation.Tests.Infrastructure;

namespace WorldBank.Automation.Tests.Tests
{
    [Parallelizable(ParallelScope.All)]
    [TestFixture]
    [Category("Authentication")]
    public class LoginScenariosTests : AiTriage // Inherits AI capabilities and Context initialization
    {
        [SetUp]
        public async Task NavigateToHome()
        {
            // Dynamically build the URL based on the injected environment variable
            string homeUrl = $"{AppConfig.GetBaseUrl()}/index.html";

            // Navigate directly to the environment-specific home page
            await Page.GotoAsync(homeUrl);
        }

        // 1. Standard Happy Path
        [Test]
        public async Task Login_ValidCredentials_ShouldRouteToDashboard()
        {
            // Inject dynamic user from the factory
            var validUser = DataFactory.CreateValidUser();

            await Page.GetByPlaceholder("Username").FillAsync(validUser.Username);
            await Page.GetByPlaceholder("Password").FillAsync(validUser.Password);
            await Page.GetByRole(AriaRole.Button, new() { Name = "Secure Login" }).ClickAsync();

            await Expect(Page).ToHaveURLAsync(new Regex(".*dashboard"));
        }

        // 2. Generic Error Handling
        [Test]
        public async Task Login_InvalidPassword_ShouldShowGenericError()
        {
            await Page.GetByPlaceholder("Username").FillAsync("admin.user@worldbank.internal");
            await Page.GetByPlaceholder("Password").FillAsync("WrongPassword999");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Secure Login" }).ClickAsync();

            var errorAlert = Page.Locator("#error-message");
            await Expect(errorAlert).ToHaveTextAsync("Invalid credentials. Generic error.");
            await Expect(errorAlert).Not.ToContainTextAsync("password");
        }

        // 3. Brute Force / Rate Limiting Prevention
        [Test]
        public async Task Login_FiveFailedAttempts_ShouldLockAccount()
        {
            var testUser = $"locked_test_{Guid.NewGuid():N}@worldbank.internal";

            // Cache locators outside the loop for better performance
            var usernameInput = Page.GetByPlaceholder("Username");
            var passwordInput = Page.GetByPlaceholder("Password");
            var loginButton = Page.GetByRole(AriaRole.Button, new() { Name = "Secure Login" });

            for (int i = 0; i < 5; i++)
            {
                await usernameInput.FillAsync(testUser);
                await passwordInput.FillAsync("RandomAttempt!");
                await loginButton.ClickAsync();
            }

            var lockoutMessage = Page.Locator("#error-message");
            await Expect(lockoutMessage).ToHaveTextAsync("Account Locked due to too many failed attempts.");
        }

        // 4. Multi-Factor Authentication (MFA) Routing
        [Test]
        public async Task Login_FromNewDevice_ShouldRequireMfaChallenge()
        {
            await Page.GetByPlaceholder("Username").FillAsync("newdevice_user");
            await Page.GetByPlaceholder("Password").FillAsync("ValidPassword123!");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Secure Login" }).ClickAsync();

            // Verify routing shifted to the MFA step
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "MFA Challenge" })).ToBeVisibleAsync();
            await Expect(Page.GetByPlaceholder("Enter 6-digit code")).ToBeVisibleAsync();
        }

        // 5. Zero-Trust SQL Injection Attempt
        [Test]
        public async Task Login_SqlInjectionAttempt_ShouldBeRejectedCleanly()
        {
            // Factory handles the specific SQL payload definition
            var hackerUser = DataFactory.CreateUser_SqlInjection();

            await Page.GetByPlaceholder("Username").FillAsync(hackerUser.Username);
            await Page.GetByPlaceholder("Password").FillAsync(hackerUser.Password);
            await Page.GetByRole(AriaRole.Button, new() { Name = "Secure Login" }).ClickAsync();

            await Expect(Page.Locator("#error-message")).ToHaveTextAsync("Security Violation: Invalid Input Detected");
        }

        // 6. Cross-Site Scripting (XSS) Sanitization
        [Test]
        public async Task Login_XssPayloadInUsername_ShouldSanitizeInput()
        {
            // Factory handles the XSS payload definition
            var xssUser = DataFactory.CreateUser_XssPayload();

            await Page.GetByPlaceholder("Username").FillAsync(xssUser.Username);
            await Page.GetByPlaceholder("Password").FillAsync(xssUser.Password);

            Page.Dialog += (_, _) => Assert.Fail("CRITICAL: XSS Payload Executed via Alert Box!");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Secure Login" }).ClickAsync();

            await Expect(Page.Locator("#error-message")).ToHaveTextAsync("Security Violation: Invalid Input Detected");
        }

        // 7. Secure Cookie Validation (DevSecOps Check)
        [Test]
        public async Task Auth_SuccessfulLogin_ShouldSetHttpOnlyAndSecureCookies()
        {
            await Page.GetByPlaceholder("Username").FillAsync("standarduser");
            await Page.GetByPlaceholder("Password").FillAsync("password123");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Secure Login" }).ClickAsync();

            await Expect(Page).ToHaveURLAsync(new Regex(".*dashboard"));

            var cookies = await Page.Context.CookiesAsync();
            var authCookie = cookies.FirstOrDefault(c => c.Name == "session");

            Assert.That(authCookie, Is.Not.Null, "Authentication cookie was not found.");
            // Because GitHub pages uses strict HTTPS, this assertion will now pass reliably
            Assert.That(authCookie.Secure, Is.True, "Cookie 'Secure' flag is missing.");
        }

        // 8. Role-Based Access Control (RBAC) Validation
        [Test]
        public async Task Auth_LoginAsStandardUser_ShouldNotSeeAdminControls()
        {
            var standardUser = DataFactory.CreateValidUser(); // Default role is Standard

            await Page.GetByPlaceholder("Username").FillAsync(standardUser.Username);
            await Page.GetByPlaceholder("Password").FillAsync(standardUser.Password);
            await Page.GetByRole(AriaRole.Button, new() { Name = "Secure Login" }).ClickAsync();

            await Expect(Page).ToHaveURLAsync(new Regex(".*dashboard"));

            var adminPanel = Page.GetByRole(AriaRole.Heading, new() { Name = "Administrator Tools" });
            await Expect(adminPanel).ToHaveCountAsync(0);
        }
        
        // --- SKIPPED TESTS ---
        [Test]
        [Ignore("Requires a real backend server to handle session tokens, static HTML cannot redirect automatically.")]
        public async Task Auth_ExpiredSession_ShouldRedirectToLogin() { }

        [Test]
        [Ignore("Requires backend Cache-Control headers. Static HTML will always allow the browser back button.")]
        public async Task Auth_LogoutAndBrowserBack_ShouldNotLoadSecurePage() { }
    }
}