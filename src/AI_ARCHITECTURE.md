# AI-Augmented Failure Triage Architecture

## The Problem
In enterprise CI/CD pipelines, test suites often generate hundreds of lines of logs upon failure. Automation engineers spend hours manually parsing Playwright traces, stack traces, and DOM errors to determine if a failure is a "Flaky Network," "Data Issue," or an actual "Application Bug."

## The Solution: Air-Gapped LLM Integration
This framework integrates a local Large Language Model (LLM) to automatically ingest failure contexts and output a categorized, 2-sentence root-cause analysis directly into the NUnit test report.

### Why Local (Ollama + Llama 3)?
Financial institutions operate under strict data privacy regulations. Sending proprietary source code, internal stack traces, or DOM snapshots to public APIs like OpenAI (ChatGPT) constitutes a critical security breach. 

By utilizing **Ollama**, the framework runs a quantized version of **Llama 3** entirely on the local machine (or the CI/CD runner). **Zero bytes of data leave the corporate network.**

---

## Technical Implementation Details

### 1. The `AiTriage` Base Class
Instead of inheriting directly from Playwright's `PageTest`, our test classes inherit from an abstract `AiTriage` class. This class uses NUnit's `[TearDown]` attribute to intercept the execution thread immediately after a test fails.

```csharp
[TearDown]
public async Task AnalyzeFailureAsync()
{
    if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
    {
        // Extracts the raw error and passes it to the AI Engine
    }
}
2. Prompt Engineering for Categorization
The LLM is not used as an open-ended chatbot. It is constrained by a strict architectural prompt designed for pipeline integration. It is forced to categorize the error into predefined buckets:

[LOCATOR_CHANGE]

[NETWORK_FLAKE]

[DATA_ISSUE]

[APPLICATION_BUG]

3. Overcoming Static Analysis & Resource Exhaustion
Calling an LLM requires an HttpClient. A common anti-pattern is creating a new HttpClient for every test, leading to socket exhaustion.

To solve this, the client is declared as static. However, static IDisposable objects trigger memory leak warnings in static analyzers (Roslyn/SonarLint).

The Architect's Fix: We shifted the lifecycle management of the HttpClient to NUnit's [SetUpFixture] (GlobalSetup.cs).

Creation: [OneTimeSetUp] guarantees the client is instantiated exactly once before the parallel test threads spawn.

Disposal: [OneTimeTearDown] guarantees the client is safely disposed when the application domain unloads.
This ensures thread safety during highly parallelized execution while passing all strict static analysis checks.

4. API Optimization
When querying the local Ollama API, the payload explicitly sets "stream": false. This prevents the NUnit test runner from hanging while waiting for token-by-token generation, ensuring the AI analysis is returned as a single, synchronous JSON response.

5. Jenkins CI/CD integration - Integrated a local LLM into the CI/CD pipeline to automatically generate Root Cause Analysis reports inside Allure for broken builds

6. The Browser is Alive: Because we injected those missing Linux graphics libraries, Headless Chromium successfully launched. It didn't crash out at the operating system level anymore!

7. TearDown is Firing: Since the setup succeeded, the tests actually ran. When they failed, your NUnit [TearDown] method perfectly caught the exceptions and successfully requested the analysis from your AI Triage class.

8. Artifact Archiving Works: Your Jenkins post actions correctly scanned the workspace, found the newly generated _AITriage.md files, and attached them to the build alongside the Allure report.

9. You now have a fully functioning DevSecOps pipeline with integrated AI failure analysis.