using Allure.Commons;
using Microsoft.Playwright.NUnit;
using NUnit.Allure.Core;
using NUnit.Framework;
using System.Text;
using System.Net.Http.Json;

namespace WorldBank.Automation.Tests.Infrastructure;

[AllureNUnit]
public class AiTriage : PageTest
{
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

                // Wait in line before talking to the AI
                var aiAnalysis = await ProcessAiRequestWithQueue(errorMessage, stackTrace);

                string entry = $"### ❌ {testName}\n{aiAnalysis}\n\n---\n";
                GlobalSetup.AiReports.Add(entry);

                AllureLifecycle.Instance.AddAttachment($"AI Analysis - {testName}", "text/markdown", Encoding.UTF8.GetBytes(aiAnalysis), ".md");
            }
        }
    }

    private async Task<string> ProcessAiRequestWithQueue(string error, string stack)
    {
        // 1. Point to the Global Traffic Light
        await GlobalSetup.AiQueue.WaitAsync();

        try
        {
            return await GenerateLlama3Report(error, stack);
        }
        finally
        {
            // 2. Release the Global Traffic Light
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

            // 4. INCREASE TIMEOUT: Give the AI up to 3 minutes just in case it is a "cold start"
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