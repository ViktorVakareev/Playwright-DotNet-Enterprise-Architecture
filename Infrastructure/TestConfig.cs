namespace WorldBank.Automation.Tests.Infrastructure;

public static class TestConfig
{
    public static string TargetEnvironment { get; private set; } = "Sandbox";
    public static string BaseUrl { get; private set; } = "https://sandbox.worldbank.internal";

    // This method evaluates the Jenkins environment variables
    public static void Initialize()
    {
        // 1. Read the environment variable passed from the Jenkinsfile
        TargetEnvironment = Environment.GetEnvironmentVariable("TEST_ENV") ?? "Sandbox";

        // 2. Map the environment to the correct URLs/Secrets
        switch (TargetEnvironment.ToUpper())
        {
            case "QA":
                BaseUrl = "https://qa.worldbank.internal";
                // Optionally load QA-specific user secrets here
                break;
            case "PRE-PROD":
                BaseUrl = "https://preprod.worldbank.internal";
                break;
            case "SANDBOX":
            default:
                BaseUrl = "https://sandbox.worldbank.internal";
                break;
        }
    }
}