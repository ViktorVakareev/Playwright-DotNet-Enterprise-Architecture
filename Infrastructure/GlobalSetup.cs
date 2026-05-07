using NUnit.Framework;

namespace WorldBank.Automation.Tests.Infrastructure;

[SetUpFixture]
public class GlobalSetup
{
    // The client is now globally owned and managed here
    public static HttpClient AiClient { get; private set; } = null!;

    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        // Initialize exactly once before the test run starts
        AiClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    [OneTimeTearDown]
    public void RunAfterAllTests()
    {
        // Dispose exactly once after all tests finish
        AiClient?.Dispose();
        TestContext.Progress.WriteLine("Global Teardown Complete: AiClient safely disposed.");
    }
}