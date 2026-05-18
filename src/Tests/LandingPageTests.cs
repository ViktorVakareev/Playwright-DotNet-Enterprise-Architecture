using System.Text.RegularExpressions;
using Microsoft.Playwright;
using WorldBank.Automation.Tests.Infrastructure;

namespace WorldBank.Automation.Tests.Tests
{
    [Parallelizable(ParallelScope.All)]
    [TestFixture]
    [Category("Smoke")]
    public class LandingPageTests : AiTriage // Inherits AI capabilities and Context initialization
    {
        [SetUp]
        public async Task NavigateToHome()
        {
            // Dynamically build the URL based on the injected environment variable
            string homeUrl = $"{AppConfig.GetBaseUrl()}/index.html";

            // Navigate directly to the environment-specific home page
            await AuthenticateAndNavigateAsync(homeUrl);
        }

        // 1. Updated Original Test (Happy Path Data Search)
        [Test]
        public async Task Search_WithSyntheticUser_ShouldLoadResults()
        {
            // Act
            await Page.GetByPlaceholder("Search employees/users...").FillAsync("Synthetic User John");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();

            // Assert: Auto-waiting for the specific DOM element created by our JavaScript
            var resultsPanel = Page.Locator(".result-item");
            await Expect(resultsPanel).ToHaveTextAsync("Synthetic User Profile Loaded");
            await Expect(resultsPanel).ToBeVisibleAsync();
        }

        // 2. Negative Path Search
        [Test]
        public async Task Search_WithUnregisteredUser_ShouldShowNoResults()
        {
            await Page.GetByPlaceholder("Search employees/users...").FillAsync("Unknown Random Person");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();

            await Expect(Page.Locator("#search-results")).ToHaveTextAsync("No results found.");
        }

        // 3. Routing & Navigation Verification
        [Test]
        public async Task Navigation_LoginLink_ShouldRouteToAuthGateway()
        {
            await Page.GetByRole(AriaRole.Link, new() { Name = "Go to Secure Login" }).ClickAsync();

            // Ensure the URL changed appropriately
            await Expect(Page).ToHaveURLAsync(new Regex(".*login.html"));
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Authentication Gateway" })).ToBeVisibleAsync();
        }

        // 4. Core Layout & Smoke Test
        [Test]
        public async Task PageLoad_VerifyCoreElements_ShouldBeVisible()
        {
            await Expect(Page).ToHaveTitleAsync("World Bank Sandbox - Home");
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "World Bank Internal Portal" })).ToBeVisibleAsync();
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Directory Search" })).ToBeVisibleAsync();
        }

        // 5. State Mutation (Ensures DOM replaces, not appends, results)
        [Test]
        public async Task Search_ConsecutiveSearches_ShouldUpdateResultsDiv()
        {
            // First Search
            await Page.GetByPlaceholder("Search employees/users...").FillAsync("Synthetic");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();
            await Expect(Page.Locator(".result-item")).ToHaveTextAsync("Synthetic User Profile Loaded");

            // Second Search
            await Page.GetByPlaceholder("Search employees/users...").FillAsync("Missing");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();

            // Assert the previous result was cleared and replaced
            await Expect(Page.Locator("#search-results")).ToHaveTextAsync("No results found.");
            await Expect(Page.Locator(".result-item")).ToHaveCountAsync(0);
        }

        // 6. Edge Case: Empty Submission
        [Test]
        public async Task Search_EmptyQuery_ShouldShowNoResults()
        {
            // Click search without typing anything
            await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();

            await Expect(Page.Locator("#search-results")).ToHaveTextAsync("No results found.");
        }

        // 7. Security: Cross-Site Scripting (XSS) in Search
        [Test]
        public async Task Security_SearchInput_ShouldNotExecuteScripts()
        {
            // Listen for any unexpected JavaScript alerts popping up
            Page.Dialog += (_, _) => Assert.Fail("CRITICAL: XSS Payload Executed via Alert Box!");

            // Inject malicious payload
            await Page.GetByPlaceholder("Search employees/users...").FillAsync("<script>alert('Hacked')</script>");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();

            // Ensure the app handled it safely without executing the script
            await Expect(Page.Locator("#search-results")).ToBeVisibleAsync();
        }

        // 8. UX Validation
        [Test]
        public async Task UI_SearchInput_ShouldHaveCorrectPlaceholder()
        {
            // Validating attributes directly helps ensure front-end devs don't break UX
            var searchInput = Page.Locator("#search-input");
            await Expect(searchInput).ToHaveAttributeAsync("placeholder", "Search employees/users...");
        }

        // 9. Accessibility (A11y) & Keyboard Navigation
        [Test]
        public async Task Accessibility_LoginLink_ShouldBeKeyboardFocusable()
        {
            var loginLink = Page.GetByRole(AriaRole.Link, new() { Name = "Go to Secure Login" });

            // Programmatically focus it to ensure it supports keyboard navigation
            await loginLink.FocusAsync();

            // Assert the browser registers it as the active element
            await Expect(loginLink).ToBeFocusedAsync();
        }

        // 10. Performance / Stability: Catch Silent Console Errors
        [Test]
        public async Task Performance_Page_ShouldNotHaveConsoleErrors()
        {
            var consoleErrors = new List<string>();

            // Attach an event listener to the browser console
            Page.Console += (_, msg) =>
            {
                if (msg.Type == "error")
                {
                    consoleErrors.Add(msg.Text);
                }
            };

            // Reload to capture any initial load errors
            await Page.ReloadAsync();

            // Force Playwright to wait for all network traffic to finish
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert no console errors were captured
            Assert.That(consoleErrors, Is.Empty, $"Found console errors: {string.Join(", ", consoleErrors)}");
        }
    }
}