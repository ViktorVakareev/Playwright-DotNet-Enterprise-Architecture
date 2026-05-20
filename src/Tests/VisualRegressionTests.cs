using System.Text.RegularExpressions;
using WorldBank.Automation.Tests.Infrastructure;

namespace WorldBank.Automation.Tests.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [Category("Visual")]
    public class VisualRegressionTests : AiTriage
    {
        [SetUp]
        public async Task SetupDashboardAsync()
        {
            await Page.GotoAsync($"{AppConfig.GetBaseUrl()}/dashboard.html?role=standard");

            // Wait for the app router to confirm we are in the secure zone
            await Expect(Page).ToHaveURLAsync(new Regex(".*dashboard.*"));

            // Wait for a core component to ensure JS hydration is complete
            await Expect(Page.GetByTestId("app-title")).ToBeVisibleAsync();
        }

        /* ==========================================
           DASHBOARD TESTS
           ========================================== */

        [Test]
        public async Task Dashboard_LightMode_ShouldRenderBaseline()
        {
            await Verifier.Verify(Page);
        }

        [Test]
        public async Task Dashboard_DarkMode_ShouldRenderCorrectly()
        {
            await Page.Locator("#btn-dark-mode").ClickAsync();

            // Ensure the theme change has processed via CSS
            await Expect(Page.Locator("body")).ToHaveAttributeAsync("data-theme", "dark");

            await Verifier.Verify(Page);
        }

        [Test]
        public async Task Dashboard_NotificationModal_ShouldOverlayCorrectly()
        {
            await Page.Locator("#btn-notifications").ClickAsync();

            var modal = Page.Locator("#notification-modal");
            await Expect(modal).ToBeVisibleAsync();

            // 🏆 FIXED: Clean string-based line scrubber that avoids syntax nesting issues.
            // Any line containing your dynamic text will be cleanly scrubbed out.
            var settings = new VerifySettings();
            settings.ScrubLines(line => line.Contains("notification-content"));

            await Verifier.Verify(Page, settings);
        }

        /* ==========================================
           WIRE TRANSFER TESTS
           ========================================== */

        [Test]
        public async Task WireTransfer_Step1_ShouldRenderCorrectly()
        {
            await Page.GotoAsync($"{AppConfig.GetBaseUrl()}/transfer.html");
            await Expect(Page.GetByTestId("step-1-form")).ToBeVisibleAsync();
            await Verifier.Verify(Page);
        }

        [Test]
        public async Task WireTransfer_Step2_ShouldRenderCorrectly()
        {
            await Page.GotoAsync($"{AppConfig.GetBaseUrl()}/transfer.html");

            // Fixed the method names to include the necessary Async suffixes
            await Page.GetByTestId("recipient-select").SelectOptionAsync("acme");
            await Page.GetByTestId("acc-number").FillAsync("1234567890");
            await Page.GetByTestId("btn-next-1").ClickAsync();

            await Expect(Page.GetByTestId("step-2-form")).ToBeVisibleAsync();
            await Verifier.Verify(Page);
        }
    }
}