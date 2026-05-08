using NUnit.Framework;

namespace WorldBank.Automation.Tests.Infrastructure;

[SetUpFixture]
public class GlobalSetup
{
    public static HttpClient AiClient { get; private set; } = null!;

    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        // 1. Initialize the global configuration from Jenkins
        TestConfig.Initialize();

        // 2. Initialize the AI Triage Client
        AiClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        TestContext.Progress.WriteLine($"[INIT] Target Environment set to: {TestConfig.TargetEnvironment}");
        TestContext.Progress.WriteLine($"[INIT] Base URL mapped to: {TestConfig.BaseUrl}");
    }

    [OneTimeTearDown]
    public void RunAfterAllTests()
    {
        AiClient?.Dispose();
    }
}