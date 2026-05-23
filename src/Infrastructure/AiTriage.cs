using Allure.Commons;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Allure.Core;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
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

    public BrowserTypeLaunchOptions LaunchOptions()
    {
        var options = new BrowserTypeLaunchOptions();

        // 🛡️ ENTERPRISE FIX: Prevent Docker container deadlocks
        options.Args = new[]
        {
        "--disable-dev-shm-usage", // Forces Chromium to use /tmp instead of 64MB /dev/shm
        "--disable-gpu",           // Redundant in headless, but ensures no hardware acceleration hangs
        "--no-sandbox"             // Required for running Chromium inside standard Jenkins Docker containers
    };

        return options;
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions() ?? new BrowserNewContextOptions();

        // 🛡️ ISOLATION: Inject a unique GUID or NUnit Worker ID into the path.
        // This guarantees parallel browser threads will NEVER lock each other's video files.
        var threadId = TestContext.CurrentContext.WorkerId ?? Guid.NewGuid().ToString();
        var videoDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "playwright-videos", threadId);

        options.RecordVideoDir = videoDir;
        options.RecordVideoSize = new RecordVideoSize { Width = 1920, Height = 1080 };

        return options;
    }

    protected async Task AuthenticateAndNavigateAsync(string targetRelativeUrl)
    {
        var sessionUser = DataFactory.CreateValidUser();

        await Page.GotoAsync("login.html");

        
        await Page.GetByPlaceholder("Username").FillAsync(sessionUser.Username);
        await Page.GetByPlaceholder("Password").FillAsync(sessionUser.Password);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Secure Login" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(".*dashboard.*"));

        if (!Page.Url.Contains(targetRelativeUrl))
        {
            // 🚀 ENTERPRISE FIX 3: Clean relative navigation
            await Page.GotoAsync(targetRelativeUrl);
        }
    }

    [TearDown]
    public async Task ExecuteEnterpriseTeardownAsync()
    {
        var testResult = TestContext.CurrentContext.Result.Outcome.Status;
        bool isFailed = testResult == NUnit.Framework.Interfaces.TestStatus.Failed;
        var testName = TestContext.CurrentContext.Test.Name;

        /* ==========================================
           PHASE 1: VISUAL ARTIFACTS & I/O CLEANUP
           Must execute first to guarantee evidence isn't lost 
           if external AI network calls timeout.
           ========================================== */

        if (isFailed)
        {
            // Capture Full Page Screenshot safely
            var screenshotFileName = $"{TestContext.CurrentContext.Test.MethodName}_{Guid.NewGuid():N}.png";
            var screenshotPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, screenshotFileName);

            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = screenshotPath,
                FullPage = true
            });

            TestContext.AddTestAttachment(screenshotPath, "📸 UI State on Failure");
        }

        // 🛡️ CRITICAL: Close the Context to force Playwright to flush the .webm video file
        await Context.CloseAsync();

        // I/O Resilient Video Processing
        if (Page.Video != null)
        {
            if (isFailed)
            {
                var videoPath = await Page.Video.PathAsync();
                TestContext.AddTestAttachment(videoPath, "🎥 Execution Recording");
            }
            else
            {
                try
                {
                    // Immediate deterministic cleanup for passing tests
                    await Page.Video.DeleteAsync();
                }
                catch (IOException ex)
                {
                    TestContext.Progress.WriteLine($"[WARNING] Could not immediately delete video artifact for passing test. Handled by Jenkins lifecycle. Exception: {ex.Message}");
                }
            }
        }

        /* ==========================================
           PHASE 2: AI FAILURE TRIAGE
           Executes only after browser processes are safely terminated.
           ========================================== */

        if (isFailed)
        {
            bool isAiEnabled = Environment.GetEnvironmentVariable("AI_TRIAGE_ENABLED")?.ToLower() == "true";

            if (isAiEnabled)
            {
                var stackTrace = TestContext.CurrentContext.Result.StackTrace ?? "No stack trace available";
                var errorMessage = TestContext.CurrentContext.Result.Message ?? "No error message available";

                // Execute local LLM triage
                var aiAnalysis = await ProcessAiRequestWithQueue(errorMessage, stackTrace);

                // 1. Build the Markdown Entry
                string entry = $"### ❌ {testName}\n\n**Analysis:**\n{aiAnalysis}\n\n**Error:** `{errorMessage}`\n\n---\n";

                // 2. Append to Global List (For aggregated pipeline summary)
                GlobalSetup.AiReports.Add(entry);

                // 3. Write to physical workspace file for Jenkins artifact archiving
                var workspacePath = Environment.GetEnvironmentVariable("WORKSPACE") ?? ".";
                var reportPath = Path.Combine(workspacePath, "AiTriage_Summary.md");
                await File.AppendAllTextAsync(reportPath, entry);

                // 4. Inject directly into the Allure HTML Report Context
                try
                {
                    // Using AllureLifecycle directly here is brilliant because it allows us 
                    // to inject native Markdown rendering without saving individual physical .md files
                    AllureLifecycle.Instance.AddAttachment(
                        $"🤖 AI Analysis - {testName}",
                        "text/markdown",
                        Encoding.UTF8.GetBytes(aiAnalysis),
                        ".md"
                    );
                }
                catch (ArgumentNullException)
                {
                    TestContext.Progress.WriteLine($"[WARNING] Allure lost context for {testName}. Cannot attach AI logic.");
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