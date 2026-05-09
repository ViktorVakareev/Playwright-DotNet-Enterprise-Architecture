using Allure.Commons;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Allure.Core;
using System.Net.Http.Json;
using System.Text;

namespace WorldBank.Automation.Tests.Infrastructure;

[AllureNUnit]
public class AiTriage : PageTest
{
    [TearDown]
    public async Task TriageOnFailure()
    {
        // Only run if the test actually failed
        if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            bool isAiEnabled = Environment.GetEnvironmentVariable("AI_TRIAGE_ENABLED")?.ToLower() == "true";

            if (isAiEnabled)
            {
                var testName = TestContext.CurrentContext.Test.Name;
                var stackTrace = TestContext.CurrentContext.Result.StackTrace ?? "No stack trace available";
                var errorMessage = TestContext.CurrentContext.Result.Message ?? "No error message available";

                // 1. Call your real Llama 3 Logic
                var aiAnalysis = await GenerateLlama3Report(errorMessage, stackTrace);

                // 2. Format the entry and add it to the Global bucket
                string entry = $"### ❌ {testName}\n{aiAnalysis}\n\n---\n";
                GlobalSetup.AiReports.Add(entry);

                // 3. Keep it in Allure as an attachment for easy reading in the UI
                AllureLifecycle.Instance.AddAttachment(
                    $"AI Triage - {testName}",
                    "text/markdown",
                    Encoding.UTF8.GetBytes(aiAnalysis),
                    ".md"
                );
            }
        }
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions() ?? new BrowserNewContextOptions();

        // Set the base URL here once and for all!
        options.BaseURL = "http://sandbox.worldbank.internal:8081";

        // Remember that HTTPS bypass we talked about earlier? You can add it here too!
        options.IgnoreHTTPSErrors = true;

        return options;
    }

    private async Task<string> GenerateLlama3Report(string error, string stack)
    {
        try
        {
            string baseUrl = Environment.GetEnvironmentVariable("OLLAMA_API_URL") ?? "http://host.docker.internal:11434";
            using var client = new HttpClient();
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(60); // AI needs time to think

            var payload = new
            {
                model = "llama3:latest", // Ensure this matches exactly what 'ollama list' shows
                prompt = $"Explain why this test failed and suggest a fix:\nError: {error}\nStack: {stack}",
                stream = false
            };

            // Ensure the endpoint is exactly /api/generate
            var response = await client.PostAsJsonAsync("/api/generate", payload);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<dynamic>();
                // Ollama returns the text in a property called "response"
                return result?.response?.ToString() ?? "AI returned empty text.";
            }

            // This is where your current error is coming from
            return $"> **AI Triage Warning:** Ollama reachable but returned {response.StatusCode}. (Is llama3 pulled?)";
        }
        catch (Exception ex)
        {
            return $"> **AI Triage Error:** {ex.Message}";
        }
    }
}       