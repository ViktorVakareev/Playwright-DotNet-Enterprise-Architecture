using Allure.Commons;
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

    private async Task<string> GenerateLlama3Report(string error, string stack)
    {
        try
        {
            // Pull the URL we just defined in the Jenkinsfile
            string baseUrl = Environment.GetEnvironmentVariable("OLLAMA_API_URL") ?? "http://localhost:11434";

            using var client = new HttpClient();
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30); // AI can take a moment to think

            var payload = new
            {
                model = "llama3",
                prompt = $"You are a Senior QA Automation Engineer. Analyze this Playwright .NET failure and suggest a fix.\n\nError: {error}\n\nStack: {stack}",
                stream = false
            };

            var response = await client.PostAsJsonAsync("/api/generate", payload);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Newtonsoft.Json.Linq.JObject>();
                return result?["response"]?.ToString() ?? "AI returned an empty response.";
            }

            return $"> **AI Triage Warning:** Could not reach Llama 3 (Status: {response.StatusCode}). Check if Ollama is running.";
        }
        catch (Exception ex)
        {
            return $"> **AI Triage Error:** {ex.Message}. Verify that Ollama is listening on 0.0.0.0 and port 11434 is open.";
        }
    }
}       