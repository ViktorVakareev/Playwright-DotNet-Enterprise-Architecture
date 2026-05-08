using Allure.Commons;
using Microsoft.Playwright.NUnit;
using NUnit.Allure.Core;

namespace WorldBank.Automation.Tests.Infrastructure;

[AllureNUnit]
public class AiTriage : PageTest
{
    [TearDown]
    public async Task TriageOnFailure()
    {
        // 1. Check if the test actually failed
        if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            // 2. Check if Jenkins passed the parameter to run the AI
            bool isAiEnabled = Environment.GetEnvironmentVariable("AI_TRIAGE_ENABLED")?.ToLower() == "true";

            if (isAiEnabled)
            {
                TestContext.Progress.WriteLine("[AI] Test failed. Requesting Llama 3 analysis...");

                // Grab the error message and stack trace
                string stackTrace = TestContext.CurrentContext.Result.StackTrace ?? "No stack trace available";
                string errorMessage = TestContext.CurrentContext.Result.Message ?? "No error message available";

                // Call your local Llama 3 model (Assume this method exists in your AiClient)
                var aiAnalysis = await GenerateLlama3Report(errorMessage, stackTrace);

                // 3. Save the report to a physical file for Jenkins to archive
                var safeTestName = TestContext.CurrentContext.Test.Name.Replace("\"", "").Replace(" ", "_");
                var reportPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{safeTestName}_AITriage.md");

                await File.WriteAllTextAsync(reportPath, aiAnalysis);

                // 4. Attach the AI Report to NUnit (and inherently to Allure)
                TestContext.AddTestAttachment(reportPath, "Llama 3 Triage Analysis");
                AllureLifecycle.Instance.AddAttachment("Llama 3 Root Cause Analysis", "text/markdown", reportPath);

                TestContext.Progress.WriteLine($"[AI] Analysis saved to {reportPath}");
            }
            else
            {
                TestContext.Progress.WriteLine("[AI] Triage skipped (AI_TRIAGE_ENABLED is false).");
            }
        }
    }

    private async Task<string> GenerateLlama3Report(string error, string stack)
    {
        // Your existing Llama 3 HTTP POST logic goes here.
        // Returning a placeholder for demonstration.
        await Task.CompletedTask;
        return $"## 🤖 Llama 3 Failure Analysis\n\n**Error:** `{error}`\n\n**Root Cause Hypothesis:** The element was likely intercepted by a loading spinner or MFA overlay.\n\n**Suggested Fix:** Add `await Page.Locator('.spinner').WaitForAsync(new() {{ State = WaitForSelectorState.Hidden }});` before clicking.";
    }
}