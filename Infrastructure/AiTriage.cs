using Microsoft.Playwright.NUnit;
using NUnit.Allure.Core;
using NUnit.Framework.Interfaces;
using System.Text;
using System.Text.Json;

namespace WorldBank.Automation.Tests.Infrastructure;

[AllureNUnit]
public abstract class AiTriage : PageTest
{
    private const string OllamaEndpoint = "http://localhost:11434/api/generate";

    [TearDown]
    public async Task AnalyzeFailureAsync()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
        {
            var errorMessage = TestContext.CurrentContext.Result.Message ?? "Unknown Error";
            var stackTrace = TestContext.CurrentContext.Result.StackTrace ?? "";

            var fullContext = $"Error: {errorMessage}\nStack: {stackTrace}";
            var analysis = await GetFailureAnalysis(fullContext);

            TestContext.WriteLine("\n" + new string('=', 40));
            TestContext.WriteLine("--- [AI ARCHITECT TRIAGE REPORT] ---");
            TestContext.WriteLine(analysis);
            TestContext.WriteLine(new string('=', 40) + "\n");
        }
    }

    public static async Task<string> GetFailureAnalysis(string context)
    {
        var prompt = $@"
        Act as a Senior Test Architect. Analyze this Playwright/NUnit failure.
        Categorize it into: [LOCATOR_CHANGE], [NETWORK_FLAKE], [DATA_ISSUE], or [APPLICATION_BUG].
        Provide a 1-sentence root cause and a 1-sentence fix.
        
        Failure Context:
        {context}";

        var requestBody = new
        {
            model = "llama3",
            prompt = prompt,
            stream = false
        };

        try
        {
            var jsonPayload = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Notice we use GlobalSetup.AiClient here
            var response = await GlobalSetup.AiClient.PostAsync(OllamaEndpoint, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);

            return doc.RootElement.GetProperty("response").GetString() ?? "Analysis failed.";
        }
        catch (Exception ex)
        {
            return $"[TRIAGE_ERROR]: AI Triage failed. ({ex.Message})";
        }
    }
}