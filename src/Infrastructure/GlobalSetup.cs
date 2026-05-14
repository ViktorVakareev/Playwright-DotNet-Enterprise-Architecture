using NUnit.Framework;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace WorldBank.Automation.Tests;

[SetUpFixture]
public class GlobalSetup
{
    public static readonly ConcurrentBag<string> AiReports = new();

    // 1. Move the Traffic Light here!
    public static readonly SemaphoreSlim AiQueue = new SemaphoreSlim(1, 1);

    private static string ReportPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "AiTriage_Summary.md");

    [OneTimeSetUp]
    public void GlobalSetupMethod()
    {
        if (File.Exists(ReportPath)) File.Delete(ReportPath);
    }

    [OneTimeTearDown]
    public void GlobalTeardownMethod()
    {
        if (!AiReports.IsEmpty)
        {
            string header = "# 🤖 Llama 3 Aggregate Failure Analysis\n\n";
            File.WriteAllText(ReportPath, header + string.Join("\n", AiReports));
            TestContext.Progress.WriteLine($"[AI] Master report generated: {ReportPath}");
        }

        // 2. Properly dispose of the queue when the ENTIRE test run is completely finished
        AiQueue?.Dispose();
    }
}