using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WorldBank.Automation.Tests.Infrastructure;

namespace WorldBank.Automation.Tests // Adjust if your namespace includes .Infrastructure
{
    [SetUpFixture]
    public class GlobalSetup
    {
        // =====================================================================
        // AI TRIAGE STATE & QUEUE
        // =====================================================================
        public static readonly ConcurrentBag<string> AiReports = new();
        public static readonly SemaphoreSlim AiQueue = new SemaphoreSlim(1, 1);
        private static string ReportPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "AiTriage_Summary.md");

        // =====================================================================
        // PLAYWRIGHT AUTHENTICATION STATE
        // =====================================================================
        // Defines a single source of truth for the auth file location
        // Dynamically locks the file to the exact directory where NUnit is executing the DLL
        public static readonly string AuthStatePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "auth.json");

        [OneTimeSetUp]
        public async Task GlobalSetupMethod()
        {
            // 1. Clean up previous AI report
            if (File.Exists(ReportPath)) File.Delete(ReportPath);

            // 2. Perform Global Authentication (One-Time Login)
            TestContext.Progress.WriteLine("[Setup] Provisioning Global Authentication State...");

            var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            // Navigate to login and authenticate
            await page.GotoAsync($"{AppConfig.GetBaseUrl()}/login.html");
            await page.GetByPlaceholder("Username").FillAsync("standarduser");
            await page.GetByPlaceholder("Password").FillAsync("password123");
            await page.GetByRole(AriaRole.Button, new() { Name = "Secure Login" }).ClickAsync();

            // Wait for the JS routing to push to the dashboard, confirming auth succeeded
            await page.WaitForURLAsync("**/dashboard*");

            // Dump the session cookies and localStorage to the JSON file
            await context.StorageStateAsync(new() { Path = AuthStatePath });

            // Gracefully close the global setup browser to free up memory before tests start
            await browser.CloseAsync();
            playwright.Dispose();

            TestContext.Progress.WriteLine("[Setup] Global Authentication State successfully saved.");
        }

        [OneTimeTearDown]
        public void GlobalTeardownMethod()
        {
            // 1. Write the aggregated AI Report
            if (!AiReports.IsEmpty)
            {
                string header = "# 🤖 Llama 3 Aggregate Failure Analysis\n\n";
                File.WriteAllText(ReportPath, header + string.Join("\n", AiReports));
                TestContext.Progress.WriteLine($"[AI] Master report generated: {ReportPath}");
            }

            // 2. Properly dispose of the concurrency queue
            AiQueue?.Dispose();

            // 3. Clean up the storage state artifact so it doesn't leak into the next pipeline run
            if (File.Exists(AuthStatePath)) File.Delete(AuthStatePath);
        }
    }
}