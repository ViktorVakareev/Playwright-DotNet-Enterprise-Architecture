using System.Runtime.CompilerServices;
using Microsoft.Playwright;
using WorldBank.Automation.Tests.Infrastructure;

namespace WorldBank.Automation.Tests.Tests
{
    // A ModuleInitializer automatically sets up the Verify engine 
    // when the NUnit assembly loads, before any tests run.
    public static class VerifySetup
    {
        [ModuleInitializer]
        public static void Init() => VerifyPlaywright.Initialize();
    }

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [Category("Visual")]
    public class VisualRegressionTests : AiTriage // Inherits AI capabilities and Context initialization
    {
        /* ==========================================
           DASHBOARD TESTS (THEMES & Z-INDEX)
           ========================================== */

        [Test]
        public async Task Dashboard_LightMode_ShouldRenderBaseline()
        {
            // Dynamically construct the URL
            string url = $"{AppConfig.GetBaseUrl()}/dashboard.html?role=standard";
            await Page.GotoAsync(url);

            // Wait for a core element to render to guarantee the DOM is painted before snapping
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "World Bank Secure Dashboard" })).ToBeVisibleAsync();

            // Verifier captures BOTH the Screenshot and the HTML DOM State
            await Verifier.Verify(Page);
        }

        [Test]
        public async Task Dashboard_DarkMode_ShouldRenderCorrectly()
        {
            string url = $"{AppConfig.GetBaseUrl()}/dashboard.html?role=standard";
            await Page.GotoAsync(url);

            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "World Bank Secure Dashboard" })).ToBeVisibleAsync();

            await Page.Locator("#btn-dark-mode").ClickAsync();

            await Verifier.Verify(Page);
        }

        [Test]
        public async Task Dashboard_NotificationModal_ShouldOverlayCorrectly()
        {
            string url = $"{AppConfig.GetBaseUrl()}/dashboard.html?role=standard";
            await Page.GotoAsync(url);

            await Page.Locator("#btn-notifications").ClickAsync();

            var modal = Page.Locator("#notification-modal");
            await Expect(modal).ToBeVisibleAsync();

            // Verifying the whole page ensures we check the background mask/z-index
            await Verifier.Verify(Page);
        }

        /* ==========================================
           WIRE TRANSFER TESTS (STEPPER STATE)
           ========================================== */

        [Test]
        public async Task WireTransfer_Step1_ShouldRenderCorrectly()
        {
            // Note: Update this to the exact mock URL if transfer is handled inside the dashboard
            string url = $"{AppConfig.GetBaseUrl()}/transfer.html";
            await Page.GotoAsync(url);

            // Ensure the form is painted
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Initiate Wire Transfer" })).ToBeVisibleAsync();

            await Verifier.Verify(Page);
        }

        [Test]
        public async Task WireTransfer_Step2_ShouldRenderCorrectly()
        {
            string url = $"{AppConfig.GetBaseUrl()}/transfer.html";
            await Page.GotoAsync(url);

            // Swap fragile text selector for a robust ARIA role locator
            await Page.GetByRole(AriaRole.Button, new() { Name = "Next Step" }).ClickAsync();

            // Optional: If there is a specific UI transition, wait for the Step 2 header/indicator
            // await Expect(Page.Locator(".step-2-active")).ToBeVisibleAsync();

            await Verifier.Verify(Page);
        }
    }
}