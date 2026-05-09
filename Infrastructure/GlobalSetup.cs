using NUnit.Framework;
using System.Collections.Concurrent;
using System.IO;

namespace WorldBank.Automation.Tests;

[SetUpFixture]
public class GlobalSetup
{
    // This holds all failures in memory until the very end
    public static readonly ConcurrentBag<string> AiReports = new();

    // This goes up two levels from the bin folder to the project root
    private static string ReportPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "AiTriage_Summary.md");

    [OneTimeSetUp]
    public void GlobalSetupMethod()
    {
        if (File.Exists(ReportPath)) File.Delete(ReportPath);
    }

    [OneTimeTearDown]
    public void GlobalTeardownMethod()
    {
        if (AiReports.IsEmpty) return;

        // Create the one single file that Jenkins will show
        string header = "# 🤖 Llama 3 Failure Analysis Summary\n\n";
        File.WriteAllText(ReportPath, header + string.Join("\n", AiReports));
    }
}