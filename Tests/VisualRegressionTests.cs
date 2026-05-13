using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using VerifyNUnit;
using VerifyTests;
using System.Runtime.CompilerServices;

namespace WorldBank.Automation.Tests
{
    // A ModuleInitializer automatically sets up the Verify engine 
    // when the NUnit assembly loads, before any tests run.
    public static class VerifySetup
    {
        [ModuleInitializer]
        public static void Init() => VerifyPlaywright.Initialize();
    }

    [TestFixture]
    [Parallelizable(ParallelScope.Self)]
    public class VisualRegressionTests : PageTest
    {
        private readonly string _baseUrl = "http://localhost:8081";

        /* ==========================================
           DASHBOARD TESTS (THEMES & Z-INDEX)
           ========================================== */

        [Test]
        public async Task Dashboard_LightMode_ShouldRenderBaseline()
        {
            await Page.GotoAsync($"{_baseUrl}/dashboard.html");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verifier captures BOTH the Screenshot and the HTML DOM State!
            await Verifier.Verify(Page);
        }

        [Test]
        public async Task Dashboard_DarkMode_ShouldRenderCorrectly()
        {
            await Page.GotoAsync($"{_baseUrl}/dashboard.html");
            await Page.ClickAsync("#btn-dark-mode");

            await Verifier.Verify(Page);
        }

        [Test]
        public async Task Dashboard_NotificationModal_ShouldOverlayCorrectly()
        {
            await Page.GotoAsync($"{_baseUrl}/dashboard.html");
            await Page.ClickAsync("#btn-notifications");
            await Expect(Page.Locator("#notification-modal")).ToBeVisibleAsync();

            await Verifier.Verify(Page);
        }

        /* ==========================================
           WIRE TRANSFER TESTS (STEPPER STATE)
           ========================================== */

        [Test]
        public async Task WireTransfer_Step1_ShouldRenderCorrectly()
        {
            await Page.GotoAsync($"{_baseUrl}/transfer.html");

            await Verifier.Verify(Page);
        }

        [Test]
        public async Task WireTransfer_Step2_ShouldRenderCorrectly()
        {
            await Page.GotoAsync($"{_baseUrl}/transfer.html");
            await Page.ClickAsync("text='Next Step'");

            await Verifier.Verify(Page);
        }
    }
}