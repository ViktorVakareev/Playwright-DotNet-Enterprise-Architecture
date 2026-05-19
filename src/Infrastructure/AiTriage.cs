using Allure.Commons;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Allure.Core;
using System.IO;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WorldBank.Automation.Tests.Data;

namespace WorldBank.Automation.Tests.Infrastructure;

[AllureNUnit]
public class AiTriage : PageTest
{
    // 1. Map Jenkins variables to Native Playwright variables BEFORE tests start
    public AiTriage()
    {
        var browserEnv = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSER")?.ToLower();

        if (!string.IsNullOrEmpty(browserEnv))
        {
            // Set native Playwright Browser (chromium, firefox, or webkit)
            if (browserEnv.Contains("firefox"))
                Environment.SetEnvironmentVariable("BROWSER", "firefox");
            else if (browserEnv.Contains("webkit"))
                Environment.SetEnvironmentVariable("BROWSER", "webkit");
            else
                Environment.SetEnvironmentVariable("BROWSER", "chromium");

            // Set native Playwright Headless mode
            bool isHeadless = browserEnv.Contains("headless");
            Environment.SetEnvironmentVariable("HEADLESS", isHeadless ? "true" : "false");

            // Set native Playwright Channel (forces standard Google Chrome instead of Chromium)
            if (browserEnv.StartsWith("chrome"))
            {
                Environment.SetEnvironmentVariable("BROWSER_CHANNEL", "chrome");
            }
            else
            {
                Environment.SetEnvironmentVariable("BROWSER_CHANNEL", null);
            }
        }
    }

    // 2. Set your Mock App BaseURL (This IS a valid Playwright override)
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions() ?? new BrowserNewContextOptions();
        options.BaseURL = "http://sandbox.worldbank.internal:8081";
        options.IgnoreHTTPSErrors = true;

        return options;
    }
    protected async Task AuthenticateAndNavigateAsync(string targetSecureUrl)
    {
        var sessionUser = DataFactory.CreateValidUser();

        // ⚠️ CRITICAL: This must be native Page.GotoAsync
        await Page.GotoAsync($"{AppConfig.GetBaseUrl()}/login.html");

        await Page.GetByPlaceholder("Username").FillAsync(sessionUser.Username);
        await Page.GetByPlaceholder("Password").FillAsync(sessionUser.Password);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Secure Login" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(".*dashboard.*"));

        if (!Page.Url.Contains(targetSecureUrl))
        {
            // ⚠️ CRITICAL: This must be native Page.GotoAsync
            await Page.GotoAsync(targetSecureUrl);
        }
    }

    [TearDown]
    public async Task TriageOnFailure()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            bool isAiEnabled = Environment.GetEnvironmentVariable("AI_TRIAGE_ENABLED")?.ToLower() == "true";

            if (isAiEnabled)
            {
                var testName = TestContext.CurrentContext.Test.Name;
                var stackTrace = TestContext.CurrentContext.Result.StackTrace ?? "No stack trace available";
                var errorMessage = TestContext.CurrentContext.Result.Message ?? "No error message available";

                var aiAnalysis = await ProcessAiRequestWithQueue(errorMessage, stackTrace);

                // 1. Create the entry
                string entry = $"### ❌ {testName}\n\n**Analysis:**\n{aiAnalysis}\n\n**Error:** `{errorMessage}`\n\n---\n";

                // 2. Add to global list (for Allure summary)
                GlobalSetup.AiReports.Add(entry);

                var workspacePath = Environment.GetEnvironmentVariable("WORKSPACE") ?? ".";
                var reportPath = Path.Combine(workspacePath, "AiTriage_Summary.md");
                await File.AppendAllTextAsync(reportPath, entry);

                try
                {
                    AllureLifecycle.Instance.AddAttachment($"AI Analysis - {testName}", "text/markdown", Encoding.UTF8.GetBytes(aiAnalysis), ".md");
                }
                catch (ArgumentNullException)
                {
                    TestContext.Progress.WriteLine($"[WARNING] Allure lost context for {testName}.");
                }
            }
        }
    }

    private async Task<string> ProcessAiRequestWithQueue(string error, string stack)
    {
        // Point to the Global Traffic Light
        await GlobalSetup.AiQueue.WaitAsync();

        try
        {
            return await GenerateLlama3Report(error, stack);
        }
        finally
        {
            // Release the Global Traffic Light
            GlobalSetup.AiQueue.Release();
        }
    }

    private async Task<string> GenerateLlama3Report(string error, string stack)
    {
        try
        {
            string baseUrl = Environment.GetEnvironmentVariable("OLLAMA_API_URL") ?? "http://host.docker.internal:11434";
            using var client = new HttpClient();
            client.BaseAddress = new Uri(baseUrl);

            // INCREASE TIMEOUT: Give the AI up to 3 minutes just in case it is a "cold start"
            client.Timeout = TimeSpan.FromMinutes(3);

            var payload = new
            {
                model = "llama3:latest",
                prompt = $"Explain why this test failed and suggest a fix:\nError: {error}\nStack: {stack}",
                stream = false
            };

            var response = await client.PostAsJsonAsync("/api/generate", payload);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<dynamic>();
                return result?.response?.ToString() ?? "AI returned empty text.";
            }

            return $"> **AI Triage Warning:** Ollama reachable but returned {response.StatusCode}.";
        }
        catch (Exception ex)
        {
            return $"> **AI Triage Error:** {ex.Message}";
        }
    }
}